namespace MbokTask.Application.DTOs;

public class ApiResponse<T>
{
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public string LocalizedMessage { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public bool Success { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public PaginationInfo? Pagination { get; set; }

    public static ApiResponse<T> SuccessResult(T data, string message = "", string localizedMessage = "")
    {
        return new ApiResponse<T>
        {
            Data = data,
            Message = message,
            LocalizedMessage = localizedMessage,
            Success = true
        };
    }

    public static ApiResponse<T> ErrorResult(string message, List<string>? errors = null, string localizedMessage = "")
    {
        return new ApiResponse<T>
        {
            Message = message,
            LocalizedMessage = localizedMessage,
            Errors = errors ?? new List<string>(),
            Success = false
        };
    }

    public static ApiResponse<T> ErrorResult(List<string> errors, string message = "", string localizedMessage = "")
    {
        return new ApiResponse<T>
        {
            Message = message,
            LocalizedMessage = localizedMessage,
            Errors = errors,
            Success = false
        };
    }
}

public class PaginationInfo
{
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}