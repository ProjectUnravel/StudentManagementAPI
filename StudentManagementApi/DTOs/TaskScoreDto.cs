namespace StudentManagementApi.DTOs;

public class TaskScoreDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public TaskDto? Task { get; set; }
    public Guid StudentId { get; set; }
    public StudentDto? Student { get; set; }
    public double Score { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTaskScoreDto
{
    public Guid TaskId { get; set; }
    public Guid StudentId { get; set; }
    public double Score { get; set; }
}

public class UpdateTaskScoreDto
{
    public double Score { get; set; }
}