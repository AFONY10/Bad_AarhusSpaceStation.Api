namespace AarhusSpaceProgram.Api.Dtos.Experiments;

public class ExperimentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public int MissionId { get; set; }
    public string MissionName { get; set; } = string.Empty;

    public int ScientistId { get; set; }
    public string ScientistName { get; set; } = string.Empty;
}
