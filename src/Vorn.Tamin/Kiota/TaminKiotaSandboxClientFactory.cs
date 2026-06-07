using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Serialization.Json;
using Vorn.Tamin.Kiota.Sandbox;

namespace Vorn.Tamin.Kiota;

/// <summary>Creates generated sandbox Kiota clients configured with the friendly session's base URL.</summary>
internal sealed class TaminKiotaSandboxClientFactory
{
    public const string DefaultBaseUrl = "https://ep-test.tamin.ir/";

    public TaminKiotaSandboxClient Create(HttpClient httpClient, Uri baseUri)
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

        return new TaminKiotaSandboxClient(adapter);
    }
}
