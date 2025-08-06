using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementApi.Data;
using StudentManagementApi.Models;
using StudentManagementApi.DTOs;
using StudentManagementApi.Services;
using StudentManagementApi.Extensions;
using StudentManagementApi.Models.Entities;

namespace StudentManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AttendanceController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Attendance
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AttendanceDto>>>> GetAttendances([FromQuery] PaginationRequest pagination)
    {
        var query = _context.Attendances
            .Include(a => a.Student)
            .Include(c => c.Course)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(pagination.Search))
        {
            query = query.Where(a => a.Student.FirstName.Contains(pagination.Search) ||
                                   a.Student.LastName.Contains(pagination.Search) ||
                                   a.Student.Email.Contains(pagination.Search));
        }

        // Apply sorting
        query = pagination.SortBy?.ToLower() switch
        {
            "student" => pagination.SortDescending 
                ? query.OrderByDescending(a => a.Student.FirstName)
                : query.OrderBy(a => a.Student.FirstName),
            "clockin" => pagination.SortDescending 
                ? query.OrderByDescending(a => a.ClockIn)
                : query.OrderBy(a => a.ClockIn),
            "clockout" => pagination.SortDescending 
                ? query.OrderByDescending(a => a.ClockOut)
                : query.OrderBy(a => a.ClockOut),
            "createdat" => pagination.SortDescending 
                ? query.OrderByDescending(a => a.CreatedAt)
                : query.OrderBy(a => a.CreatedAt),
            _ => query.OrderByDescending(a => a.CreatedAt)
        };

        var (attendances, totalCount) = await query.ToPaginatedListAsync(pagination);
        var attendanceDtos = attendances.Select(a => a.ToDto()).ToList();
        var metaData = MetaData.Create(pagination.PageIndex, pagination.PageSize, totalCount);

        var response = ApiResponse<List<AttendanceDto>>.Ok(attendanceDtos, "Attendance records retrieved successfully", metaData);
        return Ok(response);
    }

    // GET: api/Attendance/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AttendanceDto>>> GetAttendance(Guid id)
    {
        var attendance = await _context.Attendances
            .Include(a => a.Student)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attendance == null)
        {
            var notFoundResponse = ApiResponse<AttendanceDto>.NotFound("Attendance record not found");
            return NotFound(notFoundResponse);
        }

        var attendanceDto = attendance.ToDto();
        var response = ApiResponse<AttendanceDto>.Ok(attendanceDto, "Attendance record retrieved successfully");
        return Ok(response);
    }

    // GET: api/Attendance/student/5
    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<ApiResponse<List<AttendanceDto>>>> GetStudentAttendances(Guid studentId, [FromQuery] PaginationRequest pagination)
    {
        var query = _context.Attendances
            .Include(a => a.Student)
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.CreatedAt);

        var (attendances, totalCount) = await query.ToPaginatedListAsync(pagination);
        var attendanceDtos = attendances.Select(a => a.ToDto()).ToList();
        var metaData = MetaData.Create(pagination.PageIndex, pagination.PageSize, totalCount);

        var response = ApiResponse<List<AttendanceDto>>.Ok(attendanceDtos, "Student attendance records retrieved successfully", metaData);
        return Ok(response);
    }

    // POST: api/Attendance
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AttendanceDto>>> PostAttendance(CreateAttendanceDto attendanceDto)
    {
        try
        {
            // Check if student exists
            var studentExists = await _context.Students.AnyAsync(s => s.Id == attendanceDto.StudentId);
            if (!studentExists)
            {
                var notFoundResponse = ApiResponse<AttendanceDto>.NotFound("Student not found");
                return NotFound(notFoundResponse);
            }

            var attendance = attendanceDto.ToEntity();
            attendance.Id = Guid.NewGuid();
            attendance.CreatedAt = DateTime.UtcNow;

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            // Load the student information
            await _context.Entry(attendance)
                .Reference(a => a.Student)
                .LoadAsync();

            var createdAttendanceDto = attendance.ToDto();
            var response = ApiResponse<AttendanceDto>.Created(createdAttendanceDto, "Attendance record created successfully");
            return CreatedAtAction("GetAttendance", new { id = attendance.Id }, response);
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse<AttendanceDto>.Fail($"An error occurred: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    // POST: api/Attendance/clockin
    [HttpPost("clockin")]
    public async Task<ActionResult<ApiResponse<AttendanceDto>>> ClockIn([FromBody] ClockInRequestDto request)
    {
        try
        {
            // Check if student exists
            var studentExists = await _context.Students.AnyAsync(s => s.Id == request.StudentId);
            if (!studentExists)
            {
                var notFoundResponse = ApiResponse<AttendanceDto>.NotFound("Student not found");
                return NotFound(notFoundResponse);
            }

            // Check if student already has an active attendance record (clocked in but not clocked out)
            var activeAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.StudentId == request.StudentId && 
                                        a.ClockIn != null && a.ClockOut == null);

            if (activeAttendance != null)
            {
                var conflictResponse = ApiResponse<AttendanceDto>.Fail("Student is already clocked in", 409);
                return Conflict(conflictResponse);
            }

            var attendance = new Attendance
            {
                Id = Guid.NewGuid(),
                StudentId = request.StudentId,
                ClockIn = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CourseId = request.CourseId,
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            // Load the student information
            await _context.Entry(attendance)
                .Reference(a => a.Student)
                .LoadAsync();

            var attendanceDto = attendance.ToDto();
            var response = ApiResponse<AttendanceDto>.Created(attendanceDto, "Student clocked in successfully");
            return CreatedAtAction("GetAttendance", new { id = attendance.Id }, response);
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse<AttendanceDto>.Fail($"An error occurred: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    // POST: api/Attendance/clockout
    [HttpPost("clockout")]
    public async Task<ActionResult<ApiResponse<AttendanceDto>>> ClockOut([FromBody] ClockOutRequestDto request)
    {
        try
        {
            // Find the active attendance record for the student
            var activeAttendance = await _context.Attendances
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.StudentId == request.StudentId && a.CourseId == request.CourseId && 
                                        a.ClockIn != null && a.ClockOut == null);

            if (activeAttendance == null)
            {
                var notFoundResponse = ApiResponse<AttendanceDto>.NotFound("No active attendance record found for student");
                return NotFound(notFoundResponse);
            }

            activeAttendance.ClockOut = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var attendanceDto = activeAttendance.ToDto();
            var response = ApiResponse<AttendanceDto>.Ok(attendanceDto, "Student clocked out successfully");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse<AttendanceDto>.Fail($"An error occurred: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    // PUT: api/Attendance/5
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<AttendanceDto>>> PutAttendance(Guid id, UpdateAttendanceDto attendanceDto)
    {
        try
        {
            var existingAttendance = await _context.Attendances.FindAsync(id);
            if (existingAttendance == null)
            {
                var notFoundResponse = ApiResponse<AttendanceDto>.NotFound("Attendance record not found");
                return NotFound(notFoundResponse);
            }

            // Update only the allowed fields
            attendanceDto.UpdateEntity(existingAttendance);

            await _context.SaveChangesAsync();

            // Load the student information
            await _context.Entry(existingAttendance)
                .Reference(a => a.Student)
                .LoadAsync();

            var updatedAttendanceDto = existingAttendance.ToDto();
            var response = ApiResponse<AttendanceDto>.Ok(updatedAttendanceDto, "Attendance record updated successfully");
            return Ok(response);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!AttendanceExists(id))
            {
                var notFoundResponse = ApiResponse<AttendanceDto>.NotFound("Attendance record not found");
                return NotFound(notFoundResponse);
            }
            else
            {
                var errorResponse = ApiResponse<AttendanceDto>.Fail("Concurrency error occurred", 409);
                return Conflict(errorResponse);
            }
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse<AttendanceDto>.Fail($"An error occurred: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    // DELETE: api/Attendance/5
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAttendance(Guid id)
    {
        try
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
            {
                var notFoundResponse = ApiResponse<object>.NotFound("Attendance record not found");
                return NotFound(notFoundResponse);
            }

            _context.Attendances.Remove(attendance);
            await _context.SaveChangesAsync();

            var response = ApiResponse<object>.Ok("Attendance record deleted successfully");
            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            return StatusCode(500, errorResponse);
        }
    }

    private bool AttendanceExists(Guid id)
    {
        return _context.Attendances.Any(e => e.Id == id);
    }
}

