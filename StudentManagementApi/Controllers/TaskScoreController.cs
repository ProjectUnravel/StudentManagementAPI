using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementApi.Data;
using StudentManagementApi.DTOs;
using StudentManagementApi.Models;
using StudentManagementApi.Services;

namespace StudentManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaskScoreController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TaskScoreController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<TaskScoreDto>>>> GetTaskScores(
        string? search = null,
        int page = 1,
        int pageSize = 10,
        string? sortBy = "createdAt",
        string? sortOrder = "desc",
        Guid? taskId = null,
        Guid? studentId = null)
    {
        try
        {
            var query = _context.TaskScores
                .Include(ts => ts.Task)
                    .ThenInclude(t => t.Course)
                .Include(ts => ts.Student)
                .AsQueryable();

            // Filter by task
            if (taskId.HasValue)
            {
                query = query.Where(ts => ts.TaskId == taskId.Value);
            }

            // Filter by student
            if (studentId.HasValue)
            {
                query = query.Where(ts => ts.StudentId == studentId.Value);
            }

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(ts => ts.Task.Title.Contains(search) ||
                                         ts.Student.FirstName.Contains(search) ||
                                         ts.Student.LastName.Contains(search) ||
                                         ts.Student.Email.Contains(search));
            }

            // Sorting
            query = sortBy?.ToLower() switch
            {
                "score" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(ts => ts.Score)
                    : query.OrderBy(ts => ts.Score),
                "tasktitle" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(ts => ts.Task.Title)
                    : query.OrderBy(ts => ts.Task.Title),
                "studentname" => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(ts => ts.Student.LastName)
                        .ThenByDescending(ts => ts.Student.FirstName)
                    : query.OrderBy(ts => ts.Student.LastName)
                        .ThenBy(ts => ts.Student.FirstName),
                _ => sortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(ts => ts.CreatedAt)
                    : query.OrderBy(ts => ts.CreatedAt)
            };

            // Count for pagination
            var totalCount = await query.CountAsync();

            // Pagination
            var taskScores = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ts => ts.ToDto())
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<TaskScoreDto>>.Ok(taskScores, "Task scores retrieved successfully", 
                MetaData.Create(page, pageSize, totalCount)));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<IEnumerable<TaskScoreDto>>.Fail("An error occurred while retrieving task scores", 500));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<TaskScoreDto>>> GetTaskScore(Guid id)
    {
        try
        {
            var taskScore = await _context.TaskScores
                .Include(ts => ts.Task)
                    .ThenInclude(t => t.Course)
                .Include(ts => ts.Student)
                .FirstOrDefaultAsync(ts => ts.Id == id);

            if (taskScore == null)
            {
                return NotFound(ApiResponse<TaskScoreDto>.NotFound("Task score not found"));
            }

            return Ok(ApiResponse<TaskScoreDto>.Ok(taskScore.ToDto(), "Task score retrieved successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<TaskScoreDto>.Fail("An error occurred while retrieving the task score", 500));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TaskScoreDto>>> CreateTaskScore(CreateTaskScoreDto createTaskScoreDto)
    {
        try
        {
            // Validate that the task exists
            var task = await _context.Tasks.FindAsync(createTaskScoreDto.TaskId);
            if (task == null)
            {
                return BadRequest(ApiResponse<TaskScoreDto>.Fail("The specified task does not exist"));
            }

            // Validate that the student exists
            var student = await _context.Students.FindAsync(createTaskScoreDto.StudentId);
            if (student == null)
            {
                return BadRequest(ApiResponse<TaskScoreDto>.Fail("The specified student does not exist"));
            }

            // Validate that the student is enrolled in the course tied to the task
            var isStudentEnrolled = await _context.CourseRegistrations
                .AnyAsync(cr => cr.StudentId == createTaskScoreDto.StudentId && cr.CourseId == task.CourseId);

            if (!isStudentEnrolled)
            {
                return BadRequest(ApiResponse<TaskScoreDto>.Fail("The student is not enrolled in the course tied to this task"));
            }

            // Validate that the score doesn't exceed the maximum obtainable score
            if (createTaskScoreDto.Score > task.MaxObtainableScore)
            {
                return BadRequest(ApiResponse<TaskScoreDto>.Fail($"The score cannot exceed the maximum obtainable score of {task.MaxObtainableScore}"));
            }

            // Check if a score already exists for this student and task (handled by unique constraint, but provide better error message)
            var existingScore = await _context.TaskScores
                .AnyAsync(ts => ts.TaskId == createTaskScoreDto.TaskId && ts.StudentId == createTaskScoreDto.StudentId);

            if (existingScore)
            {
                return BadRequest(ApiResponse<TaskScoreDto>.Fail("A score already exists for this student and task. Use PUT to update the existing score."));
            }

            var taskScore = createTaskScoreDto.ToEntity();
            _context.TaskScores.Add(taskScore);
            await _context.SaveChangesAsync();

            // Reload with related data
            var createdTaskScore = await _context.TaskScores
                .Include(ts => ts.Task)
                    .ThenInclude(t => t.Course)
                .Include(ts => ts.Student)
                .FirstOrDefaultAsync(ts => ts.Id == taskScore.Id);

            return CreatedAtAction(nameof(GetTaskScore), new { id = taskScore.Id },
                ApiResponse<TaskScoreDto>.Created(createdTaskScore!.ToDto(), "Task score created successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<TaskScoreDto>.Fail("An error occurred while creating the task score", 500));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<TaskScoreDto>>> UpdateTaskScore(Guid id, UpdateTaskScoreDto updateTaskScoreDto)
    {
        try
        {
            var taskScore = await _context.TaskScores
                .Include(ts => ts.Task)
                .FirstOrDefaultAsync(ts => ts.Id == id);

            if (taskScore == null)
            {
                return NotFound(ApiResponse<TaskScoreDto>.NotFound("Task score not found"));
            }

            // Validate that the score doesn't exceed the maximum obtainable score
            if (updateTaskScoreDto.Score > taskScore.Task.MaxObtainableScore)
            {
                return BadRequest(ApiResponse<TaskScoreDto>.Fail($"The score cannot exceed the maximum obtainable score of {taskScore.Task.MaxObtainableScore}"));
            }

            updateTaskScoreDto.UpdateEntity(taskScore);
            await _context.SaveChangesAsync();

            // Reload with related data
            var updatedTaskScore = await _context.TaskScores
                .Include(ts => ts.Task)
                    .ThenInclude(t => t.Course)
                .Include(ts => ts.Student)
                .FirstOrDefaultAsync(ts => ts.Id == id);

            return Ok(ApiResponse<TaskScoreDto>.Ok(updatedTaskScore!.ToDto(), "Task score updated successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<TaskScoreDto>.Fail("An error occurred while updating the task score", 500));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteTaskScore(Guid id)
    {
        try
        {
            var taskScore = await _context.TaskScores.FindAsync(id);
            if (taskScore == null)
            {
                return NotFound(ApiResponse<object>.NotFound("Task score not found"));
            }

            _context.TaskScores.Remove(taskScore);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok("Task score deleted successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail("An error occurred while deleting the task score", 500));
        }
    }
}