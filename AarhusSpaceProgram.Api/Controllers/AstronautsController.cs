using AarhusSpaceProgram.Api.Data;
using AarhusSpaceProgram.Api.Dtos.Astronauts;
using AarhusSpaceProgram.Api.Models;
using AarhusSpaceProgram.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AarhusSpaceProgram.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.ReadAccess)]
[Route("api/[controller]")]
public class AstronautsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AstronautsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AstronautDto>>> GetAstronauts()
    {
        var astronauts = await _context.Astronauts
            .Select(a => new AstronautDto
            {
                Id = a.Id,
                FullName = a.FullName,
                HoursInSpace = a.HoursInSpace
            })
            .ToListAsync();

        return Ok(astronauts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AstronautDto>> GetAstronaut(int id)
    {
        var astronaut = await _context.Astronauts.FindAsync(id);

        if (astronaut == null)
        {
            return NotFound();
        }

        var dto = new AstronautDto
        {
            Id = astronaut.Id,
            FullName = astronaut.FullName,
            HoursInSpace = astronaut.HoursInSpace
        };

        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOnly)]
    public async Task<ActionResult<AstronautDto>> CreateAstronaut(CreateAstronautDto dto)
    {
        var astronaut = new Astronaut
        {
            FullName = dto.FullName,
            HoursInSpace = dto.HoursInSpace
        };

        _context.Astronauts.Add(astronaut);
        await _context.SaveChangesAsync();

        var resultDto = new AstronautDto
        {
            Id = astronaut.Id,
            FullName = astronaut.FullName,
            HoursInSpace = astronaut.HoursInSpace
        };

        return CreatedAtAction(nameof(GetAstronaut), new { id = astronaut.Id }, resultDto);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOnly)]
    public async Task<IActionResult> UpdateAstronaut(int id, UpdateAstronautDto dto)
    {
        var astronaut = await _context.Astronauts.FindAsync(id);

        if (astronaut == null)
        {
            return NotFound();
        }

        astronaut.FullName = dto.FullName;
        astronaut.HoursInSpace = dto.HoursInSpace;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicyNames.ManagerOnly)]
    public async Task<IActionResult> DeleteAstronaut(int id)
    {
        var astronaut = await _context.Astronauts.FindAsync(id);

        if (astronaut == null)
        {
            return NotFound();
        }

        _context.Astronauts.Remove(astronaut);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
