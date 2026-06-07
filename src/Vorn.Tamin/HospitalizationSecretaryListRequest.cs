using System.Text.Json.Serialization;

namespace Vorn.Tamin;

/// <summary>Request data for secretary hospitalization list retrieval.</summary>
public sealed class HospitalizationSecretaryListRequest
{
    [JsonPropertyName("siam_id")] public required string SiamId { get; init; }
    [JsonPropertyName("secretary_national_code")] public required string SecretaryNationalCode { get; init; }
}
