using Vorn.Tamin.Kiota;

namespace Vorn.Tamin;

/// <summary>
/// Represents pharmacy operations; pharmacy provider paths are unsupported until they exist in the generated Kiota client.
/// </summary>
public sealed class PharmacyClient
{
    internal PharmacyClient(ITaminKiotaGateway gateway)
    {
        ArgumentNullException.ThrowIfNull(gateway);
    }
}
