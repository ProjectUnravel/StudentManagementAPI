using StudentManagementApi.DTOs;
using StudentManagementApi.Models.Entities;

namespace StudentManagementApi.Services;

public static class MappingService
{
    // Student mappings
    public static StudentDto ToDto(this Student student)
    {
        return new StudentDto
        {
            Id = student.Id,
            FirstName = student.FirstName,
            LastName = student.LastName,
            Email = student.Email,
            PhoneNumber = student.PhoneNumber,
            Gender = student.Gender,
            CreatedAt = student.CreatedAt
        };
    }

    public static Student ToEntity(this CreateStudentDto dto)
    {
        return new Student
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Gender = dto.Gender
        };
    }

    public static void UpdateEntity(this UpdateStudentDto dto, Student student)
    {
        student.FirstName = dto.FirstName;
        student.LastName = dto.LastName;
        student.Email = dto.Email;
        student.PhoneNumber = dto.PhoneNumber;
        student.Gender = dto.Gender;
    }

    // Course mappings
    public static CourseDto ToDto(this Course course)
    {
        return new CourseDto
        {
            Id = course.Id,
            CourseCode = course.CourseCode,
            CourseTitle = course.CourseTitle,
            CreatedAt = course.CreatedAt,
            CourseRegistrationCount = course.CourseRegistrations.Count
        };
    }

    public static Course ToEntity(this CreateCourseDto dto)
    {
        return new Course
        {
            CourseCode = dto.CourseCode,
            CourseTitle = dto.CourseTitle
        };
    }

    public static void UpdateEntity(this UpdateCourseDto dto, Course course)
    {
        course.CourseCode = dto.CourseCode;
        course.CourseTitle = dto.CourseTitle;
    }

    // Attendance mappings
    public static AttendanceDto ToDto(this Attendance attendance)
    {
        return new AttendanceDto
        {
            Id = attendance.Id,
            StudentId = attendance.StudentId,
            ClockIn = attendance.ClockIn,
            ClockOut = attendance.ClockOut,
            CreatedAt = attendance.CreatedAt,
            Student = attendance.Student?.ToDto(),
            CourseId = attendance.CourseId,
            Course = attendance.Course?.ToDto()
        };
    }

    public static Attendance ToEntity(this CreateAttendanceDto dto)
    {
        return new Attendance
        {
            StudentId = dto.StudentId,
            ClockIn = dto.ClockIn,
            ClockOut = dto.ClockOut,
            CourseId = dto.CourseId,
        };
    }

    public static void UpdateEntity(this UpdateAttendanceDto dto, Attendance attendance)
    {
        attendance.ClockIn = dto.ClockIn;
        attendance.ClockOut = dto.ClockOut;
    }

    // Course Registration mappings
    public static CourseRegistrationDto ToDto(this CourseRegistration registration)
    {
        return new CourseRegistrationDto
        {
            Id = registration.Id,
            StudentId = registration.StudentId,
            CourseId = registration.CourseId,
            CreatedAt = registration.CreatedAt,
            Student = registration.Student?.ToDto(),
            Course = registration.Course?.ToDto()
        };
    }

    public static CourseRegistration ToEntity(this CreateCourseRegistrationDto dto)
    {
        return new CourseRegistration
        {
            StudentId = dto.StudentId,
            CourseId = dto.CourseId
        };
    }

    public static TeamDto ToDto(this Team team) => new()
    {
        createdAt = team.CreatedAt,
        Description = team.Description,
        Id = team.Id,
        Name = team.Name
    };

    public static Team ToEntity(this CreateTeamDto dto) => new() { Name = dto.Name!, Description = dto.Description };

    // Task mappings
    public static TaskDto ToDto(this Models.Entities.Task task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            CourseId = task.CourseId,
            Course = task.Course?.ToDto(),
            MaxObtainableScore = task.MaxObtainableScore,
            CreatedAt = task.CreatedAt,
            TaskScoresCount = task.TaskScores.Count
        };
    }

    public static Models.Entities.Task ToEntity(this CreateTaskDto dto)
    {
        return new Models.Entities.Task
        {
            Title = dto.Title,
            Description = dto.Description,
            CourseId = dto.CourseId,
            MaxObtainableScore = dto.MaxObtainableScore
        };
    }

    public static void UpdateEntity(this UpdateTaskDto dto, Models.Entities.Task task)
    {
        task.Title = dto.Title;
        task.Description = dto.Description;
        task.MaxObtainableScore = dto.MaxObtainableScore;
    }

    // TaskScore mappings
    public static TaskScoreDto ToDto(this TaskScore taskScore)
    {
        return new TaskScoreDto
        {
            Id = taskScore.Id,
            TaskId = taskScore.TaskId,
            Task = taskScore.Task?.ToDto(),
            StudentId = taskScore.StudentId,
            Student = taskScore.Student?.ToDto(),
            Score = taskScore.Score,
            CreatedAt = taskScore.CreatedAt
        };
    }

    public static TaskScore ToEntity(this CreateTaskScoreDto dto)
    {
        return new TaskScore
        {
            TaskId = dto.TaskId,
            StudentId = dto.StudentId,
            Score = dto.Score
        };
    }

    public static void UpdateEntity(this UpdateTaskScoreDto dto, TaskScore taskScore)
    {
        taskScore.Score = dto.Score;
    }
}