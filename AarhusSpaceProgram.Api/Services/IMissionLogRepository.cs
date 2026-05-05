using AarhusSpaceProgram.Api.Models;

namespace AarhusSpaceProgram.Api.Services;

public interface IMissionLogRepository
{
    Task CreateAsync(MissionLog log, CancellationToken cancellationToken = default);

    Task<List<MissionLog>> GetByMissionIdAsync(int missionId, CancellationToken cancellationToken = default);
}
