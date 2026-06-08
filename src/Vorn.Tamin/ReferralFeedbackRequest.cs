using System.Text.Json.Serialization;

namespace Vorn.Tamin;

/// <summary>Request data for locating referral feedback detail.</summary>
public sealed class ReferralFeedbackRequest
{
    /// <summary>Referral detail identifier.</summary>
    [JsonPropertyName("id")] public long Id { get; init; }

    /// <summary>Parent referral note header identifier.</summary>
    [JsonPropertyName("master_parent")] public long MasterParent { get; init; }
}
