namespace AarhusSpaceProgram.Api.Dtos.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public IEnumerable<string> Roles { get; set; } = [];

    public IEnumerable<StaffProfileDto> StaffProfiles { get; set; } = [];
}

public class StaffProfileDto
{
    public string ProfileType { get; set; } = string.Empty;

    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;
}
