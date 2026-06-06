using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Kiota.Abstractions;

namespace Vorn.Tamin.Kiota;

/// <summary>Translates friendly SDK operations into Kiota request information and executes them with common headers.</summary>
internal sealed class TaminKiotaGateway : ITaminKiotaGateway
{
    private readonly HttpClient _httpClient;
    private readonly Uri _baseUri;
    private readonly string? _clientId;
    private readonly TaminOpenAPIClient _client;

    public TaminKiotaGateway(HttpClient httpClient, Uri baseUri, string? oauthToken, string? clientId, TaminKiotaClientFactory? clientFactory = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
        _clientId = clientId;

        if (!string.IsNullOrWhiteSpace(oauthToken))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oauthToken);

        if (!string.IsNullOrWhiteSpace(clientId))
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Client-Id", clientId);

        _client = (clientFactory ?? new TaminKiotaClientFactory()).Create(_httpClient, _baseUri);
    }

    public Task<JsonElement> GetAsync(string endpoint, IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken)
    {
        var requestInfo = CreateRequestInformation<object>(Method.GET, endpoint, query, payload: null);
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> PostAsync<TPayload>(string endpoint, TPayload payload, CancellationToken cancellationToken)
    {
        if (payload is null)
            throw new ArgumentNullException(nameof(payload));

        var requestInfo = CreateRequestInformation(Method.POST, endpoint, query: null, payload);
        return SendAsync(requestInfo, cancellationToken);
    }

    private RequestInformation CreateRequestInformation<TPayload>(
        Method method,
        string endpoint,
        IReadOnlyDictionary<string, string?>? query,
        TPayload? payload)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            endpoint = "interface/epresc/SendEpresc";

        var requestInfo = new RequestInformation
        {
            HttpMethod = method,
            URI = BuildUri(endpoint, query)
        };
        requestInfo.Headers.TryAdd("Accept", "application/json");
        requestInfo.Headers.TryAdd("Request-Id", Guid.NewGuid().ToString());
        if (!string.IsNullOrWhiteSpace(_clientId))
            requestInfo.Headers.TryAdd("Client-Id", _clientId);

        if (payload is not null)
        {
            requestInfo.Headers.TryAdd("Content-Type", "application/json");
            requestInfo.Content = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        }

        _ = _client;
        return requestInfo;
    }

    private async Task<JsonElement> SendAsync(RequestInformation requestInfo, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(ToHttpMethod(requestInfo.HttpMethod), requestInfo.URI);
        foreach (var header in requestInfo.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (requestInfo.Content is not null)
        {
            requestInfo.Content.Position = 0;
            request.Content = new StreamContent(requestInfo.Content);
            if (requestInfo.Headers.TryGetValue("Content-Type", out var contentTypes))
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentTypes.First());
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ResponseHandling.Handle(response.StatusCode, response.ReasonPhrase, content);
    }

    private Uri BuildUri(string endpoint, IReadOnlyDictionary<string, string?>? query)
    {
        var absolute = new Uri(_baseUri, endpoint.TrimStart('/'));
        if (query is null || query.Count == 0)
            return absolute;

        var queryString = string.Join("&", query
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}"));

        if (string.IsNullOrEmpty(queryString))
            return absolute;

        var separator = absolute.Query.Length == 0 ? "?" : "&";
        return new Uri($"{absolute}{separator}{queryString}");
    }

    private static HttpMethod ToHttpMethod(Method method)
        => method switch
        {
            Method.GET => HttpMethod.Get,
            Method.POST => HttpMethod.Post,
            Method.PUT => HttpMethod.Put,
            Method.PATCH => HttpMethod.Patch,
            Method.DELETE => HttpMethod.Delete,
            _ => throw new NotSupportedException($"HTTP method '{method}' is not supported by the friendly Kiota gateway.")
        };
}
