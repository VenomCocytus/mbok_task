using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MbokTask.Domain.Entities;
using MbokTask.Domain.Enums;
using MbokTask.Infrastructure.Persistence.Context;

namespace MbokTask.Infrastructure.Services;

public class DatabaseSeeder
{
    private readonly TaskManagementDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        TaskManagementDbContext context,
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding...");

            // Ensure database is created and migrated
            await _context.Database.MigrateAsync();

            // Seed in order of dependencies
            await SeedRolesAsync();
            await SeedUsersAsync();
            await SeedProjectsAsync();
            await SeedTasksAsync();
            await SeedCommentsAsync();

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        _logger.LogInformation("Seeding roles...");

        var roles = new[] { "Admin", "Manager", "Member" };
        
        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpper()
                };

                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Created role: {RoleName}", roleName);
                }
                else
                {
                    _logger.LogWarning("Failed to create role {RoleName}: {Errors}", 
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private async Task SeedUsersAsync()
    {
        _logger.LogInformation("Seeding users...");

        var users = new[]
        {
            new
            {
                Email = "admin@taskmanagement.com",
                FirstName = "System",
                LastName = "Administrator",
                Role = "Admin",
                Password = "Admin123!"
            },
            new
            {
                Email = "manager@taskmanagement.com",
                FirstName = "Project",
                LastName = "Manager",
                Role = "Manager",
                Password = "Manager123!"
            },
            new
            {
                Email = "john.doe@taskmanagement.com",
                FirstName = "John",
                LastName = "Doe",
                Role = "Member",
                Password = "Member123!"
            },
            new
            {
                Email = "jane.smith@taskmanagement.com",
                FirstName = "Jane",
                LastName = "Smith",
                Role = "Member",
                Password = "Member123!"
            },
            new
            {
                Email = "bob.wilson@taskmanagement.com",
                FirstName = "Bob",
                LastName = "Wilson",
                Role = "Member",
                Password = "Member123!"
            }
        };

        foreach (var userData in users)
        {
            if (await _userManager.FindByEmailAsync(userData.Email) == null)
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = userData.Email,
                    Email = userData.Email,
                    FirstName = userData.FirstName,
                    LastName = userData.LastName,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    PreferredLanguage = "en"
                };

                var result = await _userManager.CreateAsync(user, userData.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, userData.Role);
                    _logger.LogInformation("Created user: {Email} with role: {Role}", userData.Email, userData.Role);
                }
                else
                {
                    _logger.LogWarning("Failed to create user {Email}: {Errors}", 
                        userData.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private async Task SeedProjectsAsync()
    {
        _logger.LogInformation("Seeding projects...");

        if (await _context.Projects.AnyAsync())
        {
            _logger.LogInformation("Projects already exist, skipping seeding");
            return;
        }

        var adminUser = await _userManager.FindByEmailAsync("admin@taskmanagement.com");
        var managerUser = await _userManager.FindByEmailAsync("manager@taskmanagement.com");
        var johnUser = await _userManager.FindByEmailAsync("john.doe@taskmanagement.com");
        var janeUser = await _userManager.FindByEmailAsync("jane.smith@taskmanagement.com");

        if (adminUser == null || managerUser == null)
        {
            _logger.LogWarning("Required users not found for project seeding");
            return;
        }

        var projects = new[]
        {
            new Project
            {
                Id = Guid.NewGuid(),
                Name = "Task Management System",
                Description = "A comprehensive task management system with user authentication, project management, and team collaboration features.",
                Status = ProjectStatus.Active,
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddDays(60),
                Color = "#3498db",
                OwnerId = adminUser.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30),
                IsActive = true
            },
            new Project
            {
                Id = Guid.NewGuid(),
                Name = "Mobile App Development",
                Description = "Development of a mobile application for task management with offline capabilities and real-time synchronization.",
                Status = ProjectStatus.Planning,
                StartDate = DateTime.UtcNow.AddDays(7),
                EndDate = DateTime.UtcNow.AddDays(120),
                Color = "#e74c3c",
                OwnerId = managerUser.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-15),
                IsActive = true
            },
            new Project
            {
                Id = Guid.NewGuid(),
                Name = "API Documentation",
                Description = "Create comprehensive API documentation with examples and integration guides for developers.",
                Status = ProjectStatus.Active,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(30),
                Color = "#2ecc71",
                OwnerId = adminUser.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10),
                IsActive = true
            }
        };

        _context.Projects.AddRange(projects);
        await _context.SaveChangesAsync();

        // Add project members
        var projectMembers = new List<ProjectMember>();

        // Task Management System members
        var taskMgmtProject = projects[0];
        projectMembers.AddRange(new[]
        {
            new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = taskMgmtProject.Id,
                UserId = managerUser.Id,
                Role = UserRole.Manager,
                JoinedAt = DateTime.UtcNow.AddDays(-25),
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow.AddDays(-25),
                IsActive = true
            },
            new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = taskMgmtProject.Id,
                UserId = johnUser?.Id ?? Guid.NewGuid(),
                Role = UserRole.Member,
                JoinedAt = DateTime.UtcNow.AddDays(-20),
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-20),
                IsActive = true
            },
            new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = taskMgmtProject.Id,
                UserId = janeUser?.Id ?? Guid.NewGuid(),
                Role = UserRole.Member,
                JoinedAt = DateTime.UtcNow.AddDays(-18),
                CreatedAt = DateTime.UtcNow.AddDays(-18),
                UpdatedAt = DateTime.UtcNow.AddDays(-18),
                IsActive = true
            }
        });

        // Mobile App Development members
        var mobileProject = projects[1];
        projectMembers.AddRange(new[]
        {
            new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = mobileProject.Id,
                UserId = johnUser?.Id ?? Guid.NewGuid(),
                Role = UserRole.Member,
                JoinedAt = DateTime.UtcNow.AddDays(-12),
                CreatedAt = DateTime.UtcNow.AddDays(-12),
                UpdatedAt = DateTime.UtcNow.AddDays(-12),
                IsActive = true
            }
        });

        _context.ProjectMembers.AddRange(projectMembers);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {ProjectCount} projects with {MemberCount} project members", 
            projects.Length, projectMembers.Count);
    }

    private async Task SeedTasksAsync()
    {
        _logger.LogInformation("Seeding tasks...");

        if (await _context.Tasks.AnyAsync())
        {
            _logger.LogInformation("Tasks already exist, skipping seeding");
            return;
        }

        var projects = await _context.Projects.ToListAsync();
        var users = await _userManager.Users.ToListAsync();

        if (!projects.Any() || !users.Any())
        {
            _logger.LogWarning("No projects or users found for task seeding");
            return;
        }

        var adminUser = users.First(u => u.Email == "admin@taskmanagement.com");
        var johnUser = users.FirstOrDefault(u => u.Email == "john.doe@taskmanagement.com");
        var janeUser = users.FirstOrDefault(u => u.Email == "jane.smith@taskmanagement.com");

        var taskMgmtProject = projects.First(p => p.Name == "Task Management System");
        var apiDocProject = projects.First(p => p.Name == "API Documentation");

        var tasks = new[]
        {
            new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Implement User Authentication",
                Description = "Develop JWT-based authentication system with role-based authorization for secure user access.",
                Status = Domain.Enums.TaskStatus.Done,
                Priority = TaskPriority.High,
                DueDate = DateTime.UtcNow.AddDays(-5),
                CompletedAt = DateTime.UtcNow.AddDays(-7),
                EstimatedHours = 16,
                ActualHours = 18,
                Tags = "authentication,security,jwt",
                ProjectId = taskMgmtProject.Id,
                CreatedById = adminUser.Id,
                AssignedToId = johnUser?.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow.AddDays(-7),
                IsActive = true
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Design Database Schema",
                Description = "Create comprehensive database schema with proper relationships, indexes, and constraints for optimal performance.",
                Status = Domain.Enums.TaskStatus.Done,
                Priority = TaskPriority.Critical,
                DueDate = DateTime.UtcNow.AddDays(-15),
                CompletedAt = DateTime.UtcNow.AddDays(-18),
                EstimatedHours = 12,
                ActualHours = 14,
                Tags = "database,schema,design",
                ProjectId = taskMgmtProject.Id,
                CreatedById = adminUser.Id,
                AssignedToId = adminUser.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-18),
                IsActive = true
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Implement Task CRUD Operations",
                Description = "Develop complete CRUD operations for task management including creation, reading, updating, and deletion with proper validation.",
                Status = Domain.Enums.TaskStatus.InProgress,
                Priority = TaskPriority.High,
                DueDate = DateTime.UtcNow.AddDays(5),
                EstimatedHours = 20,
                ActualHours = 12,
                Tags = "crud,tasks,api",
                ProjectId = taskMgmtProject.Id,
                CreatedById = adminUser.Id,
                AssignedToId = janeUser?.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                IsActive = true
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Add Real-time Notifications",
                Description = "Implement SignalR for real-time notifications when tasks are created, updated, or assigned to team members.",
                Status = Domain.Enums.TaskStatus.ToDo,
                Priority = TaskPriority.Medium,
                DueDate = DateTime.UtcNow.AddDays(15),
                EstimatedHours = 24,
                ActualHours = 0,
                Tags = "signalr,notifications,realtime",
                ProjectId = taskMgmtProject.Id,
                CreatedById = adminUser.Id,
                AssignedToId = johnUser?.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10),
                IsActive = true
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Write API Documentation",
                Description = "Create comprehensive API documentation using OpenAPI/Swagger with detailed examples and integration guides.",
                Status = Domain.Enums.TaskStatus.InProgress,
                Priority = TaskPriority.Medium,
                DueDate = DateTime.UtcNow.AddDays(10),
                EstimatedHours = 16,
                ActualHours = 8,
                Tags = "documentation,api,swagger",
                ProjectId = apiDocProject.Id,
                CreatedById = adminUser.Id,
                AssignedToId = janeUser?.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                IsActive = true
            },
            new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Setup CI/CD Pipeline",
                Description = "Configure continuous integration and deployment pipeline using GitHub Actions for automated testing and deployment.",
                Status = Domain.Enums.TaskStatus.ToDo,
                Priority = TaskPriority.Low,
                DueDate = DateTime.UtcNow.AddDays(20),
                EstimatedHours = 12,
                ActualHours = 0,
                Tags = "cicd,github-actions,deployment",
                ProjectId = taskMgmtProject.Id,
                CreatedById = adminUser.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
                IsActive = true
            }
        };

        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync();

        // Create activity logs for completed tasks
        var activityLogs = new List<ActivityLog>();
        
        foreach (var task in tasks.Where(t => t.Status == Domain.Enums.TaskStatus.Done))
        {
            activityLogs.AddRange(new[]
            {
                ActivityLog.CreateTaskActivity(
                    task.CreatedById,
                    task.Id,
                    ActivityType.TaskCreated,
                    $"Task '{task.Title}' created"
                ),
                ActivityLog.CreateTaskActivity(
                    task.AssignedToId ?? task.CreatedById,
                    task.Id,
                    ActivityType.TaskStatusChanged,
                    $"Task status changed from ToDo to InProgress",
                    "ToDo",
                    "InProgress"
                ),
                ActivityLog.CreateTaskActivity(
                    task.AssignedToId ?? task.CreatedById,
                    task.Id,
                    ActivityType.TaskStatusChanged,
                    $"Task status changed from InProgress to Done",
                    "InProgress",
                    "Done"
                )
            });
        }

        foreach (var task in tasks.Where(t => t.Status == Domain.Enums.TaskStatus.InProgress))
        {
            activityLogs.AddRange(new[]
            {
                ActivityLog.CreateTaskActivity(
                    task.CreatedById,
                    task.Id,
                    ActivityType.TaskCreated,
                    $"Task '{task.Title}' created"
                ),
                ActivityLog.CreateTaskActivity(
                    task.AssignedToId ?? task.CreatedById,
                    task.Id,
                    ActivityType.TaskStatusChanged,
                    $"Task status changed from ToDo to InProgress",
                    "ToDo",
                    "InProgress"
                )
            });
        }

        foreach (var task in tasks.Where(t => t.Status == Domain.Enums.TaskStatus.ToDo))
        {
            activityLogs.Add(ActivityLog.CreateTaskActivity(
                task.CreatedById,
                task.Id,
                ActivityType.TaskCreated,
                $"Task '{task.Title}' created"
            ));
        }

        _context.ActivityLogs.AddRange(activityLogs);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {TaskCount} tasks with {ActivityCount} activity logs", 
            tasks.Length, activityLogs.Count);
    }

    private async Task SeedCommentsAsync()
    {
        _logger.LogInformation("Seeding task comments...");

        if (await _context.TaskComments.AnyAsync())
        {
            _logger.LogInformation("Task comments already exist, skipping seeding");
            return;
        }

        var tasks = await _context.Tasks.ToListAsync();
        var users = await _userManager.Users.ToListAsync();

        if (!tasks.Any() || !users.Any())
        {
            _logger.LogWarning("No tasks or users found for comment seeding");
            return;
        }

        var johnUser = users.FirstOrDefault(u => u.Email == "john.doe@taskmanagement.com");
        var janeUser = users.FirstOrDefault(u => u.Email == "jane.smith@taskmanagement.com");
        var adminUser = users.First(u => u.Email == "admin@taskmanagement.com");

        var authTask = tasks.FirstOrDefault(t => t.Title == "Implement User Authentication");
        var crudTask = tasks.FirstOrDefault(t => t.Title == "Implement Task CRUD Operations");
        var docTask = tasks.FirstOrDefault(t => t.Title == "Write API Documentation");

        var comments = new List<TaskComment>();

        if (authTask != null)
        {
            comments.AddRange(new[]
            {
                new TaskComment
                {
                    Id = Guid.NewGuid(),
                    Content = "Started working on the JWT implementation. Setting up the authentication middleware and token validation.",
                    TaskId = authTask.Id,
                    AuthorId = johnUser?.Id ?? adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-20),
                    UpdatedAt = DateTime.UtcNow.AddDays(-20),
                    IsActive = true
                },
                new TaskComment
                {
                    Id = Guid.NewGuid(),
                    Content = "Authentication system is working well. Added role-based authorization and tested with different user roles.",
                    TaskId = authTask.Id,
                    AuthorId = johnUser?.Id ?? adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-10),
                    IsActive = true
                },
                new TaskComment
                {
                    Id = Guid.NewGuid(),
                    Content = "Great work on the authentication! The JWT implementation looks solid and secure.",
                    TaskId = authTask.Id,
                    AuthorId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-8),
                    UpdatedAt = DateTime.UtcNow.AddDays(-8),
                    IsActive = true
                }
            });
        }

        if (crudTask != null)
        {
            comments.AddRange(new[]
            {
                new TaskComment
                {
                    Id = Guid.NewGuid(),
                    Content = "Working on the task creation and update endpoints. The validation logic is getting complex with all the business rules.",
                    TaskId = crudTask.Id,
                    AuthorId = janeUser?.Id ?? adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5),
                    IsActive = true
                },
                new TaskComment
                {
                    Id = Guid.NewGuid(),
                    Content = "Need to add more comprehensive error handling for edge cases. Also considering adding bulk operations for better performance.",
                    TaskId = crudTask.Id,
                    AuthorId = janeUser?.Id ?? adminUser.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2),
                    IsActive = true
                }
            });
        }

        if (docTask != null)
        {
            comments.Add(new TaskComment
            {
                Id = Guid.NewGuid(),
                Content = "Started documenting the authentication endpoints. Adding request/response examples and error codes for better developer experience.",
                TaskId = docTask.Id,
                AuthorId = janeUser?.Id ?? adminUser.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-3),
                IsActive = true
            });
        }

        _context.TaskComments.AddRange(comments);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {CommentCount} task comments", comments.Count);
    }
}