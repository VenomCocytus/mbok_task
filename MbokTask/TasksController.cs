using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using MbokTask.Domain.Entities;
using MbokTask.Domain.Enums;
using MbokTask.Infrastructure.Persistence.Context;
using MbokTask.Application.DTOs;
using Asp.Versioning;
using TaskStatus = MbokTask.Domain.Enums.TaskStatus;

namespace MbokTask.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly TaskManagementDbContext _context;
    private readonly IStringLocalizer<TasksController> _localizer;
    private readonly ILogger<TasksController> _logger;

    public TasksController(
        TaskManagementDbContext context,
        IStringLocalizer<TasksController> localizer,
        ILogger<TasksController> logger)
    {
        _context = context;
        _localizer = localizer;
        _logger = logger;
    }

    /// <summary>
    /// Get all tasks with pagination and filtering
    /// </summary>
    /// <param name="projectId">Filter by project ID</param>
    /// <param name="status">Filter by task status</param>
    /// <param name="assignedToId">Filter by assigned user ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>Paginated list of tasks</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TaskDto>>), 200)]
    public async Task<IActionResult> GetTasks(
        [FromQuery] Guid? projectId = null,
        [FromQuery] TaskStatus? status = null,
        [FromQuery] Guid? assignedToId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            
            var query = _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .Where(t => t.Project.Members.Any(m => m.UserId == currentUserId) || t.Project.OwnerId == currentUserId);

            if (projectId.HasValue)
                query = query.Where(t => t.ProjectId == projectId.Value);

            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);

            if (assignedToId.HasValue)
                query = query.Where(t => t.AssignedToId == assignedToId.Value);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var tasks = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TaskDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status,
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                    CompletedAt = t.CompletedAt,
                    EstimatedHours = t.EstimatedHours,
                    ActualHours = t.ActualHours,
                    Tags = t.Tags,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project.Name,
                    CreatedById = t.CreatedById,
                    CreatedByName = t.CreatedBy.FullName,
                    AssignedToId = t.AssignedToId,
                    AssignedToName = t.AssignedTo != null ? t.AssignedTo.FullName : null,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    IsOverdue = t.IsOverdue
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

            var response = ApiResponse<List<TaskDto>>.SuccessResult(tasks, "tasks.retrieved.success", _localizer["Tasks retrieved successfully"]);
            response.Pagination = pagination;

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving tasks");
            return StatusCode(500, ApiResponse<object>.ErrorResult("internal.server.error", new List<string> { _localizer["An error occurred"] }));
        }
    }

    /// <summary>
    /// Get a specific task by ID
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <returns>Task details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetTask(Guid id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            
            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .Include(t => t.Comments.Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.Author)
                .Where(t => t.Id == id && (t.Project.Members.Any(m => m.UserId == currentUserId) || t.Project.OwnerId == currentUserId))
                .FirstOrDefaultAsync();

            if (task == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("task.not.found", new List<string> { _localizer["Task not found"] }));
            }

            var taskDto = new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                DueDate = task.DueDate,
                CompletedAt = task.CompletedAt,
                EstimatedHours = task.EstimatedHours,
                ActualHours = task.ActualHours,
                Tags = task.Tags,
                ProjectId = task.ProjectId,
                ProjectName = task.Project.Name,
                CreatedById = task.CreatedById,
                CreatedByName = task.CreatedBy.FullName,
                AssignedToId = task.AssignedToId,
                AssignedToName = task.AssignedTo?.FullName,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                IsOverdue = task.IsOverdue,
                Comments = task.Comments.Select(c => new TaskCommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    AuthorId = c.AuthorId,
                    AuthorName = c.Author.FullName,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).ToList()
            };

            return Ok(ApiResponse<TaskDto>.SuccessResult(taskDto, "task.retrieved.success", _localizer["Task retrieved successfully"]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving task {TaskId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("internal.server.error", new List<string> { _localizer["An error occurred"] }));
        }
    }

    /// <summary>
    /// Create a new task
    /// </summary>
    /// <param name="request">Task creation details</param>
    /// <returns>Created task</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.ErrorResult(errors, "validation.failed", _localizer["Validation failed"]));
            }

            var currentUserId = GetCurrentUserId();

            // Verify user has access to the project
            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId && 
                    (p.OwnerId == currentUserId || p.Members.Any(m => m.UserId == currentUserId && !m.IsDeleted)));

            if (project == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("project.access.denied", new List<string> { _localizer["Access to project denied"] }));
            }

            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                Status = TaskStatus.ToDo,
                Priority = request.Priority,
                DueDate = request.DueDate,
                EstimatedHours = request.EstimatedHours,
                Tags = request.Tags,
                ProjectId = request.ProjectId,
                CreatedById = currentUserId,
                AssignedToId = request.AssignedToId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Tasks.Add(task);

            // Create activity log
            var activityLog = ActivityLog.CreateTaskActivity(
                currentUserId, 
                task.Id, 
                ActivityType.TaskCreated, 
                $"Task '{task.Title}' created");
            
            _context.ActivityLogs.Add(activityLog);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} created by user {UserId}", task.Id, currentUserId);

            // Reload task with navigation properties
            var createdTask = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .FirstAsync(t => t.Id == task.Id);

            var taskDto = new TaskDto
            {
                Id = createdTask.Id,
                Title = createdTask.Title,
                Description = createdTask.Description,
                Status = createdTask.Status,
                Priority = createdTask.Priority,
                DueDate = createdTask.DueDate,
                EstimatedHours = createdTask.EstimatedHours,
                Tags = createdTask.Tags,
                ProjectId = createdTask.ProjectId,
                ProjectName = createdTask.Project.Name,
                CreatedById = createdTask.CreatedById,
                CreatedByName = createdTask.CreatedBy.FullName,
                AssignedToId = createdTask.AssignedToId,
                AssignedToName = createdTask.AssignedTo?.FullName,
                CreatedAt = createdTask.CreatedAt,
                UpdatedAt = createdTask.UpdatedAt
            };

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, 
                ApiResponse<TaskDto>.SuccessResult(taskDto, "task.created.success", _localizer["Task created successfully"]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating task");
            return StatusCode(500, ApiResponse<object>.ErrorResult("internal.server.error", new List<string> { _localizer["An error occurred"] }));
        }
    }

    /// <summary>
    /// Update task status
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <param name="request">Status update request</param>
    /// <returns>Updated task</returns>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UpdateTaskStatus(Guid id, [FromBody] UpdateTaskStatusRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            
            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.CreatedBy)
                .Include(t => t.AssignedTo)
                .FirstOrDefaultAsync(t => t.Id == id && 
                    (t.Project.OwnerId == currentUserId || 
                     t.Project.Members.Any(m => m.UserId == currentUserId && !m.IsDeleted) ||
                     t.AssignedToId == currentUserId));

            if (task == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("task.not.found", new List<string> { _localizer["Task not found"] }));
            }

            var oldStatus = task.Status;
            task.UpdateStatus(request.Status);

            // Create activity log
            var activityLog = ActivityLog.CreateTaskActivity(
                currentUserId, 
                task.Id, 
                ActivityType.TaskStatusChanged, 
                $"Task status changed from {oldStatus} to {request.Status}",
                oldStatus.ToString(),
                request.Status.ToString());
            
            _context.ActivityLogs.Add(activityLog);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {TaskId} status updated to {Status} by user {UserId}", id, request.Status, currentUserId);

            var taskDto = new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                DueDate = task.DueDate,
                CompletedAt = task.CompletedAt,
                EstimatedHours = task.EstimatedHours,
                ActualHours = task.ActualHours,
                Tags = task.Tags,
                ProjectId = task.ProjectId,
                ProjectName = task.Project.Name,
                CreatedById = task.CreatedById,
                CreatedByName = task.CreatedBy.FullName,
                AssignedToId = task.AssignedToId,
                AssignedToName = task.AssignedTo?.FullName,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                IsOverdue = task.IsOverdue
            };

            return Ok(ApiResponse<TaskDto>.SuccessResult(taskDto, "task.status.updated.success", _localizer["Task status updated successfully"]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating task status for task {TaskId}", id);
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
public class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int EstimatedHours { get; set; }
    public int ActualHours { get; set; }
    public string? Tags { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public Guid? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsOverdue { get; set; }
    public List<TaskCommentDto> Comments { get; set; } = new();
}

public class TaskCommentDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDate { get; set; }
    public int EstimatedHours { get; set; }
    public string? Tags { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? AssignedToId { get; set; }
}

public class UpdateTaskStatusRequest
{
    public TaskStatus Status { get; set; }
}