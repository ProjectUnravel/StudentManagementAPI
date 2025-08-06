using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementApi.Data;
using StudentManagementApi.DTOs;
using StudentManagementApi.Extensions;
using StudentManagementApi.Models;
using StudentManagementApi.Models.Entities;
using StudentManagementApi.Services;

namespace StudentManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CoursesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Courses
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CourseDto>>>> GetCourses([FromQuery] PaginationRequest pagination)
    {
        var query = _context.Courses.Include(x => x.CourseRegistrations).AsNoTracking();

        // Apply search filter
        if (!string.IsNullOrEmpty(pagination.Search))
        {
            query = query.Where(c => c.CourseCode.Contains(pagination.Search) ||
                                   c.CourseTitle.Contains(pagination.Search));
        }

        // Apply sorting
        query = pagination.SortBy?.ToLower() switch
        {
            "coursecode" => pagination.SortDescending 
                ? query.OrderByDescending(c => c.CourseCode)
                : query.OrderBy(c => c.CourseCode),
            "coursetitle" => pagination.SortDescending 
                ? query.OrderByDescending(c => c.CourseTitle)
                : query.OrderBy(c => c.CourseTitle),
            "createdat" => pagination.SortDescending 
                ? query.OrderByDescending(c => c.CreatedAt)
                : query.OrderBy(c => c.CreatedAt),
            _ => query.OrderBy(c => c.CourseCode)
        };

        var (courses, totalCount) = await query.ToPaginatedListAsync(pagination);
        var courseDtos = courses.Select(c => c.ToDto()).ToList();
        var metaData = MetaData.Create(pagination.PageIndex, pagination.PageSize, totalCount);

        var response = ApiResponse<List<CourseDto>>.Ok(courseDtos, "Courses retrieved successfully", metaData);
        return Ok(response);
    }

    // GET: api/Courses/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CourseDto>>> GetCourse(Guid id)
    {
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
        {
            var notFoundResponse = ApiResponse<CourseDto>.NotFound("Course not found");
            return NotFound(notFoundResponse);
        }

        var courseDto = course.ToDto();
        var response = ApiResponse<CourseDto>.Ok(courseDto, "Course retrieved successfully");
        return Ok(response);
    }

    // GET: api/Courses/students/5
    [HttpGet("students/{courseId}")]
    public async Task<ActionResult<ApiResponse<List<StudentDto>>>> GetStudentOfferingCourse(Guid courseId)
    {
        var students = await _context.CourseRegistrations.AsNoTracking().Where(x => x.CourseId == courseId).Select(x => new StudentDto()
        {
            Id = x.StudentId,
            FirstName = x.Student.FirstName,
            LastName = x.Student.LastName,
            Email = x.Student.Email,
            PhoneNumber = x.Student.PhoneNumber,
            Gender = x.Student.Gender,
            CreatedAt = x.Student.CreatedAt
        }).ToListAsync();

        var response = ApiResponse<List<StudentDto>>.Ok(students, "students offering course retrieved", null);
        return Ok(response);
    }

    // POST: api/Courses
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CourseDto>>> PostCourse(CreateCourseDto courseDto)
    {
        try
        {
            var course = courseDto.ToEntity();
            course.Id = Guid.NewGuid();
            course.CreatedAt = DateTime.UtcNow;

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var createdCourseDto = course.ToDto();
            var response = ApiResponse<CourseDto>.Created(createdCourseDto, "Course created successfully");
            return CreatedAtAction("GetCourse", new { id = course.Id }, response);
        }
        catch (DbUpdateException)
        {
            var errorResponse = ApiResponse<CourseDto>.Fail("Course with this code already exists", 409);
            return Conflict(errorResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse<CourseDto>.Fail($"An error occurred: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    // PUT: api/Courses/5
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<CourseDto>>> PutCourse(Guid id, UpdateCourseDto courseDto)
    {
        try
        {
            var existingCourse = await _context.Courses.FindAsync(id);
            if (existingCourse == null)
            {
                var notFoundResponse = ApiResponse<CourseDto>.NotFound("Course not found");
                return NotFound(notFoundResponse);
            }

            // Update only the allowed fields
            courseDto.UpdateEntity(existingCourse);

            await _context.SaveChangesAsync();

            var updatedCourseDto = existingCourse.ToDto();
            var response = ApiResponse<CourseDto>.Ok(updatedCourseDto, "Course updated successfully");
            return Ok(response);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CourseExists(id))
            {
                var notFoundResponse = ApiResponse<CourseDto>.NotFound("Course not found");
                return NotFound(notFoundResponse);
            }
            else
            {
                var errorResponse = ApiResponse<CourseDto>.Fail("Concurrency error occurred", 409);
                return Conflict(errorResponse);
            }
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse<CourseDto>.Fail($"An error occurred: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    // DELETE: api/Courses/5
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCourse(Guid id)
    {
        try
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                var notFoundResponse = ApiResponse<object>.NotFound("Course not found");
                return NotFound(notFoundResponse);
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            var response = ApiResponse<object>.Ok("Course deleted successfully");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    private bool CourseExists(Guid id)
    {
        return _context.Courses.Any(e => e.Id == id);
    }
}