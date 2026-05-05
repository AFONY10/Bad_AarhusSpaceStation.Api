using System.Security.Claims;
using AarhusSpaceProgram.Api.Data;
using AarhusSpaceProgram.Api.Dtos.Experiments;
using AarhusSpaceProgram.Api.Models;
using AarhusSpaceProgram.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AarhusSpaceProgram.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.ReadAccess)]
[Route("api/[controller]")]
public class ExperimentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ExperimentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExperimentDto>>> GetExperiments()
    {
        var experiments = await _context.Experiments
            .AsNoTracking()
            .Select(e => new ExperimentDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                CreatedAt = e.CreatedAt,
                MissionId = e.MissionId,
                MissionName = e.Mission.Name,
                ScientistId = e.ScientistId,
                ScientistName = e.Scientist.FullName
            })
            .ToListAsync();

        return Ok(experiments);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExperimentDto>> GetExperiment(int id)
    {
        var experiment = await _context.Experiments
            .AsNoTracking()
            .Include(e => e.Mission)
            .Include(e => e.Scientist)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (experiment == null)
        {
            return NotFound();
        }

        return Ok(MapExperimentDto(experiment));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.ExperimentWriteAccess)]
    public async Task<ActionResult<ExperimentDto>> CreateExperiment(CreateExperimentDto dto)
    {
        if (!await _context.Missions.AnyAsync(m => m.Id == dto.MissionId))
        {
            return BadRequest(new { message = "Mission not found." });
        }

        var scientistId = await ResolveScientistIdAsync(dto.ScientistId);
        if (scientistId == null)
        {
            return BadRequest(new { message = "ScientistId is required unless the current user is linked to a Scientist profile." });
        }

        if (!await _context.Scientists.AnyAsync(s => s.Id == scientistId.Value))
        {
            return BadRequest(new { message = "Scientist not found." });
        }

        var experiment = new Experiment
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow,
            MissionId = dto.MissionId,
            ScientistId = scientistId.Value
        };

        _context.Experiments.Add(experiment);
        await _context.SaveChangesAsync();

        var created = await _context.Experiments
            .AsNoTracking()
            .Include(e => e.Mission)
            .Include(e => e.Scientist)
            .FirstAsync(e => e.Id == experiment.Id);

        return CreatedAtAction(nameof(GetExperiment), new { id = experiment.Id }, MapExperimentDto(created));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.ExperimentWriteAccess)]
    public async Task<IActionResult> UpdateExperiment(int id, UpdateExperimentDto dto)
    {
        var experiment = await _context.Experiments.FindAsync(id);
        if (experiment == null)
        {
            return NotFound();
        }

        if (!await _context.Missions.AnyAsync(m => m.Id == dto.MissionId))
        {
            return BadRequest(new { message = "Mission not found." });
        }

        if (!await _context.Scientists.AnyAsync(s => s.Id == dto.ScientistId))
        {
            return BadRequest(new { message = "Scientist not found." });
        }

        experiment.Name = dto.Name;
        experiment.Description = dto.Description;
        experiment.MissionId = dto.MissionId;
        experiment.ScientistId = dto.ScientistId;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicyNames.ExperimentWriteAccess)]
    public async Task<IActionResult> DeleteExperiment(int id)
    {
        var experiment = await _context.Experiments.FindAsync(id);
        if (experiment == null)
        {
            return NotFound();
        }

        _context.Experiments.Remove(experiment);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<int?> ResolveScientistIdAsync(int? scientistId)
    {
        if (scientistId.HasValue)
        {
            return scientistId;
        }

        if (!User.IsInRole(RoleNames.Scientist))
        {
            return null;
        }

        var applicationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (applicationUserId == null)
        {
            return null;
        }

        var scientist = await _context.Scientists
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.ApplicationUserId == applicationUserId);

        return scientist?.Id;
    }

    private static ExperimentDto MapExperimentDto(Experiment experiment)
    {
        return new ExperimentDto
        {
            Id = experiment.Id,
            Name = experiment.Name,
            Description = experiment.Description,
            CreatedAt = experiment.CreatedAt,
            MissionId = experiment.MissionId,
            MissionName = experiment.Mission.Name,
            ScientistId = experiment.ScientistId,
            ScientistName = experiment.Scientist.FullName
        };
    }
}
