using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Tamin.Integration.Auth;
using Tamin.Integration.Http;
using PilotAccountClient = Tamin.Client.Account.Pilot.PilotAccountClient;
using PilotApiClient = Tamin.Client.Api.Pilot.PilotApiClient;
using PilotSoaClient = Tamin.Client.Soa.Pilot.PilotSoaClient;
using ProdAccountClient = Tamin.Client.Account.Prod.ProdAccountClient;
using ProdApiClient = Tamin.Client.Api.Prod.ProdApiClient;
using ProdSoaClient = Tamin.Client.Soa.Prod.ProdSoaClient;

namespace Tamin.Integration;

/// <param name="PrimaryHandler">Owned by the caller, who keeps it alive and disposes it. Reuse one instance.</param>
/// <param name="TokenStore">Optional. Keeps the doctor token outside the client, for hosts that create a client per request or run several instances.</param>
public sealed record TaminClientOptions(string ClientId, string Audience, HttpMessageHandler PrimaryHandler, ILoggerFactory? LoggerFactory = null, ITaminTokenStore? TokenStore = null);

/// <summary>Creates <see cref="ITaminClient"/> instances. Hosts come from the generated clients, which take them from the OpenAPI servers.</summary>
public static class TaminClient
{
    public static ITaminClient CreateProduction(TaminClientOptions options)
    {
        var (account, soa, api) = Adapters(options);
        var accountClient = new ProdAccountClient(account);
        var soaClient = new ProdSoaClient(soa);
        var apiClient = new ProdApiClient(api);
        var tokens = Connect(options, isPilot: false, account, soa, api);
        return new ProductionTaminClient(accountClient, soaClient, apiClient, new(account, soa, api), tokens, options.ClientId);
    }

    public static ITaminClient CreatePilot(TaminClientOptions options)
    {
        var (account, soa, api) = Adapters(options);
        var accountClient = new PilotAccountClient(account);
        var soaClient = new PilotSoaClient(soa);
        var apiClient = new PilotApiClient(api);
        var tokens = Connect(options, isPilot: true, account, soa, api);
        return new PilotTaminClient(accountClient, soaClient, apiClient, new(account, soa, api), tokens);
    }

    /// <summary>Creates an RFC 7636 PKCE verifier. Keep it until the authorization callback.</summary>
    public static string CreateCodeVerifier(int length = 64) => TaminTokenProvider.CreateCodeVerifier(length);

    /// <summary>The S256 challenge for a verifier, as passed to <see cref="ITaminClient.CreateAuthorizationUri"/>.</summary>
    public static string CreateCodeChallenge(string codeVerifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeVerifier);
        return Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static (DeferredBearerAdapters.Account Account, DeferredBearerAdapters.Bearer Soa, DeferredBearerAdapters.Bearer Api) Adapters(TaminClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.PrimaryHandler);
        var logger = (options.LoggerFactory ?? NullLoggerFactory.Instance).CreateLogger<TaminResponseHandler>();
        return (new(Http(options.PrimaryHandler, logger)), new(Http(options.PrimaryHandler, logger)), new(Http(options.PrimaryHandler, logger)));
    }

    private static TaminTokenProvider Connect(TaminClientOptions options, bool isPilot, HttpClientRequestAdapter account, DeferredBearerAdapters.Bearer soa, DeferredBearerAdapters.Bearer api)
    {
        var accountBase = new Uri(account.BaseUrl!.TrimEnd('/') + "/");
        // The token route is outside the retrying pipeline: mutations are never retried.
        var exchange = new TaminDoctorTokenExchange(new HttpClient(options.PrimaryHandler, disposeHandler: false), new Uri(accountBase, "auth/server/v2/token"), isPilot);
        var tokens = new TaminTokenProvider(exchange, options.ClientId, options.Audience, [new Uri(soa.BaseUrl!).Host, new Uri(api.BaseUrl!).Host], options.TokenStore);
        soa.Tokens.Provider = tokens;
        api.Tokens.Provider = tokens;
        return tokens;
    }

    private static HttpClient Http(HttpMessageHandler primaryHandler, ILogger<TaminResponseHandler> logger)
    {
        var retry = new TaminTransientFaultHandler { InnerHandler = primaryHandler };
        var capture = new TaminResponseHandler(logger, TaminOperationOption.Resolve) { InnerHandler = retry };
        return new HttpClient(capture, disposeHandler: false);
    }
}

internal sealed record TaminAdapters(HttpClientRequestAdapter Account, HttpClientRequestAdapter Soa, HttpClientRequestAdapter Api);

/// <summary>
/// The hosts that bound a token are only known once the generated clients have set each adapter's base URL,
/// so bearer adapters resolve their token provider when a request is sent.
/// </summary>
internal static class DeferredBearerAdapters
{
    internal sealed class Account(HttpClient client) : HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: client);

    internal sealed class Bearer : HttpClientRequestAdapter
    {
        public Bearer(HttpClient client) : this(new DeferredTokens(), client) { }
        private Bearer(DeferredTokens tokens, HttpClient client) : base(new BaseBearerTokenAuthenticationProvider(tokens), httpClient: client) => Tokens = tokens;
        public DeferredTokens Tokens { get; }
    }

    internal sealed class DeferredTokens : IAccessTokenProvider
    {
        public TaminTokenProvider? Provider { get; set; }
        public AllowedHostsValidator AllowedHostsValidator => Required.AllowedHostsValidator;
        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default) =>
            Required.GetAuthorizationTokenAsync(uri, additionalAuthenticationContext, cancellationToken);
        private TaminTokenProvider Required => Provider ?? throw new InvalidOperationException("The token provider has not been connected.");
    }
}
