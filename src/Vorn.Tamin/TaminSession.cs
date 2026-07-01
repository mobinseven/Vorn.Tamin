using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Vorn.Tamin.Kiota;

namespace Vorn.Tamin;

/// <summary>
/// Manages an authenticated HTTP session to the EP.Tamin API.
/// Holds sub-clients for each API domain and handles common headers.
/// </summary>
public sealed class TaminSession
{
    /// <summary>The underlying <see cref="HttpClient"/> used for all requests.</summary>
    public HttpClient HttpClient { get; }

    /// <summary>Base URI for the API (always ends with a trailing slash).</summary>
    public Uri BaseUri { get; }

    /// <summary>Optional Client-Id header value issued during API onboarding.</summary>
    public string? ClientId { get; }

    /// <summary>Backward-compatible reference-data and service-lookup operations.</summary>
    public ServiceClient Service { get; }

    /// <summary>Reference-data query operations.</summary>
    public ReferenceDataClient ReferenceData { get; }

    /// <summary>E-prescription writing, query, and mutation operations.</summary>
    public PrescriptionClient Prescription { get; }

    /// <summary>Alias for role-aware prescription workflows.</summary>
    public PrescriptionClient Prescriptions => Prescription;

    /// <summary>Dental rule-check workflow operations.</summary>
    public DentistryClient Dentistry { get; }

    /// <summary>Referral workflow operations.</summary>
    public ReferralClient Referrals { get; }

    /// <summary>Eligibility lookup workflow operations.</summary>
    public EligibilityClient Eligibility { get; }

    /// <summary>Hospitalization workflow operations.</summary>
    public HospitalizationClient Hospitalization { get; }

    /// <summary>Doctor-facing workflow operations.</summary>
    public DoctorClient Doctor { get; }

    /// <summary>Secretary-facing workflow operations.</summary>
    public SecretaryClient Secretary { get; }

    /// <summary>Nurse-facing workflow operations.</summary>
    public NurseClient Nurse { get; }

    internal ITaminKiotaGateway KiotaGateway { get; }

    /// <summary>
    /// Creates a <see cref="TaminSession"/> using a pre-obtained OAuth token.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use.</param>
    /// <param name="oauthToken">****** Required unless <paramref name="needToken"/> is <c>false</c>.</param>
    /// <param name="baseUri">Override the base URI (defaults to the selected endpoint).</param>
    /// <param name="needToken">When <c>true</c> (default), throws if no token is supplied.</param>
    /// <param name="clientId">Optional Client-Id header value issued during API onboarding.</param>
    /// <param name="endpoint">Generated endpoint surface to use for request builders.</param>
    public TaminSession(
        HttpClient httpClient,
        string? oauthToken = null,
        Uri? baseUri = null,
        bool needToken = true,
        string? clientId = null,
        TaminEndpoint endpoint = TaminEndpoint.Production)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        BaseUri = EnsureTrailingSlash(baseUri ?? new Uri(DefaultBaseUrl(endpoint)));
        ClientId = clientId;

        if (needToken && string.IsNullOrWhiteSpace(oauthToken))
            throw new AuthTokenNotSuppliedException();

