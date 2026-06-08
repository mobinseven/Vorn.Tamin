using System.Text.Json.Serialization;

namespace Vorn.Tamin;

/// <summary>Official request data for saving nurse actions on prescription details.</summary>
public sealed class NurseActionWorkflowRequest
{
    /// <summary>شناسه سیام مرکز درمانی/درمانگاه/بیمارستان.</summary>
    [JsonPropertyName("siam_id")] public required string SiamId { get; init; }

    /// <summary>Nurse national code.</summary>
    [JsonPropertyName("nurse_national_code")] public required string NurseNationalCode { get; init; }

    /// <summary>Official <c>noteDetailsEprscIds</c> values to mark as acted on.</summary>
    [JsonPropertyName("note_details_eprsc_ids")] public required IReadOnlyList<long> NoteDetailsEprscIds { get; init; }
}
