using System.Text.Json.Serialization;

namespace Vorn.Tamin;

/// <summary>Request data for a nurse to-do list query.</summary>
public sealed class NurseTodoListRequest
{
    [JsonPropertyName("siam_id")] public required string SiamId { get; init; }
    [JsonPropertyName("patient_national_code")] public required string PatientNationalCode { get; init; }
}
