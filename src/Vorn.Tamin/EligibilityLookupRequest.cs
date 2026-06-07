using System.Text.Json.Serialization;

namespace Vorn.Tamin;

/// <summary>Request data for private-practice eligibility lookup.</summary>
public sealed class EligibilityLookupRequest
{
    [JsonPropertyName("request_by")] public string RequestBy { get; init; } = "doctor";
    [JsonPropertyName("siam_id")] public required string SiamId { get; init; }
    [JsonPropertyName("doctor_id")] public required string DoctorId { get; init; }
    [JsonPropertyName("doctor_national_code")] public required string DoctorNationalCode { get; init; }
    [JsonPropertyName("patient_national_code")] public required string PatientNationalCode { get; init; }
}
