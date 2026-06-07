using System.Text.Json;
using Vorn.Tamin.Kiota;

namespace Vorn.Tamin;

/// <summary>Retrieves provider reference data without mutating provider state.</summary>
public sealed class ReferenceDataClient
{
    private readonly ITaminKiotaGateway _gateway;

    internal ReferenceDataClient(ITaminKiotaGateway gateway)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
    }

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

    /// <summary>Retrieves the official drug reference list.</summary>
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

    /// <summary>Retrieves the official service reference list.</summary>
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
