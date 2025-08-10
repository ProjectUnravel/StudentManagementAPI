using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementApi.Data;
using StudentManagementApi.Models;
using StudentManagementApi.DTOs;
using StudentManagementApi.Services;
using StudentManagementApi.Extensions;
using Serilog;

namespace StudentManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly Serilog.ILogger _logger = Log.ForContext<StudentsController>();

    public StudentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Students
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<StudentDto>>>> GetStudents([FromQuery] PaginationRequest pagination)
    {
        _logger.Information("Retrieving students with pagination: {@Pagination}", pagination);
        
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

        _logger.Information("Successfully retrieved {StudentCount} students out of {TotalCount} with search: {SearchTerm}", 
            studentDtos.Count, totalCount, pagination.Search ?? "None");
        
        var response = ApiResponse<List<StudentDto>>.Ok(studentDtos, "Students retrieved successfully", metaData);
        return Ok(response);
    }

    // GET: api/Students/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<StudentDto>>> GetStudent(Guid id)
    {
        _logger.Information("Retrieving student with ID: {StudentId}", id);
        
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Id == id);

        if (student == null)
        {
            _logger.Warning("Student not found with ID: {StudentId}", id);
            var notFoundResponse = ApiResponse<StudentDto>.NotFound("Student not found");
            return NotFound(notFoundResponse);
        }

        _logger.Information("Successfully retrieved student: {StudentEmail}", student.Email);
        var studentDto = student.ToDto();
        var response = ApiResponse<StudentDto>.Ok(studentDto, "Student retrieved successfully");
        return Ok(response);
    }

    // POST: api/Students
    [HttpPost]
    public async Task<ActionResult<ApiResponse<StudentDto>>> PostStudent(CreateStudentDto studentDto)
    {
        _logger.Information("Creating new student with email: {StudentEmail}", studentDto.Email);
        
        try
        {
            var student = studentDto.ToEntity();
            student.Id = Guid.NewGuid();
            student.CreatedAt = DateTime.UtcNow;

            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            
            _logger.Information("Successfully created student with ID: {StudentId}, Email: {StudentEmail}", 
                student.Id, student.Email);

            var createdStudentDto = student.ToDto();
            var response = ApiResponse<StudentDto>.Created(createdStudentDto, "Student created successfully");
            return CreatedAtAction("GetStudent", new { id = student.Id }, response);
        }
        catch (DbUpdateException ex)
        {
            _logger.Warning(ex, "Failed to create student - email already exists: {StudentEmail}", studentDto.Email);
            var errorResponse = ApiResponse<StudentDto>.Fail("Student with this email already exists", 409);
            return Conflict(errorResponse);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error creating student: {StudentEmail}", studentDto.Email);
            var errorResponse = ApiResponse<StudentDto>.Fail($"An error occurred: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    // PUT: api/Students/5
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<StudentDto>>> PutStudent(Guid id, UpdateStudentDto studentDto)
    {
        _logger.Information("Updating student with ID: {StudentId}", id);
        
        try
        {
            var existingStudent = await _context.Students.FindAsync(id);
            if (existingStudent == null)
            {
                _logger.Warning("Student not found for update with ID: {StudentId}", id);
                var notFoundResponse = ApiResponse<StudentDto>.NotFound("Student not found");
                return NotFound(notFoundResponse);
            }

            _logger.Information("Updating student details for: {StudentEmail}", existingStudent.Email);
            
            // Update only the allowed fields
            studentDto.UpdateEntity(existingStudent);

            await _context.SaveChangesAsync();
            
            _logger.Information("Successfully updated student: {StudentId}", id);

            var updatedStudentDto = existingStudent.ToDto();
            var response = ApiResponse<StudentDto>.Ok(updatedStudentDto, "Student updated successfully");
            return Ok(response);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.Warning(ex, "Concurrency error updating student: {StudentId}", id);
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
            _logger.Error(ex, "Unexpected error updating student: {StudentId}", id);
            var errorResponse = ApiResponse<StudentDto>.Fail($"An error occurred: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    // DELETE: api/Students/5
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteStudent(Guid id)
    {
        _logger.Information("Deleting student with ID: {StudentId}", id);
        
        try
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                _logger.Warning("Student not found for deletion with ID: {StudentId}", id);
                var notFoundResponse = ApiResponse<object>.NotFound("Student not found");
                return NotFound(notFoundResponse);
            }

            _logger.Information("Deleting student: {StudentEmail}", student.Email);
            
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            
            _logger.Information("Successfully deleted student with ID: {StudentId}", id);

            var response = ApiResponse<object>.Ok("Student deleted successfully");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error deleting student: {StudentId}", id);
            var errorResponse = ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    private bool StudentExists(Guid id)
    {
        return _context.Students.Any(e => e.Id == id);
    }
}