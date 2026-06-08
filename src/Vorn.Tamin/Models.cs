using System.Text.Json.Serialization;

namespace Vorn.Tamin;

/// <summary>Response payload for a successful token request.</summary>
public sealed class TokenResult
{
    /// <summary>****** token.</summary>
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }

    /// <summary>Raw token data string returned by some API versions.</summary>
    [JsonPropertyName("data")]
    public string? Data { get; init; }

    /// <summary>Refresh token, when supported.</summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    /// <summary>Token lifetime in seconds.</summary>
    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; init; }

    /// <summary>Token type (e.g. <c>Bearer</c>).</summary>
    [JsonPropertyName("token_type")]
    public string? TokenType { get; init; }

    /// <summary>Roles assigned to the authenticated user.</summary>
    [JsonPropertyName("user_roles")]
    public IReadOnlyList<string>? UserRoles { get; init; }

    /// <summary>Provider information included in the token response.</summary>
    [JsonPropertyName("provider_info")]
    public object? ProviderInfo { get; init; }
}

/// <summary>Response payload for a token validation check.</summary>
public sealed class ValidateTokenResult
{
    /// <summary>Whether the token is still valid.</summary>
    [JsonPropertyName("valid")]
    public bool Valid { get; init; }

    /// <summary>Timestamp at which the token expires.</summary>
    [JsonPropertyName("expires_at")]
    public string? ExpiresAt { get; init; }

    /// <summary>Information about the authenticated user.</summary>
    [JsonPropertyName("user_info")]
    public object? UserInfo { get; init; }
}


// ── E-Prescription Writing DTOs (Section 8) ──────────────────────────────────

/// <summary>Represents a single drug item in a drug prescription.</summary>
public sealed class DrugItem
{
    /// <summary>Official drug code.</summary>
    [JsonPropertyName("drug_code")]
    public required string DrugCode { get; init; }

    /// <summary>Prescribed quantity.</summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; init; }

    /// <summary>Dosage instruction text.</summary>
    [JsonPropertyName("dosage_instruction")]
    public string? DosageInstruction { get; init; }

    /// <summary>Administration frequency.</summary>
    [JsonPropertyName("frequency")]
    public string? Frequency { get; init; }

    /// <summary>Duration of treatment.</summary>
    [JsonPropertyName("duration")]
    public string? Duration { get; init; }

    /// <summary>Route of administration.</summary>
    [JsonPropertyName("route")]
    public string? Route { get; init; }

    /// <summary>Free-text usage note.</summary>
    [JsonPropertyName("usage_note")]
    public string? UsageNote { get; init; }

    /// <summary>Repeat count, when supported.</summary>
    [JsonPropertyName("repeat_count")]
    public int? RepeatCount { get; init; }
}

/// <summary>Represents a single service item in a paraclinic or medical service prescription.</summary>
public sealed class ServiceItem
{
    /// <summary>Official service code.</summary>
    [JsonPropertyName("service_code")]
    public required string ServiceCode { get; init; }

    /// <summary>Service group classification.</summary>
    [JsonPropertyName("service_group")]
    public string? ServiceGroup { get; init; }

    /// <summary>Quantity ordered.</summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; init; }

    /// <summary>Effective date for the service.</summary>
    [JsonPropertyName("effective_date")]
    public string? EffectiveDate { get; init; }

    /// <summary>Service priority.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; init; }

    /// <summary>Free-text description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

/// <summary>Represents a single physiotherapy item.</summary>
public sealed class PhysiotherapyItem
{
    /// <summary>Official service or exercise code.</summary>
    [JsonPropertyName("service_code")]
    public required string ServiceCode { get; init; }

    /// <summary>Free-text description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}


/// <summary>Provider identity fields shared by SendEpresc registration requests.</summary>
public abstract class ProviderPrescriptionIdentity
{
    /// <summary>شماره نظام پزشکی بدون علامت؛ برای ماما حرف «م» در انتهای شماره نظام با <c>*</c> ارسال می‌شود.</summary>
    [JsonPropertyName("doctor_id")] public required string DoctorId { get; init; }

    /// <summary>کد ملی پزشک؛ برای پزشکان اتباع طبق مستندات با FDA/FIDA ارسال شود.</summary>
    [JsonPropertyName("doctor_national_code")] public string? DoctorNationalCode { get; init; }

    /// <summary>Mobile number for the prescribing doctor, serialized to the official <c>docMobileNo</c> field.</summary>
    [JsonPropertyName("doctor_mobile_number")] public string? DoctorMobileNumber { get; init; }

