using System.Text.Json.Serialization;

namespace Vorn.Tamin;

/// <summary>Request data for recording referral feedback.</summary>
public sealed class ReferralFeedbackRequest
{
    [JsonPropertyName("referral_id")] public required string ReferralId { get; init; }
    [JsonPropertyName("message")] public required string Message { get; init; }
}
