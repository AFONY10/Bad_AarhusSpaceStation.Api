using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace AarhusSpaceProgram.MissionLogWorker;

public class MissionLogApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MissionLogApiClient> _logger;
    private readonly MissionLogWorkerOptions _options;

    private string? _accessToken;
    private DateTime _accessTokenExpiresAtUtc;

    public MissionLogApiClient(
        HttpClient httpClient,
        ILogger<MissionLogApiClient> logger,
        IOptions<MissionLogWorkerOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;

        _httpClient.BaseAddress = new Uri(_options.ApiBaseUrl);
    }

    public async Task<List<MissionDto>> GetActiveMissionsAsync(CancellationToken cancellationToken)
    {
        var missions = await _httpClient.GetFromJsonAsync<List<MissionDto>>(
            "/api/missions?status=Active",
            cancellationToken);

        return missions ?? [];
    }

    public async Task CreateMissionLogAsync(
        int missionId,
        CreateMissionLogRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var response = await _httpClient.PostAsJsonAsync(
            $"/api/missions/{missionId}/logs",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("Mission log API token was rejected. Refreshing token and retrying.");
            _accessToken = null;
            await EnsureAuthenticatedAsync(cancellationToken);

            response = await _httpClient.PostAsJsonAsync(
                $"/api/missions/{missionId}/logs",
                request,
                cancellationToken);
        }

        response.EnsureSuccessStatusCode();
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken) &&
            _accessTokenExpiresAtUtc > DateTime.UtcNow.AddMinutes(1))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _accessToken);
            return;
        }

        var response = await _httpClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(_options.UserName, _options.Password),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
        if (login == null || string.IsNullOrWhiteSpace(login.Token))
        {
            throw new InvalidOperationException("API login did not return a JWT token.");
        }

        _accessToken = login.Token;
        _accessTokenExpiresAtUtc = login.ExpiresAtUtc;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _accessToken);
    }
}

public record MissionDto(int Id, string Name);

public record CreateMissionLogRequest(string Message, DateTime Timestamp);

public record LoginRequest(string UserName, string Password);

public record LoginResponse(string Token, DateTime ExpiresAtUtc);
