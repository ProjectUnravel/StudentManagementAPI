namespace StudentManagementApi.DTOs;

public class AttendanceDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public DateTime? ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public DateTime CreatedAt { get; set; }
    public StudentDto? Student { get; set; }
    public Guid CourseId { get; set; }
    public CourseDto? Course { get; set; }
}

public class CreateAttendanceDto
{
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public DateTime? ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
}

public class UpdateAttendanceDto
{
    public DateTime? ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
}

public class ClockInRequestDto
{
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
}

public class ClockOutRequestDto
{
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }

}