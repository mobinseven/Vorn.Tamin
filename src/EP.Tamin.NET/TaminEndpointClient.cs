using System.Text.Json;

namespace EP.Tamin.NET;

/// <summary>
/// Composes domain endpoints and delegates transport to the shared API client.
/// </summary>
internal sealed class TaminEndpointClient
{
    private readonly TaminApiClient _api;
    private readonly string _prefix;

    public TaminEndpointClient(TaminApiClient api, string prefix = "")
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _prefix = prefix.Trim('/');
    }

    public Task<JsonElement> GetAsync(
        string endpoint,
        IReadOnlyDictionary<string, string?>? query,
        CancellationToken cancellationToken)
        => _api.GetAsync(BuildEndpoint(endpoint), query, cancellationToken);

    public Task<JsonElement> PostAsync<TPayload>(string endpoint, TPayload payload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return _api.PostAsync(BuildEndpoint(endpoint), payload, cancellationToken);
    }

    public static IReadOnlyDictionary<string, string?> BuildQuery(params (string key, string? value)[] pairs)
    {
        var query = new Dictionary<string, string?>(pairs.Length);
        foreach (var (key, value) in pairs)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Query keys must be provided.", nameof(pairs));

            if (!string.IsNullOrWhiteSpace(value))
                query[key] = value;
        }

        return query;
    }

    private string BuildEndpoint(string endpoint)
    {
        var normalizedEndpoint = endpoint?.Trim('/') ?? string.Empty;

        if (string.IsNullOrWhiteSpace(_prefix))
            return normalizedEndpoint;

        return string.IsNullOrWhiteSpace(normalizedEndpoint)
            ? _prefix
            : $"{_prefix}/{normalizedEndpoint}";
    }
}
