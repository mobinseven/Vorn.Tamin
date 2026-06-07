namespace Vorn.Tamin;

/// <summary>Owns operation-level production and sandbox route definitions.</summary>
public sealed class TaminEnvironmentRoutes
{
    private static readonly IReadOnlyDictionary<(TaminEndpoint Environment, TaminOperation Operation), string> Paths =
        new Dictionary<(TaminEndpoint, TaminOperation), string>
        {
            [(TaminEndpoint.Production, TaminOperation.Authorize)] = "auth/server/authorize",
            [(TaminEndpoint.Production, TaminOperation.TokenExchange)] = "auth/server/token",
            [(TaminEndpoint.Production, TaminOperation.RefreshTokenV2)] = "auth/server/v2/token",
            [(TaminEndpoint.Production, TaminOperation.SignOut)] = "auth/signout",
            [(TaminEndpoint.Production, TaminOperation.GetServices)] = "interface/epresc/SendEpresc/v2/services",
            [(TaminEndpoint.Production, TaminOperation.SendPrescription)] = "interface/epresc/SendEpresc/v2",
            [(TaminEndpoint.Production, TaminOperation.GetPrescription)] = "interface/epresc/SendEpresc/v2/{headerId}/{doctorId}",
            [(TaminEndpoint.Production, TaminOperation.EditPrescription)] = "interface/epresc/SendEpresc/v2/edit/{headerId}/{doctorId}",
            [(TaminEndpoint.Production, TaminOperation.RemovePrescription)] = "interface/epresc/SendEpresc/v2/remove/{headerId}/{doctorId}",
            [(TaminEndpoint.Production, TaminOperation.CheckPrescriptionWarning)] = "interface/epresc/SendEpresc/v2/check-rules-in-detail",

            [(TaminEndpoint.Sandbox, TaminOperation.Authorize)] = "auth/server/authorize",
            [(TaminEndpoint.Sandbox, TaminOperation.TokenExchange)] = "auth/server/token",
            [(TaminEndpoint.Sandbox, TaminOperation.RefreshTokenV2)] = "auth/server/v2/token",
            [(TaminEndpoint.Sandbox, TaminOperation.SignOut)] = "auth/signout",
            [(TaminEndpoint.Sandbox, TaminOperation.GetServices)] = "api/v2/ws-services",
            [(TaminEndpoint.Sandbox, TaminOperation.SendPrescription)] = "api/v2/SendEpresc",
            [(TaminEndpoint.Sandbox, TaminOperation.GetPrescription)] = "api/v2/ep/{headerId}/{doctorNationalCode}/{doctorId}/detail",
            [(TaminEndpoint.Sandbox, TaminOperation.EditPrescription)] = "api/v2/ep/update/{headerId}/{doctorNationalCode}/{doctorId}",
            [(TaminEndpoint.Sandbox, TaminOperation.RemovePrescription)] = "api/v2/ep/{headerId}/{doctorNationalCode}/{doctorId}",
            [(TaminEndpoint.Sandbox, TaminOperation.CheckPrescriptionWarning)] = "api/v2/check-rules-in-detail"
        };

    private readonly Uri _productionBaseUri;
    private readonly Uri _sandboxBaseUri;

    /// <summary>Creates route presets with the SDK's default production and sandbox base URIs.</summary>
    public TaminEnvironmentRoutes()
        : this(
            new Uri(TaminSession.DefaultBaseUrl(TaminEndpoint.Production)),
            new Uri(TaminSession.DefaultBaseUrl(TaminEndpoint.Sandbox)))
    {
    }

    /// <summary>Creates route presets using explicit environment base URIs.</summary>
    public TaminEnvironmentRoutes(Uri productionBaseUri, Uri sandboxBaseUri)
    {
        _productionBaseUri = EnsureTrailingSlash(productionBaseUri ?? throw new ArgumentNullException(nameof(productionBaseUri)));
        _sandboxBaseUri = EnsureTrailingSlash(sandboxBaseUri ?? throw new ArgumentNullException(nameof(sandboxBaseUri)));
    }

    /// <summary>Resolves the route for the selected environment and provider operation.</summary>
    public TaminRoute Resolve(TaminEndpoint environment, TaminOperation operation)
    {
        if (!Paths.TryGetValue((environment, operation), out var relativePath))
            throw new TaminRouteNotDefinedException(environment, operation);

        return new TaminRoute(environment, operation, new Uri(BaseUri(environment), relativePath));
    }

    private Uri BaseUri(TaminEndpoint environment)
        => environment switch
        {
            TaminEndpoint.Production => _productionBaseUri,
            TaminEndpoint.Sandbox => _sandboxBaseUri,
            _ => throw new TaminRouteNotDefinedException(environment, null)
        };

    private static Uri EnsureTrailingSlash(Uri uri)
        => uri.AbsoluteUri.EndsWith('/') ? uri : new Uri($"{uri.AbsoluteUri}/");
}
