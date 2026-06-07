using System.Text.Json.Serialization;

namespace Vorn.Tamin;

/// <summary>Request data for recording nursing actions.</summary>
public sealed class NurseActionWorkflowRequest
{
    [JsonPropertyName("siam_id")] public required string SiamId { get; init; }
    [JsonPropertyName("nurse_national_code")] public required string NurseNationalCode { get; init; }
    [JsonPropertyName("prescription_detail_ids")] public required IReadOnlyList<long> PrescriptionDetailIds { get; init; }
}
