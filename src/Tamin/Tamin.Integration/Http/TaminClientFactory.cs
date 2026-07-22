using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Extensions.Logging;
using Tamin.Integration.Auth;
using ProdAccountClient = Tamin.Client.Account.Prod.ProdAccountClient;
using ProdApiClient = Tamin.Client.Api.Prod.ProdApiClient;
using ProdSoaClient = Tamin.Client.Soa.Prod.ProdSoaClient;
using PilotAccountClient = Tamin.Client.Account.Pilot.PilotAccountClient;
using PilotApiClient = Tamin.Client.Api.Pilot.PilotApiClient;
using PilotSoaClient = Tamin.Client.Soa.Pilot.PilotSoaClient;

namespace Tamin.Integration.Http;

public sealed record TaminClientBases(Uri Account, Uri Soa, Uri Api);
public sealed record ProductionTaminClients(ProdAccountClient Account, ProdSoaClient Soa, ProdApiClient Api);
public sealed record PilotTaminClients(PilotAccountClient Account, PilotSoaClient Soa, PilotApiClient Api);

public static class TaminClientFactory
{
    public static ProductionTaminClients CreateProduction(TaminClientBases bases, TaminTokenProvider tokenProvider, HttpMessageHandler primaryHandler, ILoggerFactory loggerFactory, Func<HttpRequestMessage, string> operationIdResolver)
    {
        ArgumentNullException.ThrowIfNull(primaryHandler);
        ArgumentNullException.ThrowIfNull(operationIdResolver);
        var accountAdapter = CreateAdapter(new AnonymousAuthenticationProvider(), bases.Account, primaryHandler, loggerFactory, operationIdResolver);
        var bearer = new BaseBearerTokenAuthenticationProvider(tokenProvider);
        var soaAdapter = CreateAdapter(bearer, bases.Soa, primaryHandler, loggerFactory, operationIdResolver);
        var apiAdapter = CreateAdapter(bearer, bases.Api, primaryHandler, loggerFactory, operationIdResolver);
        return new(new ProdAccountClient(accountAdapter), new ProdSoaClient(soaAdapter), new ProdApiClient(apiAdapter));
    }

    public static PilotTaminClients CreatePilot(TaminClientBases bases, TaminTokenProvider tokenProvider, HttpMessageHandler primaryHandler, ILoggerFactory loggerFactory, Func<HttpRequestMessage, string> operationIdResolver)
    {
        ArgumentNullException.ThrowIfNull(primaryHandler); ArgumentNullException.ThrowIfNull(operationIdResolver);
        var accountAdapter = CreateAdapter(new AnonymousAuthenticationProvider(), bases.Account, primaryHandler, loggerFactory, operationIdResolver);
        var bearer = new BaseBearerTokenAuthenticationProvider(tokenProvider);
        var soaAdapter = CreateAdapter(bearer, bases.Soa, primaryHandler, loggerFactory, operationIdResolver);
        var apiAdapter = CreateAdapter(bearer, bases.Api, primaryHandler, loggerFactory, operationIdResolver);
        return new(new PilotAccountClient(accountAdapter), new PilotSoaClient(soaAdapter), new PilotApiClient(apiAdapter));
    }

    private static HttpClientRequestAdapter CreateAdapter(IAuthenticationProvider authentication, Uri baseUrl, HttpMessageHandler primaryHandler, ILoggerFactory loggerFactory, Func<HttpRequestMessage, string> operationIdResolver)
    {
        var retry = new TaminTransientFaultHandler { InnerHandler = primaryHandler };
        var capture = new TaminResponseHandler(loggerFactory.CreateLogger<TaminResponseHandler>(), operationIdResolver) { InnerHandler = retry };
        var httpClient = new HttpClient(capture, disposeHandler: false) { BaseAddress = baseUrl };
        return new HttpClientRequestAdapter(authentication, httpClient: httpClient) { BaseUrl = baseUrl.ToString().TrimEnd('/') };
    }
}
