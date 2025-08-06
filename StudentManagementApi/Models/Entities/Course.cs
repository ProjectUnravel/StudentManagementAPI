using System.ComponentModel.DataAnnotations;

namespace StudentManagementApi.Models.Entities;

public class Course
{
    public Course()
    {
        CourseRegistrations = new HashSet<CourseRegistration>();
        Attendances = new HashSet<Attendance>();
        
    }


    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(20)]
    public string CourseCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CourseTitle { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<CourseRegistration> CourseRegistrations { get; set; }
    public virtual ICollection<Attendance> Attendances { get; set; }
}