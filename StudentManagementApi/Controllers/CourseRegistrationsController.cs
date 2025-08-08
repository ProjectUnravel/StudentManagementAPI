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
public class CourseRegistrationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CourseRegistrationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/CourseRegistrations
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CourseRegistrationDto>>>> GetCourseRegistrations([FromQuery] PaginationRequest pagination)
    {
        var query = _context.CourseRegistrations.AsNoTracking();

        // Apply search filter
        if (!string.IsNullOrEmpty(pagination.Search))
        {
            query = query.Where(cr => cr.Student.FirstName.Contains(pagination.Search) ||
                                    cr.Student.LastName.Contains(pagination.Search) ||
                                    cr.Course.CourseCode.Contains(pagination.Search) ||
                                    cr.Course.CourseTitle.Contains(pagination.Search));
        }

        // Apply sorting
        query = pagination.SortBy?.ToLower() switch
        {
            "student" => pagination.SortDescending 
                ? query.OrderByDescending(cr => cr.Student.FirstName)
                : query.OrderBy(cr => cr.Student.FirstName),
            "course" => pagination.SortDescending 
                ? query.OrderByDescending(cr => cr.Course.CourseCode)
                : query.OrderBy(cr => cr.Course.CourseCode),
            "createdat" => pagination.SortDescending 
                ? query.OrderByDescending(cr => cr.CreatedAt)
                : query.OrderBy(cr => cr.CreatedAt),
            _ => query.OrderByDescending(cr => cr.CreatedAt)
        };

        query = query.Include(cr => cr.Student).Include(cr => cr.Course);

        var (registrations, totalCount) = await query.ToPaginatedListAsync(pagination);
        var registrationDtos = registrations.Select(cr => cr.ToDto()).ToList();
        var metaData = MetaData.Create(pagination.PageIndex, pagination.PageSize, totalCount);

        var response = ApiResponse<List<CourseRegistrationDto>>.Ok(registrationDtos, "Course registrations retrieved successfully", metaData);
        return Ok(response);
    }


    // GET: api/CourseRegistrations/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CourseRegistrationDto>>> GetCourseRegistration(Guid id)
    {
        var courseRegistration = await _context.CourseRegistrations
            .Include(cr => cr.Student)
            .Include(cr => cr.Course)
            .FirstOrDefaultAsync(cr => cr.Id == id);

        if (courseRegistration == null)
        {
            var notFoundResponse = ApiResponse<CourseRegistrationDto>.NotFound("Course registration not found");
            return NotFound(notFoundResponse);
        }

        var registrationDto = courseRegistration.ToDto();
        var response = ApiResponse<CourseRegistrationDto>.Ok(registrationDto, "Course registration retrieved successfully");
        return Ok(response);
    }

    // GET: api/CourseRegistrations/student/5
    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<ApiResponse<List<CourseRegistrationDto>>>> GetStudentRegistrations(Guid studentId, [FromQuery] PaginationRequest pagination)
    {
        var query = _context.CourseRegistrations
            .Include(cr => cr.Student)
            .Include(cr => cr.Course)
            .Where(cr => cr.StudentId == studentId)
            .OrderByDescending(cr => cr.CreatedAt);

        var (registrations, totalCount) = await query.ToPaginatedListAsync(pagination);
        var registrationDtos = registrations.Select(cr => cr.ToDto()).ToList();
        var metaData = MetaData.Create(pagination.PageIndex, pagination.PageSize, totalCount);

        var response = ApiResponse<List<CourseRegistrationDto>>.Ok(registrationDtos, "Student course registrations retrieved successfully", metaData);
        return Ok(response);
    }

    // GET: api/CourseRegistrations/course/5
    [HttpGet("course/{courseId}")]
    public async Task<ActionResult<ApiResponse<List<CourseRegistrationDto>>>> GetCourseRegistrations([FromQuery] PaginationRequest pagination, [FromRoute] Guid courseId)
    {
        var query = _context.CourseRegistrations.AsNoTracking().Where(x => x.CourseId == courseId);

        // Apply search filter
        if (!string.IsNullOrEmpty(pagination.Search))
        {
            query = query.Where(cr => cr.Student.FirstName.Contains(pagination.Search) ||
                                    cr.Student.LastName.Contains(pagination.Search) ||
                                    cr.Course.CourseCode.Contains(pagination.Search) ||
                                    cr.Course.CourseTitle.Contains(pagination.Search));
        }

        // Apply sorting
        query = pagination.SortBy?.ToLower() switch
        {
            "student" => pagination.SortDescending
                ? query.OrderByDescending(cr => cr.Student.FirstName)
                : query.OrderBy(cr => cr.Student.FirstName),
            "course" => pagination.SortDescending
                ? query.OrderByDescending(cr => cr.Course.CourseCode)
                : query.OrderBy(cr => cr.Course.CourseCode),
            "createdat" => pagination.SortDescending
                ? query.OrderByDescending(cr => cr.CreatedAt)
                : query.OrderBy(cr => cr.CreatedAt),
            _ => query.OrderByDescending(cr => cr.CreatedAt)
        };

        query = query.Include(s => s.Student).Include(c => c.Course);

        var (registrations, totalCount) = await query.ToPaginatedListAsync(pagination);
        var registrationDtos = registrations.Select(cr => cr.ToDto()).ToList();
        var metaData = MetaData.Create(pagination.PageIndex, pagination.PageSize, totalCount);

        var response = ApiResponse<List<CourseRegistrationDto>>.Ok(registrationDtos, "Course registrations retrieved successfully", metaData);
        return Ok(response);
    }

    // POST: api/CourseRegistrations
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CourseRegistrationDto>>> PostCourseRegistration(CreateCourseRegistrationDto registrationDto)
    {
        try
        {
            // Check if student exists
            var studentExists = await _context.Students.AnyAsync(s => s.Id == registrationDto.StudentId);
            if (!studentExists)
            {
                var studentNotFoundResponse = ApiResponse<CourseRegistrationDto>.NotFound("Student not found");
                return NotFound(studentNotFoundResponse);
            }

            // Check if course exists
            var courseExists = await _context.Courses.AnyAsync(c => c.Id == registrationDto.CourseId);
            if (!courseExists)
            {
                var courseNotFoundResponse = ApiResponse<CourseRegistrationDto>.NotFound("Course not found");
                return NotFound(courseNotFoundResponse);
            }

            // Check if student is already registered for this course
            var existingRegistration = await _context.CourseRegistrations
                .AnyAsync(cr => cr.StudentId == registrationDto.StudentId && cr.CourseId == registrationDto.CourseId);
            
            if (existingRegistration)
            {
                var conflictResponse = ApiResponse<CourseRegistrationDto>.Fail("Student is already registered for this course", 409);
                return Conflict(conflictResponse);
            }

            var courseRegistration = registrationDto.ToEntity();
            courseRegistration.Id = Guid.NewGuid();
            courseRegistration.CreatedAt = DateTime.UtcNow;

            _context.CourseRegistrations.Add(courseRegistration);
            await _context.SaveChangesAsync();

            // Load related entities
            await _context.Entry(courseRegistration)
                .Reference(cr => cr.Student)
                .LoadAsync();
            await _context.Entry(courseRegistration)
                .Reference(cr => cr.Course)
                .LoadAsync();

            var createdRegistrationDto = courseRegistration.ToDto();
            var response = ApiResponse<CourseRegistrationDto>.Created(createdRegistrationDto, "Course registration created successfully");
            return CreatedAtAction("GetCourseRegistration", new { id = courseRegistration.Id }, response);
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse<CourseRegistrationDto>.Fail($"An error occurred: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    // DELETE: api/CourseRegistrations/5
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCourseRegistration(Guid id)
    {
        try
        {
            var courseRegistration = await _context.CourseRegistrations.FindAsync(id);
            if (courseRegistration == null)
            {
                var notFoundResponse = ApiResponse<object>.NotFound("Course registration not found");
                return NotFound(notFoundResponse);
            }

            _context.CourseRegistrations.Remove(courseRegistration);
            await _context.SaveChangesAsync();

            var response = ApiResponse<object>.Ok("Course registration deleted successfully");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    private bool CourseRegistrationExists(Guid id)
    {
        return _context.CourseRegistrations.Any(e => e.Id == id);
    }
}
