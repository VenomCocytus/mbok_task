using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using MbokTask.Domain.Entities;
using MbokTask.Domain.Enums;
using MbokTask.Infrastructure.Persistence.Context;
using MbokTask.Application.DTOs;
using MbokTask.Application.Mappings;
using Asp.Versioning;

namespace MbokTask.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly TaskManagementDbContext _context;
    private readonly IStringLocalizer<ProjectsController> _localizer;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        TaskManagementDbContext context,
        IStringLocalizer<ProjectsController> localizer,
        ILogger<ProjectsController> logger)
    {
        _context = context;
        _localizer = localizer;
        _logger = logger;
    }

    /// <summary>
    /// Get all projects for the current user
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>Paginated list of projects</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ProjectDto>>), 200)]
    public async Task<IActionResult> GetProjects([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            
            var query = _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.Members.Where(m => !m.IsDeleted))
                .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                .Where(p => p.OwnerId == currentUserId || p.Members.Any(m => m.UserId == currentUserId && !m.IsDeleted));

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var projects = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProjectDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Status = p.Status,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    Color = p.Color,
                    OwnerId = p.OwnerId,
                    OwnerName = p.Owner.FullName,
                    MemberCount = p.Members.Count(m => !m.IsDeleted),
                    TaskCount = p.TaskCount,
                    CompletedTaskCount = p.CompletedTaskCount,
                    CompletionPercentage = p.CompletionPercentage,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            var pagination = new PaginationInfo
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalItems = totalItems,
                HasNext = page < totalPages,
                HasPrevious = page > 1
            };

            var response = ApiResponse<List<ProjectDto>>.SuccessResult(projects, "projects.retrieved.success", _localizer["Projects retrieved successfully"]);
            response.Pagination = pagination;

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving projects");
            return StatusCode(500, ApiResponse<object>.ErrorResult("internal.server.error", new List<string> { _localizer["An error occurred"] }));
        }
    }

    /// <summary>
    /// Get a specific project by ID
    /// </summary>
    /// <param name="id">Project ID</param>
    /// <returns>Project details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetProject(Guid id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            
            var project = await _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.Members.Where(m => !m.IsDeleted))
                    .ThenInclude(m => m.User)
                .Include(p => p.Tasks.Where(t => !t.IsDeleted))
                .Where(p => p.Id == id && (p.OwnerId == currentUserId || p.Members.Any(m => m.UserId == currentUserId && !m.IsDeleted)))
                .FirstOrDefaultAsync();

            if (project == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("project.not.found", new List<string> { _localizer["Project not found"] }));
            }

            var projectDto = new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                Status = project.Status,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Color = project.Color,
                OwnerId = project.OwnerId,
                OwnerName = project.Owner.FullName,
                MemberCount = project.Members.Count(m => !m.IsDeleted),
                TaskCount = project.TaskCount,
                CompletedTaskCount = project.CompletedTaskCount,
                CompletionPercentage = project.CompletionPercentage,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt
            };

            return Ok(ApiResponse<ProjectDto>.SuccessResult(projectDto, "project.retrieved.success", _localizer["Project retrieved successfully"]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving project {ProjectId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("internal.server.error", new List<string> { _localizer["An error occurred"] }));
        }
    }

    /// <summary>
    /// Create a new project
    /// </summary>
    /// <param name="request">Project creation details</param>
    /// <returns>Created project</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.ErrorResult(errors, "validation.failed", _localizer["Validation failed"]));
            }

            var currentUserId = GetCurrentUserId();

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Status = ProjectStatus.Planning,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Color = request.Color,
                OwnerId = currentUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Projects.Add(project);

            // Create activity log
            var activityLog = ActivityLog.CreateProjectActivity(
                currentUserId, 
                project.Id, 
                ActivityType.ProjectCreated, 
                $"Project '{project.Name}' created");
            
            _context.ActivityLogs.Add(activityLog);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Project {ProjectId} created by user {UserId}", project.Id, currentUserId);

            // Reload project with navigation properties
            var createdProject = await _context.Projects
                .Include(p => p.Owner)
                .FirstAsync(p => p.Id == project.Id);

            var projectDto = new ProjectDto
            {
                Id = createdProject.Id,
                Name = createdProject.Name,
                Description = createdProject.Description,
                Status = createdProject.Status,
                StartDate = createdProject.StartDate,
                EndDate = createdProject.EndDate,
                Color = createdProject.Color,
                OwnerId = createdProject.OwnerId,
                OwnerName = createdProject.Owner.FullName,
                MemberCount = 0,
                TaskCount = 0,
                CompletedTaskCount = 0,
                CompletionPercentage = 0,
                CreatedAt = createdProject.CreatedAt,
                UpdatedAt = createdProject.UpdatedAt
            };

            return CreatedAtAction(nameof(GetProject), new { id = project.Id }, 
                ApiResponse<ProjectDto>.SuccessResult(projectDto, "project.created.success", _localizer["Project created successfully"]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating project");
            return StatusCode(500, ApiResponse<object>.ErrorResult("internal.server.error", new List<string> { _localizer["An error occurred"] }));
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }
}

// DTOs
public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Color { get; set; }
}