using AarhusSpaceProgram.Api.Data;
using AarhusSpaceProgram.Api.Dtos.Launchpads;
using AarhusSpaceProgram.Api.Models;
using AarhusSpaceProgram.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AarhusSpaceProgram.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.ReadAccess)]
[Route("api/[controller]")]
public class LaunchpadsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LaunchpadsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LaunchpadDto>>> GetLaunchpads()
    {
        var items = await _context.Launchpads.Select(lp => new LaunchpadDto
        {
            Id = lp.Id,
            Location = lp.Location,
            Description = lp.Description
        }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LaunchpadDto>> GetLaunchpad(int id)
    {
        var lp = await _context.Launchpads.FindAsync(id);
        if (lp == null) return NotFound();

        return Ok(new LaunchpadDto { Id = lp.Id, Location = lp.Location, Description = lp.Description });
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOnly)]
    public async Task<ActionResult<LaunchpadDto>> CreateLaunchpad(CreateLaunchpadDto dto)
    {
        var lp = new Launchpad { Location = dto.Location, Description = dto.Description };
        _context.Launchpads.Add(lp);
        await _context.SaveChangesAsync();

        var result = new LaunchpadDto { Id = lp.Id, Location = lp.Location, Description = lp.Description };
        return CreatedAtAction(nameof(GetLaunchpad), new { id = lp.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOnly)]
    public async Task<IActionResult> UpdateLaunchpad(int id, UpdateLaunchpadDto dto)
    {
        var lp = await _context.Launchpads.FindAsync(id);
        if (lp == null) return NotFound();

        lp.Location = dto.Location;
        lp.Description = dto.Description;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOnly)]
    public async Task<IActionResult> DeleteLaunchpad(int id)
    {
        var lp = await _context.Launchpads.FindAsync(id);
        if (lp == null) return NotFound();

        _context.Launchpads.Remove(lp);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
