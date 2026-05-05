using Microsoft.Extensions.Options;

namespace AarhusSpaceProgram.MissionLogWorker;

public class Worker(
    MissionLogApiClient apiClient,
    ILogger<Worker> logger,
    IOptions<MissionLogWorkerOptions> options) : BackgroundService
{
    private static readonly string[] Messages =
    [
        "Telemetry check completed",
        "Life-support readings are nominal",
        "Radiation sensor calibration completed",
        "Navigation drift check completed",
        "Payload temperature sample recorded",
        "Communication link quality verified"
    ];

    private readonly MissionLogWorkerOptions _options = options.Value;
    private readonly Random _random = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(10, _options.PollIntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        logger.LogInformation("Mission log generator started. Poll interval: {IntervalSeconds}s", interval.TotalSeconds);

        await GenerateMissionLogsAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await GenerateMissionLogsAsync(stoppingToken);
        }
    }

    private async Task GenerateMissionLogsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var missions = await apiClient.GetActiveMissionsAsync(cancellationToken);
            if (missions.Count == 0)
            {
                logger.LogInformation("No active missions found.");
                return;
            }

            foreach (var mission in missions)
            {
                var message = Messages[_random.Next(Messages.Length)];
                await apiClient.CreateMissionLogAsync(
                    mission.Id,
                    new CreateMissionLogRequest(message, DateTime.UtcNow),
                    cancellationToken);

                logger.LogInformation(
                    "Generated mission log for mission {MissionId}: {Message}",
                    mission.Id,
                    message);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate mission logs.");
        }
    }
}
