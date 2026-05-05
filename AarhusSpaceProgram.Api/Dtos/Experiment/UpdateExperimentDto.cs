using System.ComponentModel.DataAnnotations;

namespace AarhusSpaceProgram.Api.Dtos.Experiments;

public class UpdateExperimentDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int MissionId { get; set; }

    [Required]
    public int ScientistId { get; set; }
}
