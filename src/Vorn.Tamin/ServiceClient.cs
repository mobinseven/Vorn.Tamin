using System.Text.Json;
using Vorn.Tamin.Kiota;

namespace Vorn.Tamin;

/// <summary>
/// Provides access to EP.Tamin reference-data and service-lookup endpoints.
/// </summary>
public sealed class ServiceClient
{
    private readonly ITaminKiotaGateway _gateway;

    internal ServiceClient(ITaminKiotaGateway gateway)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
    }

    // ── Legacy / untyped helpers (backward-compatible) ───────────────────────

    /// <summary>Fetches the raw service list from <c>ws-services</c>.</summary>
    public Task<JsonElement> GetAllServicesAsync(IReadOnlyDictionary<string, string?>? query = null, CancellationToken cancellationToken = default)
        => _gateway.GetAsync("ws-services", query, cancellationToken);

    /// <summary>Fetches available prescription types from <c>ws-prescription-type</c>.</summary>
    public Task<JsonElement> GetPrescriptionTypeAsync(IReadOnlyDictionary<string, string?>? query = null, CancellationToken cancellationToken = default)
        => _gateway.GetAsync("ws-prescription-type", query, cancellationToken);

    /// <summary>Fetches the paraclinic tariff list from <c>ws-par-taref</c>.</summary>
    public Task<JsonElement> GetParaclinicTarefAsync(IReadOnlyDictionary<string, string?>? query = null, CancellationToken cancellationToken = default)
        => _gateway.GetAsync("ws-par-taref", query, cancellationToken);

    /// <summary>Fetches drug amounts / reference data from <c>ws-drug-amount</c>.</summary>
    public Task<JsonElement> GetDrugAmountAsync(IReadOnlyDictionary<string, string?>? query = null, CancellationToken cancellationToken = default)
        => _gateway.GetAsync("ws-drug-amount", query, cancellationToken);

    /// <summary>Fetches drug administration instructions from <c>ws-drug-instruction</c>.</summary>
    public Task<JsonElement> GetDrugInstructionAsync(IReadOnlyDictionary<string, string?>? query = null, CancellationToken cancellationToken = default)
        => _gateway.GetAsync("ws-drug-instruction", query, cancellationToken);

    // ── Typed reference-data methods (Section 11) ────────────────────────────

    /// <summary>
    /// Retrieves the official drug reference list.
    /// Maps to <c>ws-drug-amount</c>.
    /// </summary>
    /// <param name="searchText">Optional free-text filter.</param>
    /// <param name="drugCode">Optional exact drug code filter.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Results per page.</param>
    /// <param name="activeOnly">When <c>true</c>, returns only active drugs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<JsonElement> GetDrugListAsync(
        string? searchText = null,
        string? drugCode = null,
        int? page = null,
        int? pageSize = null,
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var query = TaminQueryParameters.Build(
            ("search_text", searchText),
            ("drug_code", drugCode),
            ("page", page?.ToString()),
            ("page_size", pageSize?.ToString()),
            ("active_only", activeOnly?.ToString().ToLowerInvariant()));
        return _gateway.GetAsync("ws-drug-amount", query, cancellationToken);
    }

    /// <summary>
    /// Retrieves the official service reference list.
    /// Maps to <c>ws-services</c>.
    /// </summary>
    /// <param name="serviceType">Filter by service type.</param>
    /// <param name="serviceGroup">Filter by service group.</param>
    /// <param name="searchText">Optional free-text filter.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Results per page.</param>
    /// <param name="activeOnly">When <c>true</c>, returns only active services.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<JsonElement> GetServiceListAsync(
        string? serviceType = null,
        string? serviceGroup = null,
        string? searchText = null,
        int? page = null,
        int? pageSize = null,
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var query = TaminQueryParameters.Build(
            ("service_type", serviceType),
            ("service_group", serviceGroup),
            ("search_text", searchText),
            ("page", page?.ToString()),
            ("page_size", pageSize?.ToString()),
            ("active_only", activeOnly?.ToString().ToLowerInvariant()));
        return _gateway.GetAsync("ws-services", query, cancellationToken);
    }

    /// <summary>
    /// Shows the allowed quantity / count / limitation for a selected drug or service.
    /// Maps to <c>ws-allowed-count</c>.
    /// </summary>
    /// <param name="patientNationalId">Patient national ID.</param>
    /// <param name="itemCode">Drug or service code.</param>
    /// <param name="itemType">Type discriminator (e.g. <c>drug</c> or <c>service</c>).</param>
    /// <param name="doctorId">Prescribing doctor ID.</param>
    /// <param name="date">Reference date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<JsonElement> GetAllowedCountAsync(
        string patientNationalId,
        string itemCode,
        string itemType,
        string? doctorId = null,
        string? date = null,
        CancellationToken cancellationToken = default)
    {
        var query = TaminQueryParameters.Build(
            ("patient_national_id", patientNationalId),
            ("item_code", itemCode),
            ("item_type", itemType),
            ("doctor_id", doctorId),
            ("date", date));
        return _gateway.GetAsync("ws-allowed-count", query, cancellationToken);
    }

    /// <summary>
    /// Displays price, insurance share, patient share, or tariff data for a drug or service.
    /// Maps to <c>ws-price</c>.
    /// </summary>
    /// <param name="itemCode">Drug or service code.</param>
    /// <param name="itemType">Type discriminator (e.g. <c>drug</c> or <c>service</c>).</param>
    /// <param name="quantity">Quantity to price.</param>
    /// <param name="patientNationalId">Patient national ID.</param>
    /// <param name="providerId">Provider identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<JsonElement> GetPriceAsync(
        string itemCode,
        string itemType,
        int quantity,
        string? patientNationalId = null,
        string? providerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = TaminQueryParameters.Build(
            ("item_code", itemCode),
            ("item_type", itemType),
            ("quantity", quantity.ToString()),
            ("patient_national_id", patientNationalId),
            ("provider_id", providerId));
        return _gateway.GetAsync("ws-price", query, cancellationToken);
    }

}
