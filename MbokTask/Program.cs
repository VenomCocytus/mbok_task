using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using FluentValidation;
using MediatR;
using AspNetCoreRateLimit;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using MbokTask.Domain.Entities;
using MbokTask.Infrastructure.Persistence.Context;
using Asp.Versioning;
using MbokTask.Middleware;
using MbokTask.Infrastructure.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Task Management API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    var services = builder.Services;
    var configuration = builder.Configuration;

    // Database Configuration
    services.AddDbContext<TaskManagementDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("DefaultConnection") ?? 
            "Host=localhost;Database=mbok_task;Username=postgres;Password=#Roxanne2004"));

    // Identity Configuration
    services.AddIdentity<User, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<TaskManagementDbContext>()
    .AddDefaultTokenProviders();

    // JWT Configuration
    var jwtSettings = configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
    var key = Encoding.ASCII.GetBytes(secretKey);

    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "TaskManagementAPI",
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"] ?? "TaskManagementAPI",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    // Authorization
    services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Manager", "Admin"));
        options.AddPolicy("MemberOrAbove", policy => policy.RequireRole("Member", "Manager", "Admin"));
    });

    // Redis Cache
    services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = configuration.GetConnectionString("Redis") ?? "localhost:6379";
    });

    // Rate Limiting
    services.AddMemoryCache();
    services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
    services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
    services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
    services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

    // CORS
    services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigins", policy =>
        {
            policy.WithOrigins(configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"])
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    // API Versioning
    services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new QueryStringApiVersionReader("version"),
            new HeaderApiVersionReader("X-Version")
        );
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // MediatR
    services.AddMediatR(typeof(Program).Assembly);

    // AutoMapper
    services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

    // FluentValidation
    services.AddValidatorsFromAssembly(typeof(Program).Assembly);

    // Localization
    services.AddLocalization(options => options.ResourcesPath = "Resources");
    services.Configure<RequestLocalizationOptions>(options =>
    {
        var supportedCultures = new[]
        {
            new CultureInfo("en"),
            new CultureInfo("fr"),
            new CultureInfo("es"),
            new CultureInfo("de"),
            new CultureInfo("pt")
        };

        options.DefaultRequestCulture = new RequestCulture("en");
        options.SupportedCultures = supportedCultures;
        options.SupportedUICultures = supportedCultures;

        options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
        options.RequestCultureProviders.Insert(1, new CookieRequestCultureProvider());
        options.RequestCultureProviders.Insert(2, new AcceptLanguageHeaderRequestCultureProvider());
    });

    // Health Checks
    services.AddHealthChecks()
        .AddNpgSql(configuration.GetConnectionString("DefaultConnection")!)
        .AddRedis(configuration.GetConnectionString("Redis")!)
        .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

    services.AddHealthChecksUI(options =>
    {
        options.SetEvaluationTimeInSeconds(30);
        options.MaximumHistoryEntriesPerEndpoint(50);
        options.AddHealthCheckEndpoint("Task Management API", "/health");
    }).AddInMemoryStorage();

    // OpenTelemetry
    services.AddOpenTelemetry()
        .WithTracing(providerBuilder =>
        {
            providerBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService("TaskManagementAPI", "1.0.0"))
                   .AddAspNetCoreInstrumentation()
                   .AddEntityFrameworkCoreInstrumentation()
                   .AddHttpClientInstrumentation();

            if (configuration.GetValue<bool>("Jaeger:Enabled"))
            {
                providerBuilder.AddJaegerExporter();
            }
        });

    // Controllers
    services.AddControllers();

    // Swagger/OpenAPI
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Task Management API",
            Version = "v1",
            Description = "A production-ready task management API built with ASP.NET Core",
            Contact = new OpenApiContact
            {
                Name = "API Support",
                Email = "support@taskmanagement.com"
            }
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                []
            }
        });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Management API V1");
            c.RoutePrefix = string.Empty;
        });
    }

    // Global exception handling and request logging
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseSerilogRequestLogging();

    // Security Headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'");
        await next();
    });

    app.UseHttpsRedirection();
    app.UseCors("AllowSpecificOrigins");

    // Rate Limiting
    app.UseIpRateLimiting();

    // Localization
    app.UseRequestLocalization();

    app.UseAuthentication();
    app.UseAuthorization();

    // Health Checks
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapHealthChecksUI();
    }

    app.MapControllers();

    // Sample endpoint for testing
    app.MapGet("/api/v1/status", () => new
    {
        Status = "OK",
        Timestamp = DateTime.UtcNow,
        Version = "1.0.0",
        Environment = app.Environment.EnvironmentName
    }).WithTags("System").WithOpenApi();

    // Database Migration and Seeding
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();

        try
        {
            var seeder = new DatabaseSeeder(context, userManager, roleManager, logger);
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while migrating or seeding the database");
        }
    }

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// static async Task SeedDataAsync(UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager)
// {
//     // Seed roles
//     var roles = new[] { "Admin", "Manager", "Member" };
//     foreach (var role in roles)
//     {
//         if (!await roleManager.RoleExistsAsync(role))
//         {
//             await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role, NormalizedName = role.ToUpper() });
//         }
//     }
//
//     // Seed admin user
//     if (await userManager.FindByEmailAsync("admin@taskmanagement.com") == null)
//     {
//         var adminUser = new User
//         {
//             Id = Guid.NewGuid(),
//             UserName = "admin@taskmanagement.com",
//             Email = "admin@taskmanagement.com",
//             FirstName = "System",
//             LastName = "Administrator",
//             EmailConfirmed = true,
//             CreatedAt = DateTime.UtcNow,
//             UpdatedAt = DateTime.UtcNow,
//             IsActive = true
//         };
//
//         var result = await userManager.CreateAsync(adminUser, "Admin123!");
//         if (result.Succeeded)
//         {
//             await userManager.AddToRoleAsync(adminUser, "Admin");
//         }
//     }
// }