using System.Text.Json.Serialization;

namespace Vorn.Tamin;

/// <summary>Request data for creating the official hospitalization SendEpresc payload.</summary>
public sealed class HospitalizationCreateRequest : ProviderPrescriptionIdentity
{
    /// <summary>Official prescription type for hospitalization orders.</summary>
    [JsonPropertyName("prescription_type")] public int PrescriptionType { get; init; } = (int)Tamin.PrescriptionType.HospitalizationOrder;

    /// <summary>Prescription date as eight Jalali digits.</summary>
    [JsonPropertyName("prescription_date")] public required string PrescriptionDate { get; init; }

    /// <summary>شناسه سیام مرکز درمانی/درمانگاه/بیمارستان.</summary>
    [JsonPropertyName("siam_id")] public required string SiamId { get; init; }

    /// <summary>Optional top-level hospitalization note.</summary>
    [JsonPropertyName("message")] public string? Message { get; init; }

    /// <summary>Documented hospitalization referral detail rows.</summary>
    [JsonPropertyName("note_details_referral_list")] public required IReadOnlyList<HospitalizationReferralDetail> ReferralDetails { get; init; }
}

/// <summary>One hospitalization referral detail row for <c>noteDetailsReferralList</c>.</summary>
public sealed class HospitalizationReferralDetail
{
    /// <summary>Patient national code repeated in the hospitalization detail row.</summary>
    [JsonPropertyName("patient_national_code")] public required string PatientNationalCode { get; init; }

    /// <summary>Jalali referral date for hospitalization.</summary>
    [JsonPropertyName("referral_hijri_date")] public required string ReferralHijriDate { get; init; }

    /// <summary>شناسه سیام مرکز درمانی/درمانگاه/بیمارستان.</summary>
    [JsonPropertyName("siam_id")] public required string SiamId { get; init; }

    /// <summary>Required ICD10 diagnoses for the hospitalization order.</summary>
    [JsonPropertyName("icd10s")] public required IReadOnlyList<HospitalizationIcd10Item> Icd10Items { get; init; }

    /// <summary>Optional detail message.</summary>
    [JsonPropertyName("message")] public string? Message { get; init; }
}

/// <summary>One ICD10 diagnosis item for hospitalization orders.</summary>
public sealed class HospitalizationIcd10Item
{
    /// <summary>Official ICD10 code.</summary>
    [JsonPropertyName("icd_code")] public required string IcdCode { get; init; }

    /// <summary>Provider ICD identifier when supplied by reference data.</summary>
    [JsonPropertyName("icd_id")] public string? IcdId { get; init; }

    /// <summary>Human-readable ICD name when supplied by reference data.</summary>
    [JsonPropertyName("icd_name")] public string? IcdName { get; init; }
}
