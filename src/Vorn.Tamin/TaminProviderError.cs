using System.Net;

namespace Vorn.Tamin;

/// <summary>Preserves raw provider failure details with SDK normalization context.</summary>
/// <param name="Category">Remediation category assigned by the normalizer.</param>
/// <param name="Code">Stable SDK error catalog code.</param>
/// <param name="OperationName">Provider operation that produced the failure, when known.</param>
/// <param name="Environment">Provider environment that produced the failure, when known.</param>
/// <param name="StatusCode">HTTP status code returned by the provider, when available.</param>
/// <param name="ReasonPhrase">HTTP reason phrase returned by the provider, when available.</param>
/// <param name="ProviderMessage">Provider message extracted from the response body, when available.</param>
/// <param name="ProviderBody">Raw provider response body.</param>
public sealed record TaminProviderError(
    TaminErrorCategory Category,
    string Code,
    string? OperationName,
    TaminEndpoint? Environment,
    HttpStatusCode? StatusCode,
    string? ReasonPhrase,
    string? ProviderMessage,
    string? ProviderBody);
