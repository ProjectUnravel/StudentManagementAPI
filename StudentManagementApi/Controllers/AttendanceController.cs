using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementApi.Data;
using StudentManagementApi.Models;
using StudentManagementApi.DTOs;
using StudentManagementApi.Services;
using StudentManagementApi.Extensions;
using StudentManagementApi.Models.Entities;
using Task = StudentManagementApi.Models.Entities.Task;

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

            var courseTitle = await _context.Courses.Where(x => x.Id == request.CourseId).Select(x => x.CourseTitle).FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(courseTitle))
                return NotFound(ApiResponse<AttendanceDto>.NotFound("Course not found"));

            // Check if student already has an active attendance record (clocked in but not clocked out)
            var activeAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.StudentId == request.StudentId &&
                                        a.ClockIn != null && a.ClockOut == null);

            if (activeAttendance != null)
            {
                var conflictResponse = ApiResponse<AttendanceDto>.Fail("Student is already clocked in", 409);
                return Conflict(conflictResponse);
            }

            var currentDate = DateTime.UtcNow;
            //check if this is the first clockin for the day for the course
            string taskTitle = $"Attendance for {currentDate:ddd dd MMM, yyyy}";

            var taskId = await _context.Tasks.Where(x => x.CourseId == request.CourseId
                                                         && x.CreatedAt.Date == currentDate.Date
                                                         && x.Title == taskTitle)
                                             .Select(x => x.Id)
                                             .FirstOrDefaultAsync();

            var isFirstClockin = await _context.Attendances.AnyAsync(x => x.CourseId == request.CourseId && x.CreatedAt.Date == currentDate.Date);

            if (!isFirstClockin && taskId == Guid.Empty)
            {
                Task attendanceTask = await CreateAttendanceTask(request, courseTitle, currentDate);
                taskId = attendanceTask.Id;
            }

            Attendance attendance = await SaveAttendanceRecord(request);

            await ScoreAttendanceTask(request, taskId);

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

    private async Task<Task> CreateAttendanceTask(ClockInRequestDto request, string courseTitle, DateTime currentDate)
    {
        //create a task for attendance
        var attendanceTask = new Task()
        {
            Title = $"{courseTitle} Attendance for {currentDate:ddd dd MMM, yyyy}",
            Description = $"Daily attendance task for {courseTitle}",
            MaxObtainableScore = 5,
            CourseId = request.CourseId,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Tasks.AddAsync(attendanceTask);
        return attendanceTask;
    }

    private async System.Threading.Tasks.Task ScoreAttendanceTask(ClockInRequestDto request, Guid taskId)
    {
        //score the student
        var attendanceScore = new TaskScore()
        {
            CreatedAt = DateTime.UtcNow,
            StudentId = request.StudentId,
            TaskId = taskId,
            Score = 5,
        };

        await _context.TaskScores.AddAsync(attendanceScore);
        
    }

    private async Task<Attendance> SaveAttendanceRecord(ClockInRequestDto request)
    {
        var attendance = new Attendance
        {
            Id = Guid.NewGuid(),
            StudentId = request.StudentId,
            ClockIn = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CourseId = request.CourseId,
        };

        await _context.Attendances.AddAsync(attendance);
        return attendance;
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

    //[HttpGet("sync")]
    //public async Task<IActionResult> Sync()
    //{
    //    try
    //    {
    //        var currentDate = DateTime.UtcNow;

    //        var attendances = await _context.Attendances.AsNoTracking().Include(x => x.Course).ToListAsync();

    //        //group attendance by course

    //        var groupedAttendance = attendances.GroupBy(x => x.CourseId);

    //        List<Task> taskList = [];
    //        List<TaskScore> taskScores = [];
    //        foreach (var group in groupedAttendance)
    //        {
    //            string taskTitle = $"{group.FirstOrDefault().Course.CourseTitle} Attendance for {currentDate:ddd dd MMM, yyyy}";

    //            //create a task
    //            var task = new Task()
    //            {
    //                CourseId = group.Key,
    //                MaxObtainableScore = 5,
    //                CreatedAt = currentDate,
    //                Title = taskTitle,
    //                Description = $"Daily attendance task for {group.FirstOrDefault().Course.CourseTitle}",
    //                Id = Guid.NewGuid()
    //            };

    //            //score task for each student

    //            foreach (var student in group)
    //            {
    //                var taskScore = new TaskScore()
    //                {
    //                    TaskId = task.Id,
    //                    Score = 5,
    //                    StudentId = student.StudentId,
    //                    CreatedAt = currentDate,
    //                    Id = Guid.NewGuid()
    //                };

    //                taskScores = [.. taskScores, taskScore];
    //            }

    //            taskList = [.. taskList, task];
    //        }

    //        await _context.Tasks.AddRangeAsync(taskList);

    //        await _context.TaskScores.AddRangeAsync(taskScores);

    //        var res = await _context.SaveChangesAsync();

    //        return Ok(res);
    //    }
    //    catch (Exception e)
    //    {

    //        throw;
    //    }
    //}


}

