namespace MbokTask.Domain.Entities;

public class TaskComment
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    
    // Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    
    // Foreign keys
    public Guid TaskId { get; set; }
    public Guid AuthorId { get; set; }
    
    // Navigation properties
    public virtual TaskItem Task { get; set; } = null!;
    public virtual User Author { get; set; } = null!;

    public void UpdateContent(string newContent)
    {
        Content = newContent;
        UpdatedAt = DateTime.UtcNow;
    }
}