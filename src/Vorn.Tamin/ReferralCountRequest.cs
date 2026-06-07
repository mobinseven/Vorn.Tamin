using System.Text.Json.Serialization;

namespace Vorn.Tamin;

/// <summary>Request data for retrieving referral counts.</summary>
public sealed class ReferralCountRequest
{
    [JsonPropertyName("patient_national_code")] public required string PatientNationalCode { get; init; }
    [JsonPropertyName("doctor_id")] public required string DoctorId { get; init; }
}
