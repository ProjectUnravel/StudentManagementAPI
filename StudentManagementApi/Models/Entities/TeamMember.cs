using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementApi.Models.Entities;

public class TeamMember
{
    [Key]
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid TeamId { get; set; }

    [ForeignKey("StudentId")]
    public virtual Student Student { get; set; } = null!;

    [ForeignKey("TeamId")]
    public virtual Team Team { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
