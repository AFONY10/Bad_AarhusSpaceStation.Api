using System.ComponentModel.DataAnnotations;

namespace AarhusSpaceProgram.Api.Dtos.MissionLogs;

public class CreateMissionLogDto
{
    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public DateTime? Timestamp { get; set; }
}
