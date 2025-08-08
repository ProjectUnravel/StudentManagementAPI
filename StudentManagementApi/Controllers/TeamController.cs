using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementApi.Data;
using StudentManagementApi.Models;
using StudentManagementApi.DTOs;
using StudentManagementApi.Services;
using StudentManagementApi.Extensions;
using Microsoft.VisualBasic;

namespace StudentManagementApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TeamController(ApplicationDbContext context)
    {
        _context = context;
    }

    //GET: api/team
    [HttpGet]
    public async Task<IActionResult> GetTeams([FromQuery]PaginationRequest pagination)
    {
        var query = _context.Teams.AsNoTracking().Where(x=>x.IsActive);

        //apply search filter
        if (!string.IsNullOrWhiteSpace(pagination.Search))
            query = query.Where(x => x.Name.Contains(pagination.Search));

        //apply sorting
        if (!string.IsNullOrWhiteSpace(pagination.SortBy))
            query = pagination.SortBy?.ToLower() switch
            {
                "name" => pagination.SortDescending ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
                "createdat" => pagination.SortDescending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
                _ => query.OrderByDescending(x => x.CreatedAt)
            };

        var (teams, totalCount) = await query.ToPaginatedListAsync(pagination);
        var teamDtos = teams.Select(cr => cr.ToDto()).ToList();
        var metaData = MetaData.Create(pagination.PageIndex, pagination.PageSize, totalCount);

        var response = ApiResponse<List<TeamDto>>.Ok(teamDtos, "Teams retrieved successfully", metaData);
        return Ok(response);
    }


    //GET: api/team/2
    [HttpGet("{id}")]
    public async Task<IActionResult> Getteam([FromRoute]Guid id)
    {
        var team = await _context.Teams.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

        if (team is null)
            return NotFound(ApiResponse<TeamDto>.NotFound("Team not found"));

        var teamDto = team.ToDto();

        return Ok(ApiResponse<TeamDto>.Ok(teamDto, "Team retrieved successfully"));
    }

    //POST: api/team
    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody]CreateTeamDto request)
    {
        //check if name is already in use
        var isTeamNameInUse = await _context.Teams.AsNoTracking().AnyAsync(x => x.Name.ToLower() == request.Name!.ToLower() && x.IsActive);

        if (isTeamNameInUse)
            return Conflict(ApiResponse<string>.Fail($"{request.Name} is already in use"));

        var team = request.ToEntity();
        team.CreatedAt = DateTime.UtcNow;

        await _context.Teams.AddAsync(team);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok($"{request.Name} Team created successfully"));
    }

    //PUT: api/team/3
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTeam([FromBody] CreateTeamDto request, [FromRoute]Guid id)
    {
        var team = await _context.Teams.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

        if (team is null)
            return NotFound(ApiResponse<string>.NotFound("team not found"));

        //check if name is already in use
        var isTeamNameInUse = await _context.Teams.AsNoTracking().AnyAsync(x => x.Name.ToLower() == request.Name!.ToLower() && x.Id != id && x.IsActive);

        if (isTeamNameInUse)
            return Conflict(ApiResponse<string>.Fail($"{request.Name} is already in use"));

        team.Name = request.Name!;
        team.Description = request.Description;

        _context.Teams.Update(team);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok($"{request.Name} Team updated successfully"));
    }

    //DELETE: api/team/3
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTeam([FromRoute] Guid id)
    {
        var team = await _context.Teams.FirstOrDefaultAsync(x => x.Id == id);

        if (team is null)
            return NotFound(ApiResponse<string>.NotFound("team not found"));

        team.IsActive = false;

        _context.Teams.Update(team);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok($"{team.Name} Team deleted successfully"));
    }
}
