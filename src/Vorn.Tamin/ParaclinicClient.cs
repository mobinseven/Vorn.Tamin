using System.Text.Json;
using Vorn.Tamin.Kiota;

namespace Vorn.Tamin;

/// <summary>
/// Provides paraclinic service delivery operations (Section 13).
/// </summary>
public sealed class ParaclinicClient
{
    private readonly ITaminKiotaGateway _gateway;

    internal ParaclinicClient(ITaminKiotaGateway gateway)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
    }

    /// <summary>Checks patient treatment entitlement (Section 13.2).</summary>
    public Task<JsonElement> CheckEntitlementAsync(CheckEntitlementRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync(TaminGatewayRoute.ParaclinicCheckEntitlement, request, cancellationToken);

    /// <summary>Registers a paper prescription, when required by provider type and workflow (Section 13.3).</summary>
    public Task<JsonElement> RegisterPaperPrescriptionAsync(RegisterPaperPrescriptionRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync(TaminGatewayRoute.ParaclinicRegisterPaper, request, cancellationToken);

    /// <summary>Fetches paraclinic prescriptions waiting for service delivery (Section 13.4).</summary>
    public Task<JsonElement> GetPrescriptionListAsync(IReadOnlyDictionary<string, string?>? query = null, CancellationToken cancellationToken = default)
        => _gateway.GetAsync(TaminGatewayRoute.ParaclinicPrescriptionList, query, cancellationToken);

    /// <summary>Fetches item-level service details for a prescription (Section 13.5).</summary>
    public Task<JsonElement> GetPrescriptionDetailsAsync(string prescriptionId, string trackingCode, CancellationToken cancellationToken = default)
    {
        var query = TaminQueryParameters.Build(("prescription_id", prescriptionId), ("tracking_code", trackingCode));
        return _gateway.GetAsync(TaminGatewayRoute.ParaclinicPrescriptionDetails, query, cancellationToken);
    }

    /// <summary>Registers delivery of a service from a paper prescription (Section 13.6).</summary>
    public Task<JsonElement> ProvidePaperPrescriptionServiceAsync(ProvideServiceRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync(TaminGatewayRoute.ParaclinicProvidePaper, request, cancellationToken);

    /// <summary>Registers delivery of an electronic paraclinic service (Section 13.7).</summary>
    public Task<JsonElement> ProvideElectronicPrescriptionServiceAsync(ProvideServiceRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync(TaminGatewayRoute.ParaclinicProvideElectronic, request, cancellationToken);

    /// <summary>
    /// Registers delivery where warnings exist and continuation is allowed (Section 13.8).
    /// </summary>
    public Task<JsonElement> ProvideServiceWithWarningAsync(ProvideServiceRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync(TaminGatewayRoute.ParaclinicProvideWithWarning, request, cancellationToken);

    /// <summary>Shows tariff, insurance share, and patient share for a service (Section 13.9).</summary>
    public Task<JsonElement> GetPriceAsync(string prescriptionId, string trackingCode, CancellationToken cancellationToken = default)
    {
        var query = TaminQueryParameters.Build(("prescription_id", prescriptionId), ("tracking_code", trackingCode));
        return _gateway.GetAsync(TaminGatewayRoute.ParaclinicPrice, query, cancellationToken);
    }

    /// <summary>Deletes or cancels a service delivery record where allowed (Section 13.10).</summary>
    public Task<JsonElement> DeleteServiceDeliveryRecordAsync(DeletePrescriptionRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync(TaminGatewayRoute.ParaclinicDeleteDelivery, request, cancellationToken);
}
