using Microsoft.AspNetCore.Antiforgery;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementApi.DTOs;

public class StudentDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class StudentWithCoursesDto : StudentDto
{
    public List<CourseDto> Courses { get; set; } = new List<CourseDto>();
}

public class CreateStudentDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
}

public class UpdateStudentDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
}


public class TeamDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime createdAt { get; set; }

}

public class CreateTeamDto
{
    [Required]
    public string? Name { get; set; }

    [Required]
    public string? Description { get; set; }

}

public class AssignTeamDto
{
    public Guid TeamId { get; set; }
    public Guid StudentId { get; set; }
}

public class TeamMembersDto
{
    public TeamDto? Team { get; set; }
    public List<StudentDto>? Members { get; set; }
}