        KiotaGateway = CreateGateway(endpoint, HttpClient, BaseUri, oauthToken, clientId);
        ReferenceData = new ReferenceDataClient(KiotaGateway);
        Service = new ServiceClient(ReferenceData);
        Prescription = new PrescriptionClient(KiotaGateway);
        Dentistry = new DentistryClient(Prescription);
        Referrals = new ReferralClient(Prescription, KiotaGateway);
        Eligibility = new EligibilityClient(KiotaGateway);
        Hospitalization = new HospitalizationClient(KiotaGateway);
        Doctor = new DoctorClient(ReferenceData, Prescription, Dentistry, Referrals);
        Secretary = new SecretaryClient(Eligibility, Hospitalization);
        Nurse = new NurseClient(KiotaGateway);
    }

    /// <summary>
    /// Creates a <see cref="TaminSession"/>, optionally performing a login if no token is provided.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use.</param>
    /// <param name="oauthToken">Pre-obtained bearer token. When supplied, login is skipped.</param>
    /// <param name="baseUri">Override the base URI (defaults to the selected endpoint).</param>
    /// <param name="username">Username for the login flow.</param>
    /// <param name="password">Password for the login flow.</param>
    /// <param name="otp">One-time password, when two-step verification is required.</param>
    /// <param name="providerIdentifier">Provider identifier, when required by the server.</param>
    /// <param name="clientId">Optional Client-Id header value issued during API onboarding.</param>
    /// <param name="needToken">When <c>true</c> (default), throws if authentication cannot be established.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="endpoint">Generated endpoint surface to use for request builders.</param>
    public static async Task<TaminSession> CreateAsync(
        HttpClient httpClient,
        string? oauthToken = null,
        Uri? baseUri = null,
        string? username = null,
        string? password = null,
        string? otp = null,
        string? providerIdentifier = null,
        string? clientId = null,
        bool needToken = true,
        CancellationToken cancellationToken = default,
        TaminEndpoint endpoint = TaminEndpoint.Production)
    {
        var normalizedBaseUri = EnsureTrailingSlash(baseUri ?? new Uri(DefaultBaseUrl(endpoint)));

        if (needToken && string.IsNullOrWhiteSpace(oauthToken))
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                throw new AuthTokenNotSuppliedException();

            oauthToken = await LoginAsync(httpClient, normalizedBaseUri, username, password, otp, providerIdentifier, cancellationToken).ConfigureAwait(false);
        }

        return new TaminSession(httpClient, oauthToken, normalizedBaseUri, needToken, clientId, endpoint);
    }

    /// <summary>
    /// Attempts to refresh an expired access token using the supplied refresh token.
    /// Updates this session's <c>Authorization</c> header on success.
    /// </summary>
    /// <param name="refreshToken">The refresh token obtained from a previous login.</param>
    /// <param name="clientId">Client identifier, when required by the server.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new <see cref="TokenResult"/>.</returns>
    public async Task<TokenResult> RefreshTokenAsync(string refreshToken, string? clientId = null, CancellationToken cancellationToken = default)
    {
        var uri = new Uri(BaseUri, "ws/api/auth/refresh");
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { refresh_token = refreshToken, client_id = clientId }),
                Encoding.UTF8,
                "application/json")
        };

        using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        ResponseHandling.Handle(response.StatusCode, response.ReasonPhrase, content);

        using var doc = JsonDocument.Parse(content);
        var payload = doc.RootElement.TryGetProperty("data", out var data)
            ? data.GetRawText()
            : content;
        var result = JsonSerializer.Deserialize<TokenResult>(payload) ?? new TokenResult();

        var token = result.AccessToken ?? result.Data;
        if (!string.IsNullOrWhiteSpace(token))
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return result;
    }

    /// <summary>
    /// Checks whether the current access token is still valid.
    /// </summary>
    /// <param name="accessToken">The access token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="ValidateTokenResult"/> describing the token state.</returns>
    public async Task<ValidateTokenResult> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        var uri = new Uri(BaseUri, "ws/api/auth/validate");
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { access_token = accessToken }),
                Encoding.UTF8,
                "application/json")
        };

        using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        ResponseHandling.Handle(response.StatusCode, response.ReasonPhrase, content);

        using var doc = JsonDocument.Parse(content);
        if (doc.RootElement.TryGetProperty("data", out var data))
            return JsonSerializer.Deserialize<ValidateTokenResult>(data.GetRawText()) ?? new ValidateTokenResult();

        return JsonSerializer.Deserialize<ValidateTokenResult>(content) ?? new ValidateTokenResult();
    }

    private static async Task<string> LoginAsync(
        HttpClient httpClient,
        Uri baseUri,
        string username,
        string password,
        string? otp,
        string? providerIdentifier,
        CancellationToken cancellationToken)
    {
        var uri = new Uri(baseUri, "ws/api/auth/login");

        var loginPayload = new Dictionary<string, string?>(4)
        {
            ["client_id"] = username,
            ["secret"] = password
        };
        if (!string.IsNullOrWhiteSpace(otp))
            loginPayload["otp"] = otp;
        if (!string.IsNullOrWhiteSpace(providerIdentifier))
            loginPayload["provider_identifier"] = providerIdentifier;

        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json")
        };

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(content);

        if (response.IsSuccessStatusCode)
        {
            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.String)
                return data.GetString()!;
        }

        throw new UserLoginException(
            doc.RootElement.TryGetProperty("data", out var dataNode) ? dataNode.ToString() : null,
            doc.RootElement.TryGetProperty("status", out var statusNode) && statusNode.TryGetInt32(out var status) ? status : null,
            doc.RootElement.TryGetProperty("family", out var familyNode) ? familyNode.ToString() : null,
            doc.RootElement.TryGetProperty("reason", out var reasonNode) ? reasonNode.ToString() : null);
    }

    private static ITaminKiotaGateway CreateGateway(TaminEndpoint endpoint, HttpClient httpClient, Uri baseUri, string? oauthToken, string? clientId)
        => endpoint switch
        {
            TaminEndpoint.Production => new TaminKiotaGateway(httpClient, baseUri, oauthToken, clientId),
            TaminEndpoint.Sandbox => new TaminKiotaSandboxGateway(httpClient, baseUri, oauthToken, clientId),
            _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, "Unsupported Tamin endpoint.")
        };

    internal static string DefaultBaseUrl(TaminEndpoint endpoint)
        => endpoint switch
        {
            TaminEndpoint.Production => TaminKiotaClientFactory.DefaultBaseUrl,
            TaminEndpoint.Sandbox => TaminKiotaSandboxClientFactory.DefaultBaseUrl,
            _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, "Unsupported Tamin endpoint.")
        };

    private static Uri EnsureTrailingSlash(Uri uri)
    {
        if (uri.AbsoluteUri.EndsWith('/'))
            return uri;

        return new Uri($"{uri.AbsoluteUri}/");
    }
}

