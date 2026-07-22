using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Polly;

namespace Tamin.Integration.Http;

public sealed class TaminApiException(int? statusCode, string operationId, string rawBody) : Exception($"Tamin operation '{operationId}' failed with HTTP {statusCode?.ToString() ?? "unknown"}.")
{
    public int? StatusCode { get; } = statusCode;
    public string OperationId { get; } = operationId;
    public string RawBody { get; } = rawBody;
}

public sealed class TaminResponseHandler(ILogger<TaminResponseHandler> logger, Func<HttpRequestMessage, string> operationIdResolver) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode) return response;
        var rawBody = response.Content is null ? string.Empty : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var operationId = operationIdResolver(request);
        // Raw content stays on the exception for contract analysis; logs redact known credential fields.
        var statusCode = (int)response.StatusCode;
        logger.LogWarning("Tamin API failure {OperationId} {StatusCode}: {Body}", operationId, statusCode, Redact(rawBody));
        response.Dispose();
        throw new TaminApiException(statusCode, operationId, rawBody);
    }

    private static string Redact(string body)
    {
        return Regex.Replace(body, "(?i)(\\\"(?:access_token|refresh_token|code|code_verifier)\\\"\\s*:\\s*)\\\"[^\\\"]*\\\"", "$1\"[REDACTED]\"");
    }
}

/// <summary>Retries only safe reads: the contract documents no mutation idempotency key.</summary>
public sealed class TaminTransientFaultHandler : DelegatingHandler
{
    private static readonly IAsyncPolicy<HttpResponseMessage> Policy = Polly.Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .Or<TaskCanceledException>()
        .OrResult(response => (int)response.StatusCode >= 500)
        .WaitAndRetryAsync(2, attempt => TimeSpan.FromMilliseconds(200 * attempt), (outcome, _, _, _) => outcome.Result?.Dispose());

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        request.Method == HttpMethod.Get ? Policy.ExecuteAsync(ct => base.SendAsync(request, ct), cancellationToken) : base.SendAsync(request, cancellationToken);
}
