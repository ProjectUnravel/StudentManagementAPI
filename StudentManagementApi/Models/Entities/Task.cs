using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementApi.Models.Entities;

public class Task
{
    public Task()
    {
        TaskScores = new HashSet<TaskScore>();
    }

    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public Guid CourseId { get; set; }

    [Required]
    [Range(0.1, double.MaxValue, ErrorMessage = "MaxObtainableScore must be greater than 0")]
    public double MaxObtainableScore { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("CourseId")]
    public virtual Course Course { get; set; } = null!;

    public virtual ICollection<TaskScore> TaskScores { get; set; }
}