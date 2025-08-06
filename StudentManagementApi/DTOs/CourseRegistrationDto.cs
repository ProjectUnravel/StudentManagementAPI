namespace StudentManagementApi.DTOs;

public class CourseRegistrationDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public DateTime CreatedAt { get; set; }
    public StudentDto? Student { get; set; }
    public CourseDto? Course { get; set; }
}

public class CreateCourseRegistrationDto
{
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
}