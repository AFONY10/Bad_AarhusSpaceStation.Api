namespace AarhusSpaceProgram.Api.Security;

public static class RoleNames
{
    public const string Astronaut = "Astronaut";
    public const string Scientist = "Scientist";
    public const string Manager = "Manager";

    public static readonly string[] All = [Astronaut, Scientist, Manager];
}
