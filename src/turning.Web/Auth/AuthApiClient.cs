using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace turning.Web.Auth;

public sealed class AuthApiClient(HttpClient httpClient, AuthSession authSession)
{
    public Task<AuthResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default) =>
        SendAsync<AuthResultDto>(HttpMethod.Post, "api/auth/login", request, cancellationToken);

    public Task<AuthResultDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default) =>
        SendAsync<AuthResultDto>(HttpMethod.Post, "api/auth/register", request, cancellationToken);

    public Task<AuthenticatedUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default) =>
        SendAsync<AuthenticatedUserDto>(HttpMethod.Get, "api/auth/me", null, cancellationToken);

    public Task<AuthenticatedUserDto> SetRoleAsync(Guid userId, string role, CancellationToken ct = default) =>
        SendAsync<AuthenticatedUserDto>(HttpMethod.Put, $"api/auth/users/{userId}/role", new { role }, ct);

    private async Task<T> SendAsync<T>(HttpMethod method, string uri, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, uri);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (!string.IsNullOrWhiteSpace(authSession.AccessToken)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authSession.AccessToken);
        return await ApiResponse.SendAsync<T>(httpClient, request, ct);
    }
}

public sealed class ApiException(string message, HttpStatusCode statusCode, string? details = null) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? Details { get; } = details;
}
