using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MbokTask.Domain.Entities;

namespace MbokTask.Infrastructure.Persistence.Context;

public class TaskManagementDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public TaskManagementDbContext(DbContextOptions<TaskManagementDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<TaskComment> TaskComments { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Identity tables
        builder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ProfilePictureUrl).HasMaxLength(500);
            entity.Property(e => e.PreferredLanguage).HasMaxLength(10);
            entity.Property(e => e.RowVersion).IsRowVersion();
            
            // Indexes
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.UserName).IsUnique();
            entity.HasIndex(e => new { e.IsDeleted, e.IsActive });
        });

        builder.Entity<IdentityRole<Guid>>(entity =>
        {
            entity.ToTable("Roles");
        });

        builder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("UserRoles");
        });

        builder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        builder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("UserLogins");
        });

        builder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });

        builder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("UserTokens");
        });

        // Configure Project entity
        builder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Color).HasMaxLength(7);
            entity.Property(e => e.RowVersion).IsRowVersion();
            
            // Relationships
            entity.HasOne(e => e.Owner)
                  .WithMany(e => e.OwnedProjects)
                  .HasForeignKey(e => e.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => new { e.IsDeleted, e.IsActive });
            entity.HasIndex(e => e.OwnerId);
        });

        // Configure TaskItem entity
        builder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.RowVersion).IsRowVersion();
            
            // Relationships
            entity.HasOne(e => e.Project)
                  .WithMany(e => e.Tasks)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.CreatedBy)
                  .WithMany(e => e.CreatedTasks)
                  .HasForeignKey(e => e.CreatedById)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasOne(e => e.AssignedTo)
                  .WithMany(e => e.AssignedTasks)
                  .HasForeignKey(e => e.AssignedToId)
                  .OnDelete(DeleteBehavior.SetNull);
            
            // Indexes
            entity.HasIndex(e => new { e.IsDeleted, e.IsActive });
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.AssignedToId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DueDate);
        });

        // Configure ProjectMember entity
        builder.Entity<ProjectMember>(entity =>
        {
            entity.ToTable("ProjectMembers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RowVersion).IsRowVersion();
            
            // Relationships
            entity.HasOne(e => e.Project)
                  .WithMany(e => e.Members)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.User)
                  .WithMany(e => e.ProjectMemberships)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes
            entity.HasIndex(e => new { e.ProjectId, e.UserId }).IsUnique();
            entity.HasIndex(e => new { e.IsDeleted, e.IsActive });
        });

        // Configure TaskComment entity
        builder.Entity<TaskComment>(entity =>
        {
            entity.ToTable("TaskComments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.RowVersion).IsRowVersion();
            
            // Relationships
            entity.HasOne(e => e.Task)
                  .WithMany(e => e.Comments)
                  .HasForeignKey(e => e.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Author)
                  .WithMany(e => e.Comments)
                  .HasForeignKey(e => e.AuthorId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes
            entity.HasIndex(e => new { e.IsDeleted, e.IsActive });
            entity.HasIndex(e => e.TaskId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure ActivityLog entity
        builder.Entity<ActivityLog>(entity =>
        {
            entity.ToTable("ActivityLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
            entity.Property(e => e.OldValue).HasMaxLength(1000);
            entity.Property(e => e.NewValue).HasMaxLength(1000);
            entity.Property(e => e.Metadata).HasMaxLength(2000);
            
            // Relationships
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Activities)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasOne(e => e.Task)
                  .WithMany(e => e.ActivityLogs)
                  .HasForeignKey(e => e.TaskId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Project)
                  .WithMany(e => e.ActivityLogs)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TaskId);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ActivityType);
        });

        // Global query filters for soft delete
        builder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Project>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<TaskItem>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ProjectMember>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<TaskComment>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Property("CreatedAt").CurrentValue == null)
                    entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
                    
                if (entry.Property("UpdatedAt").CurrentValue == null)
                    entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                    
                if (entry.Property("IsActive").CurrentValue == null)
                    entry.Property("IsActive").CurrentValue = true;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
            }
        }
    }
}