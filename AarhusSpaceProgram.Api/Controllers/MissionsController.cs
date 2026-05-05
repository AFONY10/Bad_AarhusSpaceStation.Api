using AarhusSpaceProgram.Api.Dtos.Missions;
using AarhusSpaceProgram.Api.Security;
using AarhusSpaceProgram.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AarhusSpaceProgram.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.ReadAccess)]
[Route("api/[controller]")]
public class MissionsController : ControllerBase
{
    private readonly IMissionService _missionService;

    public MissionsController(IMissionService missionService)
    {
        _missionService = missionService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<MissionDto>>> GetMissions()
    {
        var missions = await _missionService.GetAllAsync();
        return Ok(missions);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MissionDetailsDto>> GetMission(int id)
    {
        var mission = await _missionService.GetByIdAsync(id);
        if (mission == null) return NotFound();

        return Ok(mission);
    }

    [HttpGet("overview")]
    public async Task<ActionResult<IEnumerable<MissionOverviewDto>>> GetMissionOverview()
    {
        var overview = await _missionService.GetOverviewAsync();
        return Ok(overview);
    }

    [HttpGet("by-target/{targetBodyName}")]
    public async Task<ActionResult<IEnumerable<MissionDto>>> GetMissionsByTarget(string targetBodyName)
    {
        var missions = await _missionService.GetByTargetBodyAsync(targetBodyName);
        return Ok(missions);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOnly)]
    public async Task<ActionResult<MissionDto>> CreateMission(CreateMissionDto dto)
    {
        var result = await _missionService.CreateAsync(dto);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return CreatedAtAction(nameof(GetMission), new { id = result.Mission!.Id }, result.Mission);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOnly)]
    public async Task<IActionResult> UpdateMission(int id, UpdateMissionDto dto)
    {
        var result = await _missionService.UpdateAsync(id, dto);

        if (!result.Success)
        {
            if (result.Error == "Mission not found.")
                return NotFound();

            return BadRequest(new { message = result.Error });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOnly)]
    public async Task<IActionResult> DeleteMission(int id)
    {
        var deleted = await _missionService.DeleteAsync(id);
        if (!deleted) return NotFound();

        return NoContent();
    }

    [HttpPost("{missionId:int}/astronauts/{astronautId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOnly)]
    public async Task<IActionResult> AssignAstronaut(int missionId, int astronautId)
    {
        var result = await _missionService.AssignAstronautAsync(missionId, astronautId);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return NoContent();
    }

    [HttpDelete("{missionId:int}/astronauts/{astronautId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOnly)]
    public async Task<IActionResult> RemoveAstronaut(int missionId, int astronautId)
    {
        var result = await _missionService.RemoveAstronautAsync(missionId, astronautId);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return NoContent();
    }

    [HttpPost("{missionId:int}/scientists/{scientistId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOnly)]
    public async Task<IActionResult> AssignScientist(int missionId, int scientistId)
    {
        var result = await _missionService.AssignScientistAsync(missionId, scientistId);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return NoContent();
    }

    [HttpDelete("{missionId:int}/scientists/{scientistId:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOnly)]
    public async Task<IActionResult> RemoveScientist(int missionId, int scientistId)
    {
        var result = await _missionService.RemoveScientistAsync(missionId, scientistId);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return NoContent();
    }
}
