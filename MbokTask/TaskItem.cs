using MbokTask.Domain.Enums;
using Ardalis.GuardClauses;
using TaskStatus = MbokTask.Domain.Enums.TaskStatus;

namespace MbokTask.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskStatus Status { get; set; } = TaskStatus.ToDo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int EstimatedHours { get; set; }
    public int ActualHours { get; set; }
    public string? Tags { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Foreign keys
    public Guid ProjectId { get; set; }
    public Guid CreatedById { get; set; }
    public Guid? AssignedToId { get; set; }
    
    // Navigation properties
    public virtual Project Project { get; set; } = null!;
    public virtual User CreatedBy { get; set; } = null!;
    public virtual User? AssignedTo { get; set; }
    public virtual ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    public void AssignTo(Guid userId)
    {
        Guard.Against.Default(userId, nameof(userId));
        AssignedToId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(TaskStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
        
        if (newStatus == TaskStatus.Done && CompletedAt == null)
        {
            CompletedAt = DateTime.UtcNow;
        }
        else if (newStatus != TaskStatus.Done)
        {
            CompletedAt = null;
        }
    }

    public void UpdatePriority(TaskPriority newPriority)
    {
        Priority = newPriority;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsOverdue => DueDate.HasValue && DueDate < DateTime.UtcNow && Status != TaskStatus.Done;
}