using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MbokTask.Domain.Entities;
using MbokTask.Domain.Enums;
using MbokTask.Infrastructure.Persistence.Context;
using Microsoft.Extensions.Logging;

namespace MbokTask.Infrastructure.Services;

/// <summary>
/// Sample data seeder for development and testing purposes
/// Creates realistic sample data for demonstration
/// </summary>
public class SampleDataSeeder
{
    private readonly TaskManagementDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<SampleDataSeeder> _logger;

    public SampleDataSeeder(
        TaskManagementDbContext context,
        UserManager<User> userManager,
        ILogger<SampleDataSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SeedSampleDataAsync()
    {
        try
        {
            _logger.LogInformation("Starting sample data seeding...");

            await SeedAdditionalUsersAsync();
            await SeedAdditionalProjectsAsync();
            await SeedAdditionalTasksAsync();
            await SeedAdditionalCommentsAsync();

            _logger.LogInformation("Sample data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding sample data");
            throw;
        }
    }

    private async Task SeedAdditionalUsersAsync()
    {
        _logger.LogInformation("Seeding additional sample users...");

        var sampleUsers = new[]
        {
            new
            {
                Email = "alice.johnson@taskmanagement.com",
                FirstName = "Alice",
                LastName = "Johnson",
                Role = "Manager",
                Password = "Manager123!",
                PreferredLanguage = "en"
            },
            new
            {
                Email = "carlos.rodriguez@taskmanagement.com",
                FirstName = "Carlos",
                LastName = "Rodriguez",
                Role = "Member",
                Password = "Member123!",
                PreferredLanguage = "es"
            },
            new
            {
                Email = "marie.dubois@taskmanagement.com",
                FirstName = "Marie",
                LastName = "Dubois",
                Role = "Member",
                Password = "Member123!",
                PreferredLanguage = "fr"
            },
            new
            {
                Email = "hans.mueller@taskmanagement.com",
                FirstName = "Hans",
                LastName = "Mueller",
                Role = "Member",
                Password = "Member123!",
                PreferredLanguage = "de"
            },
            new
            {
                Email = "ana.silva@taskmanagement.com",
                FirstName = "Ana",
                LastName = "Silva",
                Role = "Member",
                Password = "Member123!",
                PreferredLanguage = "pt"
            }
        };

        foreach (var userData in sampleUsers)
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
                    CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 90)),
                    UpdatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                    IsActive = true,
                    PreferredLanguage = userData.PreferredLanguage
                };

                var result = await _userManager.CreateAsync(user, userData.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, userData.Role);
                    _logger.LogInformation("Created sample user: {Email}", userData.Email);
                }
            }
        }
    }

    private async Task SeedAdditionalProjectsAsync()
    {
        _logger.LogInformation("Seeding additional sample projects...");

        if (await _context.Projects.CountAsync() >= 5)
        {
            _logger.LogInformation("Sufficient projects already exist, skipping additional project seeding");
            return;
        }

        var users = await _userManager.Users.ToListAsync();
        var managers = users.Where(u => _userManager.IsInRoleAsync(u, "Manager").Result || 
                                       _userManager.IsInRoleAsync(u, "Admin").Result).ToList();

        if (!managers.Any())
        {
            _logger.LogWarning("No managers found for additional project seeding");
            return;
        }

        var additionalProjects = new[]
        {
            new
            {
                Name = "E-commerce Platform",
                Description = "Development of a modern e-commerce platform with microservices architecture, payment integration, and real-time inventory management.",
                Status = ProjectStatus.Active,
                Color = "#9b59b6",
                DaysAgo = 45
            },
            new
            {
                Name = "Data Analytics Dashboard",
                Description = "Business intelligence dashboard with real-time data visualization, custom reporting, and predictive analytics capabilities.",
                Status = ProjectStatus.Planning,
                Color = "#f39c12",
                DaysAgo = 20
            },
            new
            {
                Name = "Customer Support Portal",
                Description = "Comprehensive customer support system with ticket management, knowledge base, live chat, and customer satisfaction tracking.",
                Status = ProjectStatus.OnHold,
                Color = "#e67e22",
                DaysAgo = 60
            },
            new
            {
                Name = "IoT Device Management",
                Description = "Platform for managing IoT devices with real-time monitoring, firmware updates, and predictive maintenance capabilities.",
                Status = ProjectStatus.Active,
                Color = "#1abc9c",
                DaysAgo = 30
            }
        };

        var projects = new List<Project>();

        foreach (var projectData in additionalProjects)
        {
            var owner = managers[Random.Shared.Next(managers.Count)];
            var createdDate = DateTime.UtcNow.AddDays(-projectData.DaysAgo);

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = projectData.Name,
                Description = projectData.Description,
                Status = projectData.Status,
                StartDate = projectData.Status == ProjectStatus.Active ? createdDate.AddDays(7) : null,
                EndDate = projectData.Status == ProjectStatus.Active ? createdDate.AddDays(Random.Shared.Next(90, 180)) : null,
                Color = projectData.Color,
                OwnerId = owner.Id,
                CreatedAt = createdDate,
                UpdatedAt = createdDate,
                IsActive = true
            };

            projects.Add(project);
        }

        _context.Projects.AddRange(projects);
        await _context.SaveChangesAsync();

        // Add random project members
        var allUsers = users.Where(u => _userManager.IsInRoleAsync(u, "Member").Result).ToList();
        var projectMembers = new List<ProjectMember>();

        foreach (var project in projects)
        {
            var memberCount = Random.Shared.Next(2, 6);
            var selectedMembers = allUsers.OrderBy(x => Random.Shared.Next()).Take(memberCount);

            foreach (var member in selectedMembers)
            {
                projectMembers.Add(new ProjectMember
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    UserId = member.Id,
                    Role = Random.Shared.Next(10) < 2 ? UserRole.Manager : UserRole.Member,
                    JoinedAt = project.CreatedAt.AddDays(Random.Shared.Next(1, 10)),
                    CreatedAt = project.CreatedAt.AddDays(Random.Shared.Next(1, 10)),
                    UpdatedAt = project.CreatedAt.AddDays(Random.Shared.Next(1, 10)),
                    IsActive = true
                });
            }
        }

        _context.ProjectMembers.AddRange(projectMembers);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {ProjectCount} additional projects with {MemberCount} project members", 
            projects.Count, projectMembers.Count);
    }

    private async Task SeedAdditionalTasksAsync()
    {
        _logger.LogInformation("Seeding additional sample tasks...");

        var projects = await _context.Projects.Include(p => p.Members).ToListAsync();
        var users = await _userManager.Users.ToListAsync();

        if (!projects.Any() || !users.Any())
        {
            _logger.LogWarning("No projects or users found for additional task seeding");
            return;
        }

        var taskTemplates = new[]
        {
            new { Title = "Setup Development Environment", Description = "Configure development environment with necessary tools, dependencies, and IDE settings for optimal productivity.", Priority = TaskPriority.High, EstimatedHours = 8, Tags = "setup,environment,development" },
            new { Title = "Code Review and Refactoring", Description = "Review existing codebase, identify areas for improvement, and refactor code for better maintainability and performance.", Priority = TaskPriority.Medium, EstimatedHours = 16, Tags = "code-review,refactoring,quality" },
            new { Title = "Unit Test Implementation", Description = "Write comprehensive unit tests to ensure code quality and prevent regressions in future development cycles.", Priority = TaskPriority.High, EstimatedHours = 12, Tags = "testing,unit-tests,quality" },
            new { Title = "Performance Optimization", Description = "Analyze application performance, identify bottlenecks, and implement optimizations for better user experience.", Priority = TaskPriority.Medium, EstimatedHours = 20, Tags = "performance,optimization,analysis" },
            new { Title = "Security Audit", Description = "Conduct thorough security audit to identify vulnerabilities and implement necessary security measures and best practices.", Priority = TaskPriority.Critical, EstimatedHours = 24, Tags = "security,audit,vulnerability" },
            new { Title = "User Interface Design", Description = "Design intuitive and responsive user interface components following modern UX/UI principles and accessibility standards.", Priority = TaskPriority.Medium, EstimatedHours = 18, Tags = "ui,design,ux,accessibility" },
            new { Title = "Database Migration", Description = "Plan and execute database schema migration with proper backup strategies and rollback procedures.", Priority = TaskPriority.High, EstimatedHours = 14, Tags = "database,migration,schema" },
            new { Title = "Integration Testing", Description = "Develop and execute integration tests to ensure proper communication between different system components.", Priority = TaskPriority.Medium, EstimatedHours = 16, Tags = "testing,integration,components" },
            new { Title = "Documentation Update", Description = "Update technical documentation, API references, and user guides to reflect recent changes and improvements.", Priority = TaskPriority.Low, EstimatedHours = 10, Tags = "documentation,api,guides" },
            new { Title = "Deployment Pipeline", Description = "Setup automated deployment pipeline with proper staging environments and continuous integration workflows.", Priority = TaskPriority.High, EstimatedHours = 22, Tags = "deployment,ci-cd,automation" }
        };

        var tasks = new List<TaskItem>();
        var random = Random.Shared;

        foreach (var project in projects.Take(3)) // Only add tasks to first 3 projects
        {
            var projectMembers = project.Members.Where(m => !m.IsDeleted).ToList();
            var taskCount = random.Next(3, 8);

            for (int i = 0; i < taskCount; i++)
            {
                var template = taskTemplates[random.Next(taskTemplates.Length)];
                var createdDate = project.CreatedAt.AddDays(random.Next(1, 30));
                var assignedMember = projectMembers.Any() ? projectMembers[random.Next(projectMembers.Count)] : null;
                
                var status = GetRandomTaskStatus();
                var task = new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = $"{template.Title} - {project.Name}",
                    Description = template.Description,
                    Status = status,
                    Priority = template.Priority,
                    DueDate = status != Domain.Enums.TaskStatus.Done ? createdDate.AddDays(random.Next(7, 60)) : null,
                    CompletedAt = status == Domain.Enums.TaskStatus.Done ? createdDate.AddDays(random.Next(1, 20)) : null,
                    EstimatedHours = template.EstimatedHours,
                    ActualHours = status == Domain.Enums.TaskStatus.Done ? random.Next(template.EstimatedHours - 5, template.EstimatedHours + 10) : 
                                 status == Domain.Enums.TaskStatus.InProgress ? random.Next(1, template.EstimatedHours / 2) : 0,
                    Tags = template.Tags,
                    ProjectId = project.Id,
                    CreatedById = project.OwnerId,
                    AssignedToId = assignedMember?.UserId,
                    CreatedAt = createdDate,
                    UpdatedAt = status != Domain.Enums.TaskStatus.ToDo ? createdDate.AddDays(random.Next(1, 15)) : createdDate,
                    IsActive = true
                };

                tasks.Add(task);
            }
        }

        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {TaskCount} additional tasks", tasks.Count);
    }

    private async Task SeedAdditionalCommentsAsync()
    {
        _logger.LogInformation("Seeding additional sample comments...");

        var tasks = await _context.Tasks
            .Include(t => t.Project)
            .ThenInclude(p => p.Members)
            .Where(t => !_context.TaskComments.Any(c => c.TaskId == t.Id))
            .Take(10)
            .ToListAsync();

        if (!tasks.Any())
        {
            _logger.LogInformation("No tasks without comments found, skipping additional comment seeding");
            return;
        }

        var commentTemplates = new[]
        {
            "Started working on this task. Initial analysis looks promising.",
            "Encountered some technical challenges, but found a viable solution.",
            "Making good progress. Should be completed ahead of schedule.",
            "Need to coordinate with the team on this dependency.",
            "Updated the implementation based on code review feedback.",
            "Testing phase completed successfully. Ready for deployment.",
            "Documentation has been updated to reflect the changes.",
            "Performance improvements implemented as requested.",
            "Integration with external API completed and tested.",
            "Final review completed. Task is ready for production."
        };

        var comments = new List<TaskComment>();
        var random = Random.Shared;

        foreach (var task in tasks)
        {
            var commentCount = random.Next(1, 4);
            var projectMembers = task.Project.Members.Where(m => !m.IsDeleted).ToList();
            
            if (!projectMembers.Any()) continue;

            for (int i = 0; i < commentCount; i++)
            {
                var author = projectMembers[random.Next(projectMembers.Count)];
                var commentDate = task.CreatedAt.AddDays(random.Next(1, 20));
                
                comments.Add(new TaskComment
                {
                    Id = Guid.NewGuid(),
                    Content = commentTemplates[random.Next(commentTemplates.Length)],
                    TaskId = task.Id,
                    AuthorId = author.UserId,
                    CreatedAt = commentDate,
                    UpdatedAt = commentDate,
                    IsActive = true
                });
            }
        }

        _context.TaskComments.AddRange(comments);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {CommentCount} additional comments", comments.Count);
    }

    private static Domain.Enums.TaskStatus GetRandomTaskStatus()
    {
        var statuses = new[] 
        { 
            Domain.Enums.TaskStatus.ToDo, 
            Domain.Enums.TaskStatus.InProgress, 
            Domain.Enums.TaskStatus.Done,
            Domain.Enums.TaskStatus.OnHold
        };
        
        var weights = new[] { 30, 40, 25, 5 }; // Percentage weights
        var random = Random.Shared.Next(100);
        var cumulative = 0;
        
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (random < cumulative)
                return statuses[i];
        }
        
        return Domain.Enums.TaskStatus.ToDo;
    }
}