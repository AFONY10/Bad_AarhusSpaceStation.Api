using Microsoft.AspNetCore.Identity;

namespace AarhusSpaceProgram.Api.Models;

public class ApplicationUser : IdentityUser
{
    public Astronaut? AstronautProfile { get; set; }
    public Scientist? ScientistProfile { get; set; }
    public Manager? ManagerProfile { get; set; }
}
