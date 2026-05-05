using System.ComponentModel.DataAnnotations;

namespace AarhusSpaceProgram.MissionLogWorker;

public class MissionLogWorkerOptions
{
    public const string SectionName = "MissionLogWorker";

    [Required]
    public string ApiBaseUrl { get; set; } = "http://localhost:8080";

    [Range(10, 30)]
    public int PollIntervalSeconds { get; set; } = 15;

    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
