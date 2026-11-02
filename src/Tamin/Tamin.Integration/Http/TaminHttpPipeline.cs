using Microsoft.Extensions.Logging;
using Polly;

namespace Tamin.Integration.Http;

/// <param name="statusCode">Null when the response was successful but its body was empty or unreadable.</param>
/// <param name="rawBody">The provider's body. It can hold personal data: do not log it unredacted.</param>
public sealed class TaminApiException(int? statusCode, string operationId, string rawBody, Exception? innerException = null)
    : Exception($"Tamin operation '{operationId}' failed with {(statusCode is null ? "an unreadable response" : $"HTTP {statusCode}")}.", innerException)
{
    public int? StatusCode { get; } = statusCode;
    public string OperationId { get; } = operationId;
    public string RawBody { get; } = rawBody;
}

internal sealed class TaminResponseHandler(ILogger<TaminResponseHandler> logger, Func<HttpRequestMessage, string> operationIdResolver) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode) return response;
        var rawBody = response.Content is null ? string.Empty : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var operationId = operationIdResolver(request);
        var statusCode = (int)response.StatusCode;
        // Bodies and URLs carry national codes and patient data, so neither is logged. The body stays on the exception.
        logger.LogWarning("Tamin API failure {OperationId} {StatusCode}", operationId, statusCode);
        response.Dispose();
        throw new TaminApiException(statusCode, operationId, rawBody);
    }
}

/// <summary>Retries only safe reads: the contract documents no mutation idempotency key.</summary>
internal sealed class TaminTransientFaultHandler : DelegatingHandler
{
    private static readonly IAsyncPolicy<HttpResponseMessage> Policy = Polly.Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .Or<TaskCanceledException>()
        .OrResult(response => (int)response.StatusCode >= 500)
        .WaitAndRetryAsync(2, attempt => TimeSpan.FromMilliseconds(200 * attempt), (outcome, _, _, _) => outcome.Result?.Dispose());

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        request.Method == HttpMethod.Get ? Policy.ExecuteAsync(ct => base.SendAsync(request, ct), cancellationToken) : base.SendAsync(request, cancellationToken);
}
