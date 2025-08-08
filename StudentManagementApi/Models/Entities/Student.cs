using System.ComponentModel.DataAnnotations;

namespace StudentManagementApi.Models.Entities;

public class Student
{
    public Student()
    {
        CourseRegistrations = new HashSet<CourseRegistration>();
        Attendances = new HashSet<Attendance>();
    }

    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Gender { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<CourseRegistration> CourseRegistrations { get; set; }
    public virtual ICollection<Attendance> Attendances { get; set; }
    public virtual TeamMember Team { get; set; }
}