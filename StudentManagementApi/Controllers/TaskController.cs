using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementApi.Data;
using StudentManagementApi.DTOs;
using StudentManagementApi.Models;
using StudentManagementApi.Services;
using Serilog;

namespace StudentManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaskController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly Serilog.ILogger _logger = Log.ForContext<TaskController>();

    public TaskController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<TaskDto>>>> GetTasks(
        string? search = null,
        int page = 1,
        int pageSize = 10,
        string? sortBy = "createdAt",
        string? sortOrder = "desc")
    {
        _logger.Information("Retrieving tasks with search: {Search}, page: {Page}, pageSize: {PageSize}, sortBy: {SortBy}, sortOrder: {SortOrder}", 
            search ?? "None", page, pageSize, sortBy, sortOrder);
        
        try
        {
            var query = _context.Tasks
                .Include(t => t.Course)
                .Include(t => t.TaskScores)
                .AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => t.Title.Contains(search) ||
                                        (t.Description != null && t.Description.Contains(search)) ||
                                        t.Course.CourseTitle.Contains(search));
            }

            // Sorting
            query = sortBy?.ToLower() switch
            {
                "title" => sortOrder?.ToLower() == "desc" 
                    ? query.OrderByDescending(t => t.Title)
                    : query.OrderBy(t => t.Title),
                "coursetitle" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.Course.CourseTitle)
                    : query.OrderBy(t => t.Course.CourseTitle),
                "maxscore" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.MaxObtainableScore)
                    : query.OrderBy(t => t.MaxObtainableScore),
                _ => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(t => t.CreatedAt)
                    : query.OrderBy(t => t.CreatedAt)
            };

            // Count for pagination
            var totalCount = await query.CountAsync();

            // Pagination
            var tasks = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => t.ToDto())
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<TaskDto>>.Ok(tasks, "Tasks retrieved successfully", 
                MetaData.Create(page, pageSize, totalCount)));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<TaskDto>>.Fail("An error occurred while retrieving tasks", 500));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> GetTask(Guid id)
    {
        try
        {
            var task = await _context.Tasks
                .Include(t => t.Course)
                .Include(t => t.TaskScores)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound(ApiResponse<TaskDto>.NotFound("Task not found"));
            }

            return Ok(ApiResponse<TaskDto>.Ok(task.ToDto(), "Task retrieved successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<TaskDto>.Fail("An error occurred while retrieving the task", 500));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TaskDto>>> CreateTask(CreateTaskDto createTaskDto)
    {
        try
        {
            // Validate that the course exists
            var courseExists = await _context.Courses.AnyAsync(c => c.Id == createTaskDto.CourseId);
            if (!courseExists)
            {
                return BadRequest(ApiResponse<TaskDto>.Fail("The specified course does not exist"));
            }

            var task = createTaskDto.ToEntity();
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Reload with related data
            var createdTask = await _context.Tasks
                .Include(t => t.Course)
                .Include(t => t.TaskScores)
                .FirstOrDefaultAsync(t => t.Id == task.Id);

            return CreatedAtAction(nameof(GetTask), new { id = task.Id },
                ApiResponse<TaskDto>.Created(createdTask!.ToDto(), "Task created successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<TaskDto>.Fail("An error occurred while creating the task", 500));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<TaskDto>>> UpdateTask(Guid id, UpdateTaskDto updateTaskDto)
    {
        try
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound(ApiResponse<TaskDto>.NotFound("Task not found"));
            }

            updateTaskDto.UpdateEntity(task);
            await _context.SaveChangesAsync();

            // Reload with related data
            var updatedTask = await _context.Tasks
                .Include(t => t.Course)
                .Include(t => t.TaskScores)
                .FirstOrDefaultAsync(t => t.Id == id);

            return Ok(ApiResponse<TaskDto>.Ok(updatedTask!.ToDto(), "Task updated successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<TaskDto>.Fail("An error occurred while updating the task", 500));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteTask(Guid id)
    {
        try
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound(ApiResponse<object>.NotFound("Task not found"));
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok("Task deleted successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("An error occurred while deleting the task", 500));
        }
    }
}