using System.Text.Json;
using Vorn.Tamin.Kiota;

namespace Vorn.Tamin;

/// <summary>
/// Provides patient identity verification and treatment eligibility operations (Section 7).
/// </summary>
public sealed class IdentityClient
{
    private readonly ITaminKiotaGateway _gateway;

    internal IdentityClient(ITaminKiotaGateway gateway)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
    }

    /// <summary>
    /// Verifies a patient's identity before issuing a prescription (Section 7.1).
    /// </summary>
    /// <param name="request">Identity verification parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<JsonElement> VerifyIdentityAsync(VerifyIdentityRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync("ws-verify-identity", request, cancellationToken);

    /// <summary>
    /// Checks whether a patient has active treatment coverage (Section 7.2).
    /// </summary>
    /// <param name="request">Entitlement check parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<JsonElement> CheckEntitlementAsync(CheckEntitlementRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync("ws-check-entitlement", request, cancellationToken);
}
