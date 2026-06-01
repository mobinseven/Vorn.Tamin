using System.Text.Json;

namespace Vorn.Tamin;

/// <summary>
/// Provides patient identity verification and treatment eligibility operations (Section 7).
/// </summary>
public sealed class IdentityClient
{
    private readonly TaminEndpointClient _endpointClient;

    internal IdentityClient(TaminApiClient apiClient)
    {
        _endpointClient = new TaminEndpointClient(apiClient);
    }

    /// <summary>
    /// Verifies a patient's identity before issuing a prescription (Section 7.1).
    /// </summary>
    /// <param name="request">Identity verification parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<JsonElement> VerifyIdentityAsync(VerifyIdentityRequest request, CancellationToken cancellationToken = default)
        => _endpointClient.PostAsync("ws-verify-identity", request, cancellationToken);

    /// <summary>
    /// Checks whether a patient has active treatment coverage (Section 7.2).
    /// </summary>
    /// <param name="request">Entitlement check parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<JsonElement> CheckEntitlementAsync(CheckEntitlementRequest request, CancellationToken cancellationToken = default)
        => _endpointClient.PostAsync("ws-check-entitlement", request, cancellationToken);
}
