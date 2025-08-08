using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementApi.Data;
using StudentManagementApi.Models;
using StudentManagementApi.DTOs;
using StudentManagementApi.Services;
using StudentManagementApi.Extensions;
using StudentManagementApi.Models.Entities;

namespace StudentManagementApi.Controllers;

[ApiController]
[Route("api/team-members")]
public class TeamMembersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TeamMembersController(ApplicationDbContext context)
    {
        _context = context;
    }

    //GET: api/team-members/2
    [HttpGet("{teamId}")]
    public async Task<IActionResult> GetTeamMembers([FromRoute]Guid teamId, [FromQuery]PaginationRequest pagination)
    {
        var query = _context.TeamMembers.AsNoTracking().Where(x => x.TeamId == teamId);

        if (!string.IsNullOrWhiteSpace(pagination.Search))
            query = query.Where(x => x.Student.FirstName.Contains(pagination.Search)
                                     || x.Student.LastName.Contains(pagination.Search)
                                     || x.Student.Email.Contains(pagination.Search)
                                     || x.Student.PhoneNumber.Contains(pagination.Search));

        if (!string.IsNullOrWhiteSpace(pagination.SortBy))
            query = pagination.SortBy.ToLower() switch
            {
                "firstname" => pagination.SortDescending
                ? query.OrderByDescending(s => s.Student.FirstName)
                : query.OrderBy(s => s.Student.FirstName),
                "lastname" => pagination.SortDescending
                    ? query.OrderByDescending(s => s.Student.LastName)
                    : query.OrderBy(s => s.Student.LastName),
                "email" => pagination.SortDescending
                    ? query.OrderByDescending(s => s.Student.Email)
                    : query.OrderBy(s => s.Student.Email),
                "createdat" => pagination.SortDescending
                    ? query.OrderByDescending(s => s.Student.CreatedAt)
                    : query.OrderBy(s => s.Student.CreatedAt),
                _ => query.OrderBy(s => s.Student.FirstName)
            };

        query = query.Include(s => s.Student).Include(t => t.Team);

        var (members, totalCount) = await query.ToPaginatedListAsync(pagination);
        var membersDto = members.Select(cr =>
        {
            var member = cr.Student.ToDto();
            member.CreatedAt = cr.CreatedAt;
            return member;
        }).ToList();
        var teamDto = members.FirstOrDefault()?.Team?.ToDto();

        var teamMembersDto = new TeamMembersDto() { Team = teamDto, Members = membersDto };

        var metaData = MetaData.Create(pagination.PageIndex, pagination.PageSize, totalCount);

        var response = ApiResponse<TeamMembersDto>.Ok(teamMembersDto, "Team memebrs retrieved successfully", metaData);
        return Ok(response);
    }

    //PUT: api/team-members/assign
    [HttpPut("assign")]
    public async Task<IActionResult> AssignMembers([FromBody] AssignTeamDto data)
    {
        //validate teamId
        var isValidTeamId = await _context.Teams.AsNoTracking().AnyAsync(x => x.Id == data.TeamId && x.IsActive);

        if (!isValidTeamId)
            return BadRequest(ApiResponse<string>.Fail("Invalid teamId"));

        //validate studentId
        var isValidStudentId = await _context.Students.AsNoTracking().AnyAsync(x => x.Id == data.StudentId);

        if (!isValidStudentId)
            return BadRequest(ApiResponse<string>.Fail("Invalid student Id"));

        //check if student already belongs to a team
        var isStudentAssignedToTeam = await _context.TeamMembers.AsNoTracking().AnyAsync(x => x.StudentId == data.StudentId);

        if (isStudentAssignedToTeam)
            return BadRequest(ApiResponse<string>.Fail("Student has previously been assigned to a team"));

        var teamMember = new TeamMember()
        {
            StudentId = data.StudentId,
            TeamId = data.TeamId,
            CreatedAt = DateTime.UtcNow
        };

        await _context.TeamMembers.AddAsync(teamMember);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok("Student assigned to team successfully"));
    }

    //PUT: api/team-members/unassign
    [HttpPut("unassign")]
    public async Task<IActionResult> UnassignMembers([FromBody] AssignTeamDto data)
    {
        //validate teamId
        var isValidTeamId = await _context.Teams.AsNoTracking().AnyAsync(x => x.Id == data.TeamId && x.IsActive);

        if (!isValidTeamId)
            return BadRequest(ApiResponse<string>.Fail("Invalid teamId"));

        //validate studentId
        var isValidStudentId = await _context.Students.AsNoTracking().AnyAsync(x => x.Id == data.StudentId);

        if (!isValidStudentId)
            return BadRequest(ApiResponse<string>.Fail("Invalid student Id"));

        //check if student already belongs to a team
        var studentMembership = await _context.TeamMembers.AsNoTracking().FirstOrDefaultAsync(x => x.StudentId == data.StudentId && x.TeamId == data.TeamId);

        if (studentMembership is null)
            return BadRequest(ApiResponse<string>.Fail("Student team not found"));

        

        _context.TeamMembers.Remove(studentMembership);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok("Student team has been unassigned successfully"));
    }
}