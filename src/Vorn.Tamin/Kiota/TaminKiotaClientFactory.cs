using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Serialization.Json;

namespace Vorn.Tamin.Kiota;

/// <summary>Creates generated Kiota clients configured with the friendly session's base URL.</summary>
internal sealed class TaminKiotaClientFactory
{
    public const string DefaultBaseUrl = "https://soa.tamin.ir/";

    public TaminKiotaClient Create(HttpClient httpClient, Uri baseUri)
    {
        if (httpClient is null)
            throw new ArgumentNullException(nameof(httpClient));
        if (baseUri is null)
            throw new ArgumentNullException(nameof(baseUri));

        var adapter = new HttpClientRequestAdapter(
            new AnonymousAuthenticationProvider(),
            new JsonParseNodeFactory(),
            new JsonSerializationWriterFactory(),
            httpClient,
            observabilityOptions: null);
        adapter.BaseUrl = baseUri.ToString().TrimEnd('/');

        return new TaminKiotaClient(adapter);
    }
}
