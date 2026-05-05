using System.ComponentModel.DataAnnotations;

namespace AarhusSpaceProgram.Api.Models;

public class Experiment
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int MissionId { get; set; }
    public Mission Mission { get; set; } = null!;

    public int ScientistId { get; set; }
    public Scientist Scientist { get; set; } = null!;
}
