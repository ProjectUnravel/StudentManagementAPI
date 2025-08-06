using System.Net;

namespace StudentManagementApi.Models;

public class ApiResponse<T>
{
    public T? Results { get; set; }

    public bool Status { get; set; }

    public string? Message { get; set; }

    public MetaData? MetaData { get; set; }

    public int StatusCode { get; set; } = (int)HttpStatusCode.OK;

    public static ApiResponse<T> Ok(T data, string message, MetaData? metaData = default) => new() 
    { 
        MetaData = metaData, 
        Status = true, 
        Message = message, 
        Results = data, 
        StatusCode = (int)HttpStatusCode.OK
    };

    public static ApiResponse<T> Ok(string message) => new() 
    { 
        MetaData = default, 
        Status = true, 
        Message = message, 
        Results = default, 
        StatusCode = (int)HttpStatusCode.OK
    };

    public static ApiResponse<T> Fail(string message, int statusCode = (int)HttpStatusCode.BadRequest) => new() 
    { 
        Status = false, 
        Message = message, 
        StatusCode = statusCode
    };

    public static ApiResponse<T> NotFound(string message = "Resource not found") => new()
    {
        Status = false,
        Message = message,
        StatusCode = (int)HttpStatusCode.NotFound
    };

    public static ApiResponse<T> Created(T data, string message) => new()
    {
        Status = true,
        Message = message,
        Results = data,
        StatusCode = (int)HttpStatusCode.Created
    };
}

public class MetaData
{
    public int PageIndex { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }

    public string? Showing { get; init; }

    public static MetaData Create(int pageIndex, int pageSize, int totalCount)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var startItem = (pageIndex - 1) * pageSize + 1;
        var endItem = Math.Min(pageIndex * pageSize, totalCount);
        
        return new MetaData
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Showing = totalCount > 0 ? $"Showing {startItem} to {endItem} of {totalCount} entries" : "No entries found"
        };
    }
}