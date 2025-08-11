using MbokTask.Domain.Enums;
using Ardalis.GuardClauses;
using TaskStatus = MbokTask.Domain.Enums.TaskStatus;

namespace MbokTask.Domain.Entities;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Color { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Foreign keys
    public Guid OwnerId { get; set; }
    
    // Navigation properties
    public virtual User Owner { get; set; } = null!;
    public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public virtual ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    public void AddMember(Guid userId, UserRole role = UserRole.Member)
    {
        Guard.Against.Default(userId, nameof(userId));
        
        if (Members.Any(m => m.UserId == userId && !m.IsDeleted))
            return;

        Members.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = Id,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        });
        
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveMember(Guid userId)
    {
        Guard.Against.Default(userId, nameof(userId));
        
        var member = Members.FirstOrDefault(m => m.UserId == userId && !m.IsDeleted);
        if (member != null)
        {
            member.IsDeleted = true;
            member.DeletedAt = DateTime.UtcNow;
            member.UpdatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateStatus(ProjectStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public int TaskCount => Tasks.Count(t => !t.IsDeleted);
    public int CompletedTaskCount => Tasks.Count(t => !t.IsDeleted && t.Status == TaskStatus.Done);
    public double CompletionPercentage => TaskCount == 0 ? 0 : (double)CompletedTaskCount / TaskCount * 100;
}