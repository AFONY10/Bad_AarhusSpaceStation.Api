namespace AarhusSpaceProgram.Api.Dtos.MissionLogs;

public class MissionLogDto
{
    public string? Id { get; set; }

    public int MissionId { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }
}
