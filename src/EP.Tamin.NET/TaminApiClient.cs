using System.Text;
using System.Text.Json;

namespace EP.Tamin.NET;

/// <summary>
/// Sends HTTP requests for domain clients and applies the shared EP.Tamin response contract.
/// </summary>
internal sealed class TaminApiClient
{
    private readonly TaminSession _session;

    public TaminApiClient(TaminSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
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

        using var request = _session.CreateRequest(method, _session.BuildUri(endpoint, query));
        if (payload is not null)
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _session.HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ResponseHandling.Handle(response.StatusCode, response.ReasonPhrase, content);
    }
}
