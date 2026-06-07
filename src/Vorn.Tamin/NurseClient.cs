using System.Text.Json;

namespace Vorn.Tamin;

/// <summary>Exposes nurse-facing to-do retrieval and action-recording workflows.</summary>
public sealed class NurseClient
{
    /// <summary>Returns the nurse to-do list for a clinic and patient.</summary>
    public Task<JsonElement> GetTodoListAsync(NurseTodoListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        throw TaminWorkflowNotImplementedException.For("nurse to-do list", "The provider request builder is not wired into the role workflow surface yet.");
    }

    /// <summary>Records completed nursing actions for prescription detail identifiers.</summary>
    public Task<JsonElement> RecordActionAsync(NurseActionWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        throw TaminWorkflowNotImplementedException.For("nurse action recording", "The provider request builder is not wired into the role workflow surface yet.");
    }
}
