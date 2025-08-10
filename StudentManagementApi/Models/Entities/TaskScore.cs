using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementApi.Models.Entities;

public class TaskScore
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TaskId { get; set; }

    [Required]
    public Guid StudentId { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Score cannot be negative")]
    public double Score { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("TaskId")]
    public virtual Task Task { get; set; } = null!;

    [ForeignKey("StudentId")]
    public virtual Student Student { get; set; } = null!;
}