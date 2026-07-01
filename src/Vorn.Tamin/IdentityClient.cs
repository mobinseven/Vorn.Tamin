using Vorn.Tamin.Kiota;

namespace Vorn.Tamin;

/// <summary>
/// Placeholder for identity-specific operations. Currently empty because no separate identity endpoints
/// exist beyond patient eligibility verification. For identity verification and entitlement checking,
/// use <see cref="EligibilityClient.LookupPrivatePracticeAsync"/> which calls the EP.Tamin "deserve-info"
/// endpoint. This client will remain empty until distinct identity operations are added to the generated
/// Kiota request builders.
/// </summary>
public sealed class IdentityClient
{
    internal IdentityClient(ITaminKiotaGateway gateway)
    {
        ArgumentNullException.ThrowIfNull(gateway);
    }
}