    /// <summary>Patient national code used as the official SendEpresc <c>patient</c> value.</summary>
    [JsonPropertyName("patient_national_id")] public required string PatientNationalId { get; init; }
}

/// <summary>Request payload for registering a visit-only prescription.</summary>
public sealed class RegisterVisitPrescriptionRequest : ProviderPrescriptionIdentity
{
    [JsonPropertyName("prescription_type")] public int PrescriptionType { get; init; } = (int)Tamin.PrescriptionType.Visit;
    [JsonPropertyName("visit_date")] public required string VisitDate { get; init; }
    [JsonPropertyName("clinic_id")] public required string ClinicId { get; init; }
    [JsonPropertyName("mobile_number")] public string? MobileNumber { get; init; }
    [JsonPropertyName("diagnosis_code")] public string? DiagnosisCode { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
}

/// <summary>Request payload for registering a drug prescription.</summary>
public sealed class RegisterDrugPrescriptionRequest : ProviderPrescriptionIdentity
{
    [JsonPropertyName("prescription_type")] public int PrescriptionType { get; init; } = (int)Tamin.PrescriptionType.Drug;
    [JsonPropertyName("visit_date")] public required string VisitDate { get; init; }
    [JsonPropertyName("mobile_number")] public string? MobileNumber { get; init; }
    [JsonPropertyName("diagnosis_code")] public string? DiagnosisCode { get; init; }
    [JsonPropertyName("drug_items")] public required IReadOnlyList<DrugItem> DrugItems { get; init; }
}

/// <summary>Request payload for registering a paraclinic prescription.</summary>
public sealed class RegisterParaclinicPrescriptionRequest : ProviderPrescriptionIdentity
{
    [JsonPropertyName("prescription_type")] public int PrescriptionType { get; init; } = (int)Tamin.PrescriptionType.Paraclinic;
    [JsonPropertyName("visit_date")] public required string VisitDate { get; init; }
    [JsonPropertyName("service_items")] public required IReadOnlyList<ServiceItem> ServiceItems { get; init; }
}

/// <summary>Request payload for registering a medical service prescription.</summary>
public sealed class RegisterMedicalServicePrescriptionRequest : ProviderPrescriptionIdentity
{
    [JsonPropertyName("prescription_type")] public int PrescriptionType { get; init; } = (int)Tamin.PrescriptionType.Service;
    [JsonPropertyName("visit_date")] public required string VisitDate { get; init; }
    [JsonPropertyName("service_items")] public required IReadOnlyList<ServiceItem> ServiceItems { get; init; }
}

/// <summary>Request payload for registering a referral prescription.</summary>
public sealed class RegisterReferralPrescriptionRequest : ProviderPrescriptionIdentity
{
    [JsonPropertyName("prescription_type")] public int PrescriptionType { get; init; } = (int)Tamin.PrescriptionType.Referral;
    [JsonPropertyName("target_specialty")] public required string TargetSpecialty { get; init; }
    [JsonPropertyName("target_provider_type")] public required string TargetProviderType { get; init; }
    [JsonPropertyName("reason")] public required string Reason { get; init; }
    [JsonPropertyName("visit_date")] public required string VisitDate { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
}

/// <summary>Request payload for registering a physiotherapy prescription.</summary>
public sealed class RegisterPhysiotherapyPrescriptionRequest : ProviderPrescriptionIdentity
{
    [JsonPropertyName("prescription_type")] public int PrescriptionType { get; init; } = (int)Tamin.PrescriptionType.Physiotherapy;
    [JsonPropertyName("physiotherapy_items")] public required IReadOnlyList<PhysiotherapyItem> PhysiotherapyItems { get; init; }
    [JsonPropertyName("session_count")] public int SessionCount { get; init; }
    [JsonPropertyName("effective_date")] public string? EffectiveDate { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
}



// ── Prescription Mutation DTOs (Section 10) ───────────────────────────────────

/// <summary>Request payload for editing an existing prescription through the generated Kiota edit builder.</summary>
public sealed class EditPrescriptionRequest
{
    [JsonPropertyName("header_id")] public required int HeaderId { get; init; }
    [JsonPropertyName("doctor_national_code")] public string? DoctorNationalCode { get; init; }
    [JsonPropertyName("doctor_id")] public required string DoctorId { get; init; }
    [JsonPropertyName("edited_items")] public required IReadOnlyList<object> EditedItems { get; init; }
}

/// <summary>Request payload for deleting an existing prescription through the generated Kiota remove builder.</summary>
public sealed class DeletePrescriptionRequest
{
    [JsonPropertyName("header_id")] public required int HeaderId { get; init; }
    [JsonPropertyName("doctor_national_code")] public string? DoctorNationalCode { get; init; }
    [JsonPropertyName("doctor_id")] public required string DoctorId { get; init; }
}


/// <summary>Request payload for checking prescription warnings.</summary>
public sealed class CheckWarningRequest
{
    [JsonPropertyName("doctor_id")] public required string DoctorId { get; init; }
    [JsonPropertyName("patient_national_id")] public required string PatientNationalId { get; init; }
    [JsonPropertyName("prescription_items")] public required IReadOnlyList<object> PrescriptionItems { get; init; }
}
