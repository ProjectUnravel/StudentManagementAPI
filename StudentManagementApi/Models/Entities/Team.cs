using System.ComponentModel.DataAnnotations;

namespace StudentManagementApi.Models.Entities;

public class Team
{
    public Team()
    {
        TeamMembers = new HashSet<TeamMember>();
    }

    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public virtual ICollection<TeamMember> TeamMembers { get; set; }
}
