using System.Text.Json;

namespace Vorn.Tamin;

/// <summary>Orchestrates hospitalization workflows without owning provider transport details.</summary>
public sealed class HospitalizationClient
{
    /// <summary>Creates a hospitalization order when the provider builder is wired.</summary>
    public Task<JsonElement> CreateAsync(HospitalizationCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        throw TaminWorkflowNotImplementedException.For("hospitalization creation", "The provider request builder is not wired into the role workflow surface yet.");
    }

    /// <summary>Returns hospitalization orders when the provider builder is wired.</summary>
    public Task<JsonElement> GetSecretaryListAsync(HospitalizationSecretaryListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        throw TaminWorkflowNotImplementedException.For("hospitalization secretary list", "The provider request builder is not wired into the role workflow surface yet.");
    }
}
