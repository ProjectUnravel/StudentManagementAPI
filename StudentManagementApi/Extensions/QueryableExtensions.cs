using Microsoft.EntityFrameworkCore;
using StudentManagementApi.Models;

namespace StudentManagementApi.Extensions;

public static class QueryableExtensions
{
    public static async Task<(List<T> Items, int TotalCount)> ToPaginatedListAsync<T>(
        this IQueryable<T> source, 
        PaginationRequest pagination)
    {
        var totalCount = await source.CountAsync();
        
        var items = await source
            .Skip((pagination.PageIndex - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public static async Task<ApiResponse<List<T>>> ToPaginatedResponseAsync<T>(
        this IQueryable<T> source,
        PaginationRequest pagination,
        string message = "Data retrieved successfully")
    {
        var (items, totalCount) = await source.ToPaginatedListAsync(pagination);
        var metaData = MetaData.Create(pagination.PageIndex, pagination.PageSize, totalCount);

        return ApiResponse<List<T>>.Ok(items, message, metaData);
    }
}