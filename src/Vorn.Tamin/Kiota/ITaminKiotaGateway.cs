using System.Text.Json;

namespace Vorn.Tamin.Kiota;

/// <summary>
/// Executes friendly-layer requests through the generated Kiota request model without exposing generated builders publicly.
/// </summary>
internal interface ITaminKiotaGateway
{
    /// <summary>Sends a read request and returns the friendly SDK JSON response shape.</summary>
    Task<JsonElement> GetAsync(string endpoint, IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken);

    /// <summary>Sends a write request and returns the friendly SDK JSON response shape.</summary>
    Task<JsonElement> PostAsync<TPayload>(string endpoint, TPayload payload, CancellationToken cancellationToken);
}
