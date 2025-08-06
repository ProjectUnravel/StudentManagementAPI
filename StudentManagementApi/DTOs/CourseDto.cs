namespace StudentManagementApi.DTOs;

public class CourseDto
{
    public Guid Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int CourseRegistrationCount { get; set; }
}


public class CourseWithStudentsDto : CourseDto
{
    public List<StudentDto> Students { get; set; } = new List<StudentDto>();
}

public class CreateCourseDto
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
}

public class UpdateCourseDto
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
}