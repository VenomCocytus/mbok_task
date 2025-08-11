namespace MbokTask.Domain.Enums;

public enum TaskStatus
{
    ToDo = 0,
    InProgress = 1,
    Done = 2,
    Cancelled = 3,
    OnHold = 4
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum UserRole
{
    Member = 0,
    Manager = 1,
    Admin = 2
}

public enum ProjectStatus
{
    Planning = 0,
    Active = 1,
    OnHold = 2,
    Completed = 3,
    Cancelled = 4
}

public enum ActivityType
{
    TaskCreated = 0,
    TaskUpdated = 1,
    TaskAssigned = 2,
    TaskStatusChanged = 3,
    TaskDeleted = 4,
    CommentAdded = 5,
    ProjectCreated = 6,
    ProjectUpdated = 7,
    UserJoinedProject = 8,
    UserLeftProject = 9
}