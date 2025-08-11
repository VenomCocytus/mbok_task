using Asp.Versioning;
using MbokTask.Application.DTOs;
using MbokTask.Domain.Entities;
using MbokTask.Infrastructure.Persistence.Context;
using MbokTask.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MbokTask.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
// [Authorize(Roles = "Admin")]
[Produces("application/json")]
public class SeedController(
    TaskManagementDbContext context,
    UserManager<User> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    ILogger<SeedController> logger)
    : ControllerBase
{
    /// <summary>
    /// Seed the database with initial data (Admin only)
    /// </summary>
    /// <returns>Seeding result</returns>
    [HttpPost("initial")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> SeedInitialData()
    {
        try
        {
            logger.LogInformation("Starting initial data seeding via API endpoint");

            var seeder = new DatabaseSeeder(context, userManager, roleManager, 
                HttpContext.RequestServices.GetRequiredService<ILogger<DatabaseSeeder>>());
            
            await seeder.SeedAsync();

            return Ok(ApiResponse<object>.SuccessResult(
                new { Message = "Initial data seeded successfully" },
                "seed.initial.success",
                "Initial data has been seeded successfully"
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during initial data seeding");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "seed.initial.error",
                ["An error occurred while seeding initial data"]
            ));
        }
    }

    /// <summary>
    /// Seed the database with sample data for development/testing (Admin only)
    /// </summary>
    /// <returns>Seeding result</returns>
    [HttpPost("sample")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> SeedSampleData()
    {
        try
        {
            logger.LogInformation("Starting sample data seeding via API endpoint");

            var seeder = new SampleDataSeeder(context, userManager, 
                HttpContext.RequestServices.GetRequiredService<ILogger<SampleDataSeeder>>());
            
            await seeder.SeedSampleDataAsync();

            return Ok(ApiResponse<object>.SuccessResult(
                new { Message = "Sample data seeded successfully" },
                "seed.sample.success",
                "Sample data has been seeded successfully"
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during sample data seeding");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "seed.sample.error",
                ["An error occurred while seeding sample data"]
            ));
        }
    }

    /// <summary>
    /// Get database statistics (Admin only)
    /// </summary>
    /// <returns>Database statistics</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> GetDatabaseStats()
    {
        try
        {
            var stats = new
            {
                Users = await userManager.Users.CountAsync(),
                Projects = await context.Projects.CountAsync(),
                Tasks = await context.Tasks.CountAsync(),
                Comments = await context.TaskComments.CountAsync(),
                ActivityLogs = await context.ActivityLogs.CountAsync(),
                ProjectMembers = await context.ProjectMembers.CountAsync()
            };

            return Ok(ApiResponse<object>.SuccessResult(
                stats,
                "stats.retrieved.success",
                "Database statistics retrieved successfully"
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while retrieving database statistics");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "stats.error",
                ["An error occurred while retrieving database statistics"]
            ));
        }
    }

    /// <summary>
    /// Reset database (Admin only) - USE WITH CAUTION
    /// </summary>
    /// <returns>Reset result</returns>
    [HttpDelete("reset")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<IActionResult> ResetDatabase()
    {
        try
        {
            if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                return BadRequest(ApiResponse<object>.ErrorResult(
                    "reset.not.allowed",
                    ["Database reset is only allowed in development environment"]
                ));
            }

            logger.LogWarning("Starting database reset via API endpoint");

            // Delete all data in reverse dependency order
            context.TaskComments.RemoveRange(context.TaskComments);
            context.ActivityLogs.RemoveRange(context.ActivityLogs);
            context.Tasks.RemoveRange(context.Tasks);
            context.ProjectMembers.RemoveRange(context.ProjectMembers);
            context.Projects.RemoveRange(context.Projects);
            
            // Remove user roles
            var userRoles = context.UserRoles.ToList();
            context.UserRoles.RemoveRange(userRoles);
            
            // Remove users (except keep one admin for access)
            var users = await userManager.Users.Where(u => u.Email != "admin@taskmanagement.com").ToListAsync();
            foreach (var user in users)
            {
                await userManager.DeleteAsync(user);
            }

            await context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(
                new { Message = "Database reset successfully" },
                "reset.success",
                "Database has been reset successfully"
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during database reset");
            return StatusCode(500, ApiResponse<object>.ErrorResult(
                "reset.error",
                ["An error occurred while resetting the database"]
            ));
        }
    }
}