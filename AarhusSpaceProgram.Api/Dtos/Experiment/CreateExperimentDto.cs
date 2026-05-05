using System.ComponentModel.DataAnnotations;

namespace AarhusSpaceProgram.Api.Dtos.Experiments;

public class CreateExperimentDto
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int MissionId { get; set; }

    public int? ScientistId { get; set; }
}
