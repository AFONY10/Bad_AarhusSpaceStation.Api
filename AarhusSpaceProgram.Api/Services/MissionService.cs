using AarhusSpaceProgram.Api.Data;
using AarhusSpaceProgram.Api.Dtos.Missions;
using AarhusSpaceProgram.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AarhusSpaceProgram.Api.Services;

public class MissionService : IMissionService
{
    private readonly ApplicationDbContext _context;

    public MissionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MissionDto>> GetAllAsync(MissionStatus? status = null)
    {
        var query = _context.Missions
            .Include(m => m.Manager)
            .Include(m => m.Rocket)
            .Include(m => m.Launchpad)
            .Include(m => m.TargetCelestialBody)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(m => m.Status == status.Value);
        }

        return await query
            .Select(m => new MissionDto
            {
                Id = m.Id,
                Name = m.Name,
                LaunchDate = m.LaunchDate,
                Status = m.Status,
                ManagerId = m.ManagerId,
                ManagerName = m.Manager != null ? m.Manager.FullName : null,
                RocketId = m.RocketId,
                RocketModel = m.Rocket != null ? m.Rocket.Model : null,
                LaunchpadId = m.LaunchpadId,
                LaunchpadLocation = m.Launchpad != null ? m.Launchpad.Location : null,
                TargetCelestialBodyId = m.TargetCelestialBodyId,
                TargetCelestialBodyName = m.TargetCelestialBody != null ? m.TargetCelestialBody.Name : null
            })
            .ToListAsync();
    }

    public async Task<MissionDetailsDto?> GetByIdAsync(int id)
    {
        return await _context.Missions
            .Include(m => m.Manager)
            .Include(m => m.Rocket)
            .Include(m => m.Launchpad)
            .Include(m => m.TargetCelestialBody)
            .Include(m => m.Astronauts)
            .Include(m => m.Scientists)
            .Where(m => m.Id == id)
            .Select(m => new MissionDetailsDto
            {
                Id = m.Id,
                Name = m.Name,
                LaunchDate = m.LaunchDate,
                Status = m.Status,
                ManagerName = m.Manager != null ? m.Manager.FullName : null,
                RocketModel = m.Rocket != null ? m.Rocket.Model : null,
                LaunchpadLocation = m.Launchpad != null ? m.Launchpad.Location : null,
                TargetCelestialBodyName = m.TargetCelestialBody != null ? m.TargetCelestialBody.Name : null,
                Astronauts = m.Astronauts.Select(a => a.FullName).ToList(),
                Scientists = m.Scientists.Select(s => s.FullName).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Missions.AnyAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<MissionOverviewDto>> GetOverviewAsync()
    {
        return await _context.Missions
            .Include(m => m.Manager)
            .Include(m => m.Rocket)
            .Include(m => m.Launchpad)
            .Include(m => m.TargetCelestialBody)
            .Select(m => new MissionOverviewDto
            {
                Id = m.Id,
                MissionName = m.Name,
                LaunchDate = m.LaunchDate,
                ManagerName = m.Manager != null ? m.Manager.FullName : null,
                RocketModel = m.Rocket != null ? m.Rocket.Model : null,
                LaunchpadLocation = m.Launchpad != null ? m.Launchpad.Location : null,
                TargetCelestialBodyName = m.TargetCelestialBody != null ? m.TargetCelestialBody.Name : null
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<MissionDto>> GetByTargetBodyAsync(string targetBodyName)
    {
        return await _context.Missions
            .Include(m => m.TargetCelestialBody)
            .Include(m => m.Manager)
            .Include(m => m.Rocket)
            .Include(m => m.Launchpad)
            .Where(m => m.TargetCelestialBody != null && m.TargetCelestialBody.Name == targetBodyName)
            .Select(m => new MissionDto
            {
                Id = m.Id,
                Name = m.Name,
                LaunchDate = m.LaunchDate,
                Status = m.Status,
                ManagerId = m.ManagerId,
                ManagerName = m.Manager != null ? m.Manager.FullName : null,
                RocketId = m.RocketId,
                RocketModel = m.Rocket != null ? m.Rocket.Model : null,
                LaunchpadId = m.LaunchpadId,
                LaunchpadLocation = m.Launchpad != null ? m.Launchpad.Location : null,
                TargetCelestialBodyId = m.TargetCelestialBodyId,
                TargetCelestialBodyName = m.TargetCelestialBody != null ? m.TargetCelestialBody.Name : null
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error, MissionDto? Mission)> CreateAsync(CreateMissionDto dto)
    {
        var validationError = await ValidateMissionRulesAsync(dto.LaunchpadId, dto.LaunchDate, dto.RocketId, dto.Status, null, null);
        if (validationError != null)
        {
            return (false, validationError, null);
        }

        var mission = new Mission
        {
            Name = dto.Name,
            LaunchDate = dto.LaunchDate,
            Status = dto.Status,
            ManagerId = dto.ManagerId,
            RocketId = dto.RocketId,
            LaunchpadId = dto.LaunchpadId,
            TargetCelestialBodyId = dto.TargetCelestialBodyId
        };

        _context.Missions.Add(mission);
        await _context.SaveChangesAsync();

        var created = await _context.Missions
            .Include(m => m.Manager)
            .Include(m => m.Rocket)
            .Include(m => m.Launchpad)
            .Include(m => m.TargetCelestialBody)
            .FirstAsync(m => m.Id == mission.Id);

        return (true, null, MapMissionDto(created));
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateMissionDto dto)
    {
        var mission = await _context.Missions
            .Include(m => m.Astronauts)
            .Include(m => m.Scientists)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (mission == null)
        {
            return (false, "Mission not found.");
        }

        var validationError = await ValidateMissionRulesAsync(
            dto.LaunchpadId,
            dto.LaunchDate,
            dto.RocketId,
            dto.Status,
            mission.Id,
            mission);

        if (validationError != null)
        {
            return (false, validationError);
        }

        mission.Name = dto.Name;
        mission.LaunchDate = dto.LaunchDate;
        mission.Status = dto.Status;
        mission.ManagerId = dto.ManagerId;
        mission.RocketId = dto.RocketId;
        mission.LaunchpadId = dto.LaunchpadId;
        mission.TargetCelestialBodyId = dto.TargetCelestialBodyId;

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var mission = await _context.Missions.FindAsync(id);
        if (mission == null)
        {
            return false;
        }

        _context.Missions.Remove(mission);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string? Error)> AssignAstronautAsync(int missionId, int astronautId)
    {
        var mission = await _context.Missions
            .Include(m => m.Astronauts)
            .FirstOrDefaultAsync(m => m.Id == missionId);

        if (mission == null) return (false, "Mission not found.");

        var astronaut = await _context.Astronauts.FindAsync(astronautId);
        if (astronaut == null) return (false, "Astronaut not found.");

        if (mission.Astronauts.Any(a => a.Id == astronautId))
            return (false, "Astronaut already assigned to mission.");

        mission.Astronauts.Add(astronaut);
        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemoveAstronautAsync(int missionId, int astronautId)
    {
        var mission = await _context.Missions
            .Include(m => m.Astronauts)
            .FirstOrDefaultAsync(m => m.Id == missionId);

        if (mission == null) return (false, "Mission not found.");

        var astronaut = mission.Astronauts.FirstOrDefault(a => a.Id == astronautId);
        if (astronaut == null) return (false, "Astronaut is not assigned to mission.");

        mission.Astronauts.Remove(astronaut);
        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AssignScientistAsync(int missionId, int scientistId)
    {
        var mission = await _context.Missions
            .Include(m => m.Scientists)
            .FirstOrDefaultAsync(m => m.Id == missionId);

        if (mission == null) return (false, "Mission not found.");

        var scientist = await _context.Scientists.FindAsync(scientistId);
        if (scientist == null) return (false, "Scientist not found.");

        if (mission.Scientists.Any(s => s.Id == scientistId))
            return (false, "Scientist already assigned to mission.");

        mission.Scientists.Add(scientist);
        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemoveScientistAsync(int missionId, int scientistId)
    {
        var mission = await _context.Missions
            .Include(m => m.Scientists)
            .FirstOrDefaultAsync(m => m.Id == missionId);

        if (mission == null) return (false, "Mission not found.");

        var scientist = mission.Scientists.FirstOrDefault(s => s.Id == scientistId);
        if (scientist == null) return (false, "Scientist is not assigned to mission.");

        mission.Scientists.Remove(scientist);
        await _context.SaveChangesAsync();

        return (true, null);
    }

    private async Task<string?> ValidateMissionRulesAsync(
        int? launchpadId,
        DateOnly? launchDate,
        int? rocketId,
        MissionStatus newStatus,
        int? currentMissionId,
        Mission? existingMission)
    {
        if (launchpadId.HasValue && launchDate.HasValue)
        {
            var launchpadConflict = await _context.Missions.AnyAsync(m =>
                m.Id != currentMissionId &&
                m.LaunchpadId == launchpadId &&
                m.LaunchDate == launchDate);

            if (launchpadConflict)
            {
                return "Launchpad cannot handle more than one mission per day.";
            }
        }

        if (rocketId.HasValue)
        {
            var rocketConflict = await _context.Missions.AnyAsync(m =>
                m.Id != currentMissionId &&
                m.RocketId == rocketId);

            if (rocketConflict)
            {
                return "Rocket is already assigned to another mission.";
            }
        }

        if (newStatus == MissionStatus.Active)
        {
            var astronautCount = existingMission?.Astronauts.Count ?? 0;
            var scientistCount = existingMission?.Scientists.Count ?? 0;

            if (astronautCount + scientistCount == 0)
            {
                return "Mission cannot become Active without assigned personnel.";
            }
        }

        return null;
    }

    private static MissionDto MapMissionDto(Mission m)
    {
        return new MissionDto
        {
            Id = m.Id,
            Name = m.Name,
            LaunchDate = m.LaunchDate,
            Status = m.Status,
            ManagerId = m.ManagerId,
            ManagerName = m.Manager?.FullName,
            RocketId = m.RocketId,
            RocketModel = m.Rocket?.Model,
            LaunchpadId = m.LaunchpadId,
            LaunchpadLocation = m.Launchpad?.Location,
            TargetCelestialBodyId = m.TargetCelestialBodyId,
            TargetCelestialBodyName = m.TargetCelestialBody?.Name
        };
    }
}
