using AarhusSpaceProgram.Api.Data;
using AarhusSpaceProgram.Api.Dtos.Rockets;
using AarhusSpaceProgram.Api.Models;
using AarhusSpaceProgram.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AarhusSpaceProgram.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.ReadAccess)]
[Route("api/[controller]")]
public class RocketsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RocketsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RocketDto>>> GetRockets()
    {
        var items = await _context.Rockets.Select(r => new RocketDto
        {
            Id = r.Id,
            Model = r.Model,
            Weight = r.Weight,
            Manufacturer = r.Manufacturer
        }).ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RocketDto>> GetRocket(int id)
    {
        var r = await _context.Rockets.FindAsync(id);
        if (r == null) return NotFound();

        return Ok(new RocketDto { Id = r.Id, Model = r.Model, Weight = r.Weight, Manufacturer = r.Manufacturer });
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOnly)]
    public async Task<ActionResult<RocketDto>> CreateRocket(CreateRocketDto dto)
    {
        var r = new Rocket { Model = dto.Model, Weight = dto.Weight, Manufacturer = dto.Manufacturer };
        _context.Rockets.Add(r);
        await _context.SaveChangesAsync();

        var result = new RocketDto { Id = r.Id, Model = r.Model, Weight = r.Weight, Manufacturer = r.Manufacturer };
        return CreatedAtAction(nameof(GetRocket), new { id = r.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOnly)]
    public async Task<IActionResult> UpdateRocket(int id, UpdateRocketDto dto)
    {
        var r = await _context.Rockets.FindAsync(id);
        if (r == null) return NotFound();

        r.Model = dto.Model;
        r.Weight = dto.Weight;
        r.Manufacturer = dto.Manufacturer;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOnly)]
    public async Task<IActionResult> DeleteRocket(int id)
    {
        var r = await _context.Rockets.FindAsync(id);
        if (r == null) return NotFound();

        _context.Rockets.Remove(r);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
