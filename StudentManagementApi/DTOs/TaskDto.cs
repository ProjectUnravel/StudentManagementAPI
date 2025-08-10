namespace StudentManagementApi.DTOs;

public class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CourseId { get; set; }
    public CourseDto? Course { get; set; }
    public double MaxObtainableScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TaskScoresCount { get; set; }
}

public class CreateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CourseId { get; set; }
    public double MaxObtainableScore { get; set; }
}

public class UpdateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double MaxObtainableScore { get; set; }
}