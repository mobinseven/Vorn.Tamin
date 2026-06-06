using Vorn.Tamin.Kiota;

namespace Vorn.Tamin;

/// <summary>
/// Represents paraclinic operations; paraclinic provider paths are unsupported until they exist in the generated Kiota client.
/// </summary>
public sealed class ParaclinicClient
{
    internal ParaclinicClient(ITaminKiotaGateway gateway)
    {
        ArgumentNullException.ThrowIfNull(gateway);
    }
}
