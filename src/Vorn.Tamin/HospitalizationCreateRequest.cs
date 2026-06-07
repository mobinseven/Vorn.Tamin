using System.Text.Json.Serialization;

namespace Vorn.Tamin;

/// <summary>Request data for hospitalization creation.</summary>
public sealed class HospitalizationCreateRequest
{
    [JsonPropertyName("referral_id")] public required string ReferralId { get; init; }
    [JsonPropertyName("siam_id")] public required string SiamId { get; init; }
}
