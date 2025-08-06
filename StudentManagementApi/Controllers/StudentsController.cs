using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementApi.Data;
using StudentManagementApi.Models;
using StudentManagementApi.DTOs;
using StudentManagementApi.Services;
using StudentManagementApi.Extensions;

namespace StudentManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public StudentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Students
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<StudentDto>>>> GetStudents([FromQuery] PaginationRequest pagination)
    {
        var query = _context.Students.AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(pagination.Search))
        {
            query = query.Where(s => s.FirstName.Contains(pagination.Search) ||
                                   s.LastName.Contains(pagination.Search) ||
                                   s.Email.Contains(pagination.Search));
        }

        // Apply sorting
        query = pagination.SortBy?.ToLower() switch
        {
            "firstname" => pagination.SortDescending 
                ? query.OrderByDescending(s => s.FirstName)
                : query.OrderBy(s => s.FirstName),
            "lastname" => pagination.SortDescending 
                ? query.OrderByDescending(s => s.LastName)
                : query.OrderBy(s => s.LastName),
            "email" => pagination.SortDescending 
                ? query.OrderByDescending(s => s.Email)
                : query.OrderBy(s => s.Email),
            "createdat" => pagination.SortDescending 
                ? query.OrderByDescending(s => s.CreatedAt)
                : query.OrderBy(s => s.CreatedAt),
            _ => query.OrderBy(s => s.FirstName)
        };

        var (students, totalCount) = await query.ToPaginatedListAsync(pagination);
        var studentDtos = students.Select(s => s.ToDto()).ToList();
        var metaData = MetaData.Create(pagination.PageIndex, pagination.PageSize, totalCount);

        var response = ApiResponse<List<StudentDto>>.Ok(studentDtos, "Students retrieved successfully", metaData);
        return Ok(response);
    }

    // GET: api/Students/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<StudentDto>>> GetStudent(Guid id)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student == null)
        {
            var notFoundResponse = ApiResponse<StudentDto>.NotFound("Student not found");
            return NotFound(notFoundResponse);
        }

        var studentDto = student.ToDto();
        var response = ApiResponse<StudentDto>.Ok(studentDto, "Student retrieved successfully");
        return Ok(response);
    }

    // POST: api/Students
    [HttpPost]
    public async Task<ActionResult<ApiResponse<StudentDto>>> PostStudent(CreateStudentDto studentDto)
    {
        try
        {
            var student = studentDto.ToEntity();
            student.Id = Guid.NewGuid();
            student.CreatedAt = DateTime.UtcNow;

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            var createdStudentDto = student.ToDto();
            var response = ApiResponse<StudentDto>.Created(createdStudentDto, "Student created successfully");
            return CreatedAtAction("GetStudent", new { id = student.Id }, response);
        }
        catch (DbUpdateException)
        {
            var errorResponse = ApiResponse<StudentDto>.Fail("Student with this email already exists", 409);
            return Conflict(errorResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse<StudentDto>.Fail($"An error occurred: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    // PUT: api/Students/5
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<StudentDto>>> PutStudent(Guid id, UpdateStudentDto studentDto)
    {
        try
        {
            var existingStudent = await _context.Students.FindAsync(id);
            if (existingStudent == null)
            {
                var notFoundResponse = ApiResponse<StudentDto>.NotFound("Student not found");
                return NotFound(notFoundResponse);
            }

            // Update only the allowed fields
            studentDto.UpdateEntity(existingStudent);

            await _context.SaveChangesAsync();

            var updatedStudentDto = existingStudent.ToDto();
            var response = ApiResponse<StudentDto>.Ok(updatedStudentDto, "Student updated successfully");
            return Ok(response);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!StudentExists(id))
            {
                var notFoundResponse = ApiResponse<StudentDto>.NotFound("Student not found");
                return NotFound(notFoundResponse);
            }
            else
            {
                var errorResponse = ApiResponse<StudentDto>.Fail("Concurrency error occurred", 409);
                return Conflict(errorResponse);
            }
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse<StudentDto>.Fail($"An error occurred: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    // DELETE: api/Students/5
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteStudent(Guid id)
    {
        try
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                var notFoundResponse = ApiResponse<object>.NotFound("Student not found");
                return NotFound(notFoundResponse);
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            var response = ApiResponse<object>.Ok("Student deleted successfully");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    private bool StudentExists(Guid id)
    {
        return _context.Students.Any(e => e.Id == id);
    }
}