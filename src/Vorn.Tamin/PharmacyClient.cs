using System.Text.Json;
using Vorn.Tamin.Kiota;

namespace Vorn.Tamin;

/// <summary>
/// Provides pharmacy dispensing operations (Section 12).
/// </summary>
public sealed class PharmacyClient
{
    private readonly ITaminKiotaGateway _gateway;

    internal PharmacyClient(ITaminKiotaGateway gateway)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
    }

    /// <summary>Checks patient entitlement before dispensing (Section 12.2).</summary>
    public Task<JsonElement> CheckEntitlementAsync(CheckEntitlementRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync(TaminGatewayRoute.PharmacyCheckEntitlement, request, cancellationToken);

    /// <summary>Registers a paper prescription in the pharmacy system (Section 12.3).</summary>
    public Task<JsonElement> RegisterPaperPrescriptionAsync(RegisterPaperPrescriptionRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync(TaminGatewayRoute.PharmacyRegisterPaper, request, cancellationToken);

    /// <summary>Fetches available prescriptions for dispensing (Section 12.4).</summary>
    public Task<JsonElement> GetPrescriptionListAsync(IReadOnlyDictionary<string, string?>? query = null, CancellationToken cancellationToken = default)
        => _gateway.GetAsync(TaminGatewayRoute.PharmacyPrescriptionList, query, cancellationToken);

    /// <summary>Fetches prescription item-level details (Section 12.5).</summary>
    public Task<JsonElement> GetPrescriptionDetailsAsync(string prescriptionId, string trackingCode, CancellationToken cancellationToken = default)
    {
        var query = TaminQueryParameters.Build(("prescription_id", prescriptionId), ("tracking_code", trackingCode));
        return _gateway.GetAsync(TaminGatewayRoute.PharmacyPrescriptionDetails, query, cancellationToken);
    }

    /// <summary>Returns or refers a prescription to the doctor for correction (Section 12.6).</summary>
    public Task<JsonElement> ReferPrescriptionToDoctorAsync(ReferPrescriptionRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync(TaminGatewayRoute.PharmacyReferToDoctor, request, cancellationToken);

    /// <summary>Checks dispensing warnings before final delivery (Section 12.7).</summary>
    public Task<JsonElement> CheckPrescriptionWarningsAsync(CheckWarningRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync(TaminGatewayRoute.PharmacyCheckWarnings, request, cancellationToken);

    /// <summary>Registers delivery of paper prescription items (Section 12.8).</summary>
    public Task<JsonElement> DispensePaperPrescriptionAsync(DispensePrescriptionRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync(TaminGatewayRoute.PharmacyDispensePaper, request, cancellationToken);

    /// <summary>Registers delivery of electronic prescription items (Section 12.9).</summary>
    public Task<JsonElement> DispenseElectronicPrescriptionAsync(DispensePrescriptionRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync(TaminGatewayRoute.PharmacyDispenseElectronic, request, cancellationToken);

    /// <summary>
    /// Registers delivery when a warning exists and the workflow allows continuation (Section 12.10).
    /// </summary>
    public Task<JsonElement> DispenseWithWarningAsync(DispensePrescriptionRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync(TaminGatewayRoute.PharmacyDispenseWithWarning, request, cancellationToken);

    /// <summary>Registers a drug authenticity or tracking code (Section 12.11).</summary>
    public Task<JsonElement> RegisterDrugAuthenticityCodeAsync(DrugAuthenticityRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync(TaminGatewayRoute.PharmacyRegisterAuthenticityCode, request, cancellationToken);

    /// <summary>Activates an authenticity code after delivery (Section 12.12).</summary>
    public Task<JsonElement> ActivateDrugAuthenticityCodeAsync(DrugAuthenticityRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync(TaminGatewayRoute.PharmacyActivateAuthenticityCode, request, cancellationToken);

    /// <summary>Supports a two-step delivery process where required (Section 12.13).</summary>
    public Task<JsonElement> TwoStepElectronicDispensingAsync(DispensePrescriptionRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync(TaminGatewayRoute.PharmacyTwoStepDispense, request, cancellationToken);

    /// <summary>Shows an activated prescription or drug barcode (Section 12.14).</summary>
    public Task<JsonElement> GetActivatedBarcodeAsync(string prescriptionId, string trackingCode, CancellationToken cancellationToken = default)
    {
        var query = TaminQueryParameters.Build(("prescription_id", prescriptionId), ("tracking_code", trackingCode));
        return _gateway.GetAsync(TaminGatewayRoute.PharmacyActivatedBarcode, query, cancellationToken);
    }

    /// <summary>Shows price and share details for the dispensing (Section 12.15).</summary>
    public Task<JsonElement> GetPriceAsync(string prescriptionId, string trackingCode, CancellationToken cancellationToken = default)
    {
        var query = TaminQueryParameters.Build(("prescription_id", prescriptionId), ("tracking_code", trackingCode));
        return _gateway.GetAsync(TaminGatewayRoute.PharmacyPrice, query, cancellationToken);
    }

    /// <summary>Deletes or cancels a dispensing operation where allowed (Section 12.16).</summary>
    public Task<JsonElement> DeleteDispensingRecordAsync(DeletePrescriptionRequest request, CancellationToken cancellationToken = default)
        => _gateway.PostAsync(TaminGatewayRoute.PharmacyDeleteDispensing, request, cancellationToken);
}
