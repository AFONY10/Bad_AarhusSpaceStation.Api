using AarhusSpaceProgram.Api.Models;

namespace AarhusSpaceProgram.Api.Services;

public interface IJwtTokenService
{
    Task<JwtTokenResult> CreateTokenAsync(ApplicationUser user);
}

public record JwtTokenResult(string Token, DateTime ExpiresAtUtc, IReadOnlyCollection<string> Roles);
