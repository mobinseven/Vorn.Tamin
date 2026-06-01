using System.Text;
using System.Text.Json;

namespace EP.Tamin.NET;

/// <summary>
/// Sends HTTP requests for domain clients and applies the shared EP.Tamin response contract.
/// Owns all transport concerns: URI composition, request creation, and response handling.
/// </summary>
internal sealed class TaminApiClient
{
    private readonly HttpClient _httpClient;
    private readonly Uri _baseUri;

    public TaminApiClient(HttpClient httpClient, Uri baseUri)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
    }

    public Task<JsonElement> GetAsync(
        string endpoint,
        IReadOnlyDictionary<string, string?>? query = null,
        CancellationToken cancellationToken = default)
        => SendAsync<object>(HttpMethod.Get, endpoint, query, payload: null, cancellationToken);

    public Task<JsonElement> PostAsync<TPayload>(
        string endpoint,
        TPayload payload,
        CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, endpoint, query: null, payload, cancellationToken);

    private async Task<JsonElement> SendAsync<TPayload>(
        HttpMethod method,
        string endpoint,
        IReadOnlyDictionary<string, string?>? query,
        TPayload? payload,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint is required.", nameof(endpoint));

        using var request = CreateRequest(method, BuildUri(endpoint, query));
        if (payload is not null)
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ResponseHandling.Handle(response.StatusCode, response.ReasonPhrase, content);
    }

    internal Uri BuildUri(string endpoint, IReadOnlyDictionary<string, string?>? query = null)
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

    private static HttpRequestMessage CreateRequest(HttpMethod method, Uri uri)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.TryAddWithoutValidation("Request-Id", Guid.NewGuid().ToString());
        return request;
    }
}
