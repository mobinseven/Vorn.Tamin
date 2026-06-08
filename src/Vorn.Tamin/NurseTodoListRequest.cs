using System.Text.Json.Serialization;

namespace Vorn.Tamin;

/// <summary>Official request data for a nurse cartable to-do list query.</summary>
public sealed class NurseTodoListRequest
{
    /// <summary>شناسه سیام مرکز درمانی/درمانگاه/بیمارستان.</summary>
    [JsonPropertyName("siam_id")] public required string SiamId { get; init; }

    /// <summary>Nurse national code used by the official <c>nationalCode</c> path parameter.</summary>
    [JsonPropertyName("nurse_national_code")] public string? NurseNationalCode { get; init; }

    /// <summary>Backward-compatible alias; use <see cref="NurseNationalCode"/> for the official request name.</summary>
    [JsonPropertyName("patient_national_code")] public string? PatientNationalCode { get; init; }
}
