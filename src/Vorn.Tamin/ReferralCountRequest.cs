using System.Text.Json.Serialization;

namespace Vorn.Tamin;

/// <summary>Request data for retrieving open referral counts for a patient and doctor.</summary>
public sealed class ReferralCountRequest
{
    /// <summary>Patient national code.</summary>
    [JsonPropertyName("patient_national_code")] public required string PatientNationalCode { get; init; }

    /// <summary>Doctor medical council number used by the official <c>docId</c> path parameter.</summary>
    [JsonPropertyName("doctor_id")] public required string DoctorId { get; init; }
}
