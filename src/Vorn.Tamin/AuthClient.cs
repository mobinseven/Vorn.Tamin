using System.Net.Http.Headers;
using System.Text.Json;

namespace Vorn.Tamin;

/// <summary>Builds and sends EP.Tamin PKCE authorization, token, refresh, and sign-out requests.</summary>
public sealed class AuthClient
{
    private readonly TaminEndpoint _environment;
    private readonly TaminEnvironmentRoutes _routes;
    private readonly ITaminHttpTransport _transport;

    /// <summary>Creates an auth client that sends requests with the supplied HTTP client.</summary>
    public AuthClient(HttpClient httpClient, TaminEndpoint environment = TaminEndpoint.Production, TaminEnvironmentRoutes? routes = null)
        : this(new TaminHttpClientTransport(httpClient), environment, routes)
    {
    }

    internal AuthClient(ITaminHttpTransport transport, TaminEndpoint environment, TaminEnvironmentRoutes? routes = null)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _environment = environment;
        _routes = routes ?? new TaminEnvironmentRoutes();
    }

    /// <summary>Creates a PKCE authorization URL. State is mandatory and must be stored by the caller.</summary>
    public Uri CreateAuthorizationUrl(string clientId, Uri redirectUri, PkceChallenge pkce, string state)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("Client ID is required.", nameof(clientId));
        ArgumentNullException.ThrowIfNull(redirectUri);
        ArgumentNullException.ThrowIfNull(pkce);
        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("OAuth state is required and must be stored by the caller before redirecting.", nameof(state));

        var route = _routes.Resolve(_environment, TaminOperation.Authorize);
        return AppendQuery(route.Uri, new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri.ToString(),
            ["code_challenge"] = pkce.Challenge,
            ["code_challenge_method"] = pkce.Method,
            ["state"] = state
        });
    }

    /// <summary>Exchanges an authorization code and PKCE verifier for an access token.</summary>
    public Task<TokenResult> ExchangeCodeAsync(string clientId, string code, Uri redirectUri, PkceChallenge pkce, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("Client ID is required.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Authorization code is required.", nameof(code));
        ArgumentNullException.ThrowIfNull(redirectUri);
        ArgumentNullException.ThrowIfNull(pkce);

        return SendFormAsync(
            TaminOperation.TokenExchange,
            new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = clientId,
                ["code"] = code,
                ["redirect_uri"] = redirectUri.ToString(),
                ["code_verifier"] = pkce.Verifier
            },
            cancellationToken);
    }

    /// <summary>Refreshes an access token through the v2 token endpoint.</summary>
    public Task<TokenResult> RefreshTokenV2Async(string clientId, string refreshToken, string audience, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("Client ID is required.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("Refresh token is required.", nameof(refreshToken));
        if (string.IsNullOrWhiteSpace(audience))
            throw new ArgumentException("Audience is required.", nameof(audience));

        return SendFormAsync(
            TaminOperation.RefreshTokenV2,
            new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = clientId,
                ["refresh_token"] = refreshToken,
                ["audience"] = audience
            },
            cancellationToken);
    }

    /// <summary>Builds a sign-out URL with the required redirect URI.</summary>
    public Uri CreateSignOutUrl(Uri redirectUri)
    {
        ArgumentNullException.ThrowIfNull(redirectUri);
        return AppendQuery(_routes.Resolve(_environment, TaminOperation.SignOut).Uri, new Dictionary<string, string>
        {
            ["redirect_uri"] = redirectUri.ToString()
        });
    }

    /// <summary>Sends a sign-out request to the provider.</summary>
    public async Task SignOutAsync(Uri redirectUri, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, CreateSignOutUrl(redirectUri));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        await SendNoContentAsync(TaminOperation.SignOut, request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TokenResult> SendFormAsync(TaminOperation operation, IReadOnlyDictionary<string, string> fields, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _routes.Resolve(_environment, operation).Uri)
        {
            Content = new FormUrlEncodedContent(fields)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await SendAsync(operation, request, cancellationToken).ConfigureAwait(false);
        return DeserializeToken(response.Content);
    }

    private async Task SendNoContentAsync(TaminOperation operation, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _ = await SendAsync(operation, request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TaminHttpTransportResponse> SendAsync(TaminOperation operation, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        TaminHttpTransportResponse response;
        try
        {
            response = await _transport.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new TaminAuthRequestException(_environment, operation, null, null, ex);
        }

        if ((int)response.StatusCode is < 200 or > 299)
            throw new TaminAuthRequestException(_environment, operation, response.StatusCode, response.Content);

        return response;
    }

    private static TokenResult DeserializeToken(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new TokenResult();

        using var document = JsonDocument.Parse(content);
        var payload = document.RootElement.TryGetProperty("data", out var data)
            ? data.GetRawText()
            : content;
        return JsonSerializer.Deserialize<TokenResult>(payload) ?? new TokenResult();
    }

    private static Uri AppendQuery(Uri uri, IReadOnlyDictionary<string, string> query)
    {
        var queryString = string.Join("&", query.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        var separator = string.IsNullOrEmpty(uri.Query) ? "?" : "&";
        return new Uri($"{uri}{separator}{queryString}");
    }
}
