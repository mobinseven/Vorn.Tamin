using System.Text.Json;

namespace EP.Tamin.NET;

/// <summary>
/// Provides paraclinic service delivery operations (Section 13).
/// </summary>
public sealed class ParaclinicClient
{
    private readonly TaminEndpointClient _endpointClient;

    internal ParaclinicClient(TaminApiClient apiClient)
    {
        _endpointClient = new TaminEndpointClient(apiClient, "paraclinic");
    }

    /// <summary>Checks patient treatment entitlement (Section 13.2).</summary>
    public Task<JsonElement> CheckEntitlementAsync(CheckEntitlementRequest request, CancellationToken cancellationToken = default)
        => _endpointClient.PostAsync("check-entitlement", request, cancellationToken);

    /// <summary>Registers a paper prescription, when required by provider type and workflow (Section 13.3).</summary>
    public Task<JsonElement> RegisterPaperPrescriptionAsync(RegisterPaperPrescriptionRequest request, CancellationToken cancellationToken = default)
        => _endpointClient.PostAsync("register-paper", request, cancellationToken);

    /// <summary>Fetches paraclinic prescriptions waiting for service delivery (Section 13.4).</summary>
    public Task<JsonElement> GetPrescriptionListAsync(IReadOnlyDictionary<string, string?>? query = null, CancellationToken cancellationToken = default)
        => _endpointClient.GetAsync("prescription-list", query, cancellationToken);

    /// <summary>Fetches item-level service details for a prescription (Section 13.5).</summary>
    public Task<JsonElement> GetPrescriptionDetailsAsync(string prescriptionId, string trackingCode, CancellationToken cancellationToken = default)
    {
        var query = TaminEndpointClient.BuildQuery(("prescription_id", prescriptionId), ("tracking_code", trackingCode));
        return _endpointClient.GetAsync("prescription-details", query, cancellationToken);
    }

    /// <summary>Registers delivery of a service from a paper prescription (Section 13.6).</summary>
    public Task<JsonElement> ProvidePaperPrescriptionServiceAsync(ProvideServiceRequest request, CancellationToken cancellationToken = default)
        => _endpointClient.PostAsync("provide-paper", request, cancellationToken);

    /// <summary>Registers delivery of an electronic paraclinic service (Section 13.7).</summary>
    public Task<JsonElement> ProvideElectronicPrescriptionServiceAsync(ProvideServiceRequest request, CancellationToken cancellationToken = default)
        => _endpointClient.PostAsync("provide-electronic", request, cancellationToken);

    /// <summary>
    /// Registers delivery where warnings exist and continuation is allowed (Section 13.8).
    /// </summary>
    public Task<JsonElement> ProvideServiceWithWarningAsync(ProvideServiceRequest request, CancellationToken cancellationToken = default)
        => _endpointClient.PostAsync("provide-with-warning", request, cancellationToken);

    /// <summary>Shows tariff, insurance share, and patient share for a service (Section 13.9).</summary>
    public Task<JsonElement> GetPriceAsync(string prescriptionId, string trackingCode, CancellationToken cancellationToken = default)
    {
        var query = TaminEndpointClient.BuildQuery(("prescription_id", prescriptionId), ("tracking_code", trackingCode));
        return _endpointClient.GetAsync("price", query, cancellationToken);
    }

    /// <summary>Deletes or cancels a service delivery record where allowed (Section 13.10).</summary>
    public Task<JsonElement> DeleteServiceDeliveryRecordAsync(DeletePrescriptionRequest request, CancellationToken cancellationToken = default)
        => _endpointClient.PostAsync("delete-delivery", request, cancellationToken);
}
