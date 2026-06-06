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

    /// <summary>Fetches the generated Kiota service list.</summary>
    public Task<JsonElement> GetAllServicesAsync(IReadOnlyDictionary<string, string?>? query = null, CancellationToken cancellationToken = default)
        => _gateway.GetServicesAsync(query, cancellationToken);

    /// <summary>Fetches generated Kiota prescription types.</summary>
    public Task<JsonElement> GetPrescriptionTypeAsync(IReadOnlyDictionary<string, string?>? query = null, CancellationToken cancellationToken = default)
        => _gateway.GetPrescriptionTypesAsync(query, cancellationToken);

    /// <summary>Fetches generated Kiota paraclinic tariffs.</summary>
    public Task<JsonElement> GetParaclinicTarefAsync(IReadOnlyDictionary<string, string?>? query = null, CancellationToken cancellationToken = default)
        => _gateway.GetParaclinicTariffsAsync(query, cancellationToken);

    /// <summary>Fetches generated Kiota drug amount reference data.</summary>
    public Task<JsonElement> GetDrugAmountAsync(IReadOnlyDictionary<string, string?>? query = null, CancellationToken cancellationToken = default)
        => _gateway.GetDrugAmountsAsync(query, cancellationToken);

    /// <summary>Fetches generated Kiota drug administration instructions.</summary>
    public Task<JsonElement> GetDrugInstructionAsync(IReadOnlyDictionary<string, string?>? query = null, CancellationToken cancellationToken = default)
        => _gateway.GetDrugInstructionsAsync(query, cancellationToken);

    // ── Typed reference-data methods (Section 11) ────────────────────────────

    /// <summary>
    /// Retrieves the official drug reference list.
    /// Uses the generated Kiota drug amount request builder.
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
        return _gateway.GetDrugAmountsAsync(query, cancellationToken);
    }

    /// <summary>
    /// Retrieves the official service reference list.
    /// Uses the generated Kiota services request builder.
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
        return _gateway.GetServicesAsync(query, cancellationToken);
    }

}
