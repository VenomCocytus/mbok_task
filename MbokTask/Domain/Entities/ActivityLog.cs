using MbokTask.Domain.Enums;

namespace MbokTask.Domain.Entities;

public sealed class ActivityLog
{
    public Guid Id { get; init; }
    public ActivityType ActivityType { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public string? Metadata { get; init; }
    public DateTime CreatedAt { get; init; }
    
    // Foreign keys
    public Guid UserId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? ProjectId { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public TaskItem? Task { get; set; }
    public Project? Project { get; set; }

    public static ActivityLog CreateTaskActivity(Guid userId, Guid taskId, ActivityType activityType, string description, string? oldValue = null, string? newValue = null)
    {
        return new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TaskId = taskId,
            ActivityType = activityType,
            Description = description,
            OldValue = oldValue,
            NewValue = newValue,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static ActivityLog CreateProjectActivity(Guid userId, Guid projectId, ActivityType activityType, string description, string? oldValue = null, string? newValue = null)
    {
        return new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProjectId = projectId,
            ActivityType = activityType,
            Description = description,
            OldValue = oldValue,
            NewValue = newValue,
            CreatedAt = DateTime.UtcNow
        };
    }
}