using System.Text.Json;

namespace EP.Tamin.NET;

/// <summary>
/// Provides e-prescription writing, query, and mutation operations.
/// </summary>
public sealed class PrescriptionClient
{
    private readonly TaminEndpointClient _endpointClient;

    internal PrescriptionClient(TaminApiClient apiClient)
    {
        _endpointClient = new TaminEndpointClient(apiClient, "interface/epresc/SendEpresc");
    }

    // ── Low-level / untyped (backward-compatible) ────────────────────────────

    /// <summary>Fetches prescription detail using raw query parameters.</summary>
    public Task<JsonElement> GetPrescriptionDetailAsync(IReadOnlyDictionary<string, string?>? query = null, CancellationToken cancellationToken = default)
        => _endpointClient.GetAsync(string.Empty, query, cancellationToken);

    /// <summary>
    /// Submits an arbitrary prescription payload (escape hatch).
    /// Prefer the strongly-typed <c>RegisterXxx</c> overloads where possible.
    /// </summary>
    public Task<JsonElement> CreatePrescriptionAsync<TPayload>(TPayload payload, CancellationToken cancellationToken = default)
        => _endpointClient.PostAsync(string.Empty, payload, cancellationToken);

    // ── Section 8: E-Prescription Writing ────────────────────────────────────

    /// <summary>Registers a visit-only prescription or encounter (Section 8.1).</summary>
    public Task<JsonElement> RegisterVisitPrescriptionAsync(RegisterVisitPrescriptionRequest request, CancellationToken cancellationToken = default)
        => _endpointClient.PostAsync(string.Empty, request, cancellationToken);

    /// <summary>Submits prescribed drug items (Section 8.2).</summary>
    public Task<JsonElement> RegisterDrugPrescriptionAsync(RegisterDrugPrescriptionRequest request, CancellationToken cancellationToken = default)
        => _endpointClient.PostAsync(string.Empty, request, cancellationToken);

    /// <summary>Submits laboratory, imaging, diagnostic, or other paraclinic orders (Section 8.3).</summary>
    public Task<JsonElement> RegisterParaclinicPrescriptionAsync(RegisterParaclinicPrescriptionRequest request, CancellationToken cancellationToken = default)
        => _endpointClient.PostAsync(string.Empty, request, cancellationToken);

    /// <summary>Submits physician-provided services or other medical service orders (Section 8.4).</summary>
    public Task<JsonElement> RegisterMedicalServicePrescriptionAsync(RegisterMedicalServicePrescriptionRequest request, CancellationToken cancellationToken = default)
        => _endpointClient.PostAsync(string.Empty, request, cancellationToken);

    /// <summary>Registers a referral to another provider, specialty, or service centre (Section 8.5).</summary>
    public Task<JsonElement> RegisterReferralPrescriptionAsync(RegisterReferralPrescriptionRequest request, CancellationToken cancellationToken = default)
        => _endpointClient.PostAsync(string.Empty, request, cancellationToken);

    /// <summary>Registers a physiotherapy prescription (Section 8.6).</summary>
    public Task<JsonElement> RegisterPhysiotherapyPrescriptionAsync(RegisterPhysiotherapyPrescriptionRequest request, CancellationToken cancellationToken = default)
        => _endpointClient.PostAsync(string.Empty, request, cancellationToken);

    // ── Section 9: Prescription Query ────────────────────────────────────────

    /// <summary>Retrieves a previously registered prescription by ID, tracking code, and patient ID (Section 9.1).</summary>
    public Task<JsonElement> GetRegisteredPrescriptionAsync(
        string prescriptionId,
        string trackingCode,
        string patientNationalId,
        CancellationToken cancellationToken = default)
    {
        var query = TaminEndpointClient.BuildQuery(
            ("prescription_id", prescriptionId),
            ("tracking_code", trackingCode),
            ("patient_national_id", patientNationalId));
        return _endpointClient.GetAsync(string.Empty, query, cancellationToken);
    }

    /// <summary>Retrieves referral prescription details (Section 9.2).</summary>
    public Task<JsonElement> GetReferralPrescriptionAsync(
        string referralPrescriptionId,
        string trackingCode,
        string patientNationalId,
        CancellationToken cancellationToken = default)
    {
        var query = TaminEndpointClient.BuildQuery(
            ("referral_prescription_id", referralPrescriptionId),
            ("tracking_code", trackingCode),
            ("patient_national_id", patientNationalId));
        return _endpointClient.GetAsync(string.Empty, query, cancellationToken);
    }

    /// <summary>Retrieves prescriptions by patient, doctor, date range, or status (Section 9.3).</summary>
    public Task<JsonElement> GetPrescriptionListAsync(PrescriptionListFilter filter, CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(filter.DoctorId)) query["doctor_id"] = filter.DoctorId;
        if (!string.IsNullOrWhiteSpace(filter.PatientNationalId)) query["patient_national_id"] = filter.PatientNationalId;
        if (!string.IsNullOrWhiteSpace(filter.FromDate)) query["from_date"] = filter.FromDate;
        if (!string.IsNullOrWhiteSpace(filter.ToDate)) query["to_date"] = filter.ToDate;
        if (filter.PrescriptionType.HasValue) query["prescription_type"] = ((int)filter.PrescriptionType.Value).ToString();
        if (!string.IsNullOrWhiteSpace(filter.Status)) query["status"] = filter.Status;
        return _endpointClient.GetAsync(string.Empty, query, cancellationToken);
    }

    // ── Section 10: Prescription Mutation ────────────────────────────────────

    /// <summary>Edits an already-registered electronic prescription where allowed (Section 10.1).</summary>
    public Task<JsonElement> EditElectronicPrescriptionAsync(EditPrescriptionRequest request, CancellationToken cancellationToken = default)
        => _endpointClient.PostAsync("edit", request, cancellationToken);

    /// <summary>Cancels or deletes a registered electronic prescription where allowed (Section 10.2).</summary>
    public Task<JsonElement> DeleteElectronicPrescriptionAsync(DeletePrescriptionRequest request, CancellationToken cancellationToken = default)
        => _endpointClient.PostAsync("delete", request, cancellationToken);

    // ── Section 14: Warning Services ─────────────────────────────────────────

    /// <summary>Returns warnings before prescription registration or dispensing (Section 14.1).</summary>
    public Task<JsonElement> CheckPrescriptionWarningAsync(CheckWarningRequest request, CancellationToken cancellationToken = default)
        => _endpointClient.PostAsync("warning", request, cancellationToken);
}
