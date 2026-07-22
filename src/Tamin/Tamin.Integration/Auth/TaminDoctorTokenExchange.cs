using System.Net.Http.Json;
using System.Text.Json;

namespace Tamin.Integration.Auth;

/// <summary>
/// Executes the single discriminated v2 token route without Kiota's unsupported oneOf form serializer.
/// </summary>
public sealed class TaminDoctorTokenExchange(HttpClient httpClient, Uri tokenEndpoint, bool isPilot) : IDoctorTokenExchange
{
    public Task<DoctorToken> ExchangeAuthorizationCodeAsync(DoctorAuthorizationCode grant, CancellationToken cancellationToken)
    {
        ValidateEndpoint();
        ValidatePilotClientId(grant.ClientId);
        return SendAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = grant.Code,
            ["client_id"] = grant.ClientId,
            ["audience"] = grant.Audience,
            ["redirect_uri"] = grant.RedirectUri,
            ["code_verifier"] = grant.CodeVerifier
        }, cancellationToken);
    }

    public Task<DoctorToken> RefreshAsync(string refreshToken, string clientId, string audience, CancellationToken cancellationToken)
    {
        ValidateEndpoint();
        ValidatePilotClientId(clientId);
        return SendAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = clientId,
            ["audience"] = audience
        }, cancellationToken);
    }

    private async Task<DoctorToken> SendAsync(Dictionary<string, string> fields, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(fields);
        using var response = await httpClient.PostAsync(tokenEndpoint, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var payload = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Tamin token response was empty.");
        var root = payload.RootElement;
        var accessToken = root.TryGetProperty("access_token", out var access) && access.ValueKind == JsonValueKind.String ? access.GetString() : null;
        if (string.IsNullOrWhiteSpace(accessToken)) throw new InvalidOperationException("Tamin token response did not contain access_token.");
        var refreshToken = root.TryGetProperty("refresh_token", out var refresh) && refresh.ValueKind == JsonValueKind.String ? refresh.GetString() : null;
        DateTimeOffset? expiresAt = root.TryGetProperty("expires_in", out var expiry) && expiry.TryGetInt32(out var seconds)
            ? DateTimeOffset.UtcNow.AddSeconds(seconds)
            : null;
        return new DoctorToken(accessToken, refreshToken, expiresAt);
    }

    private void ValidateEndpoint()
    {
        // D-01: use the repeatedly declared v2 route, never the conflicting v1 curl example.
        if (tokenEndpoint.Scheme != Uri.UriSchemeHttps || !tokenEndpoint.AbsolutePath.Equals("/auth/server/v2/token", StringComparison.Ordinal))
            throw new InvalidOperationException("Doctor token endpoint must be the documented HTTPS /auth/server/v2/token route.");
    }

    private void ValidatePilotClientId(string clientId)
    {
        if (isPilot && !string.Equals(clientId, "portal-js", StringComparison.Ordinal))
            throw new ArgumentException("The pilot contract requires client_id 'portal-js'.", nameof(clientId));
    }
}
