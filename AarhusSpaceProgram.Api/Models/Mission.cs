using System.ComponentModel.DataAnnotations;

namespace AarhusSpaceProgram.Api.Models;

public class Mission
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public DateOnly? LaunchDate { get; set; }

    public MissionStatus Status { get; set; } = MissionStatus.Created;

    public int? ManagerId { get; set; }
    public Manager? Manager { get; set; }

    public int? RocketId { get; set; }
    public Rocket? Rocket { get; set; }

    public int? LaunchpadId { get; set; }
    public Launchpad? Launchpad { get; set; }

    public int? TargetCelestialBodyId { get; set; }
    public CelestialBody? TargetCelestialBody { get; set; }

    public List<Astronaut> Astronauts { get; set; } = new();
    public List<Scientist> Scientists { get; set; } = new();
    public List<Experiment> Experiments { get; set; } = new();
}
