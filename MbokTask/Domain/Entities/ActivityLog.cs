using MbokTask.Domain.Enums;

namespace MbokTask.Domain.Entities;

public class ActivityLog
{
    public Guid Id { get; set; }
    public ActivityType ActivityType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Foreign keys
    public Guid UserId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? ProjectId { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual TaskItem? Task { get; set; }
    public virtual Project? Project { get; set; }

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