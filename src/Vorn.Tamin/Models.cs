using System.Text.Json.Serialization;

namespace Vorn.Tamin;

// ── Authentication DTOs (Section 6) ─────────────────────────────────────────

/// <summary>Request payload for obtaining a new access token.</summary>
public sealed class GetTokenRequest
{
    /// <summary>Provider username.</summary>
    [JsonPropertyName("client_id")]
    public required string Username { get; init; }

    /// <summary>Provider password.</summary>
    [JsonPropertyName("secret")]
    public required string Password { get; init; }

    /// <summary>Client identifier issued during API onboarding.</summary>
    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; init; }

    /// <summary>One-time password, when two-step verification is required.</summary>
    [JsonPropertyName("otp")]
    public string? Otp { get; init; }

    /// <summary>Provider identifier, when required by the server.</summary>
    [JsonPropertyName("provider_identifier")]
    public string? ProviderIdentifier { get; init; }
}

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

// ── Identity & Eligibility DTOs (Section 7) ──────────────────────────────────

/// <summary>Request payload for verifying a patient's identity.</summary>
public sealed class VerifyIdentityRequest
{
    /// <summary>Patient national ID.</summary>
    [JsonPropertyName("national_id")]
    public required string NationalId { get; init; }

    /// <summary>Patient birth date (optional).</summary>
    [JsonPropertyName("birth_date")]
    public string? BirthDate { get; init; }

    /// <summary>Patient mobile number (optional).</summary>
    [JsonPropertyName("mobile_number")]
    public string? MobileNumber { get; init; }

    /// <summary>Identifier for foreign patients, when applicable.</summary>
    [JsonPropertyName("foreigner_identifier")]
    public string? ForeignerIdentifier { get; init; }
}

/// <summary>Result of a patient identity verification.</summary>
public sealed class IdentityResult
{
    /// <summary>Patient first name.</summary>
    [JsonPropertyName("patient_name")]
    public string? PatientName { get; init; }

    /// <summary>Patient family name.</summary>
    [JsonPropertyName("patient_family")]
    public string? PatientFamily { get; init; }

    /// <summary>Patient birth date.</summary>
    [JsonPropertyName("birth_date")]
    public string? BirthDate { get; init; }

    /// <summary>Patient gender.</summary>
    [JsonPropertyName("gender")]
    public string? Gender { get; init; }

    /// <summary>Identity verification status.</summary>
    [JsonPropertyName("identity_status")]
    public string? IdentityStatus { get; init; }

    /// <summary>Unique patient identifier assigned by the system.</summary>
    [JsonPropertyName("patient_identifier")]
    public string? PatientIdentifier { get; init; }
}

/// <summary>Request payload for checking a patient's treatment entitlement.</summary>
public sealed class CheckEntitlementRequest
{
    /// <summary>Patient national ID.</summary>
    [JsonPropertyName("national_id")]
    public required string NationalId { get; init; }

    /// <summary>Healthcare provider identifier.</summary>
    [JsonPropertyName("provider_id")]
    public required string ProviderId { get; init; }

    /// <summary>Date of the visit (ISO 8601 or local date string).</summary>
    [JsonPropertyName("visit_date")]
    public required string VisitDate { get; init; }

    /// <summary>Type of service being requested.</summary>
    [JsonPropertyName("service_type")]
    public required string ServiceType { get; init; }
}

/// <summary>Result of a treatment entitlement check.</summary>
public sealed class EntitlementResult
{
    /// <summary>Whether the patient is eligible for treatment.</summary>
    [JsonPropertyName("eligible_flag")]
    public bool EligibleFlag { get; init; }

    /// <summary>Coverage status description.</summary>
    [JsonPropertyName("coverage_status")]
    public string? CoverageStatus { get; init; }

    /// <summary>Insurance type.</summary>
    [JsonPropertyName("insurance_type")]
    public string? InsuranceType { get; init; }

    /// <summary>Tracking code for this eligibility inquiry — must be stored locally.</summary>
    [JsonPropertyName("tracking_code")]
    public string? TrackingCode { get; init; }

    /// <summary>Whether the patient has a special condition flag.</summary>
    [JsonPropertyName("special_patient_flag")]
    public bool SpecialPatientFlag { get; init; }

    /// <summary>Additional message from the API.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }
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

/// <summary>Request payload for registering a visit-only prescription.</summary>
public sealed class RegisterVisitPrescriptionRequest
{
    [JsonPropertyName("prescription_type")] public int PrescriptionType { get; init; } = (int)Tamin.PrescriptionType.Visit;
    [JsonPropertyName("doctor_id")] public required string DoctorId { get; init; }
    [JsonPropertyName("patient_national_id")] public required string PatientNationalId { get; init; }
    [JsonPropertyName("visit_date")] public required string VisitDate { get; init; }
    [JsonPropertyName("clinic_id")] public required string ClinicId { get; init; }
    [JsonPropertyName("mobile_number")] public string? MobileNumber { get; init; }
    [JsonPropertyName("diagnosis_code")] public string? DiagnosisCode { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
}

/// <summary>Request payload for registering a drug prescription.</summary>
public sealed class RegisterDrugPrescriptionRequest
{
    [JsonPropertyName("prescription_type")] public int PrescriptionType { get; init; } = (int)Tamin.PrescriptionType.Drug;
    [JsonPropertyName("doctor_id")] public required string DoctorId { get; init; }
    [JsonPropertyName("patient_national_id")] public required string PatientNationalId { get; init; }
    [JsonPropertyName("visit_date")] public required string VisitDate { get; init; }
    [JsonPropertyName("mobile_number")] public string? MobileNumber { get; init; }
    [JsonPropertyName("diagnosis_code")] public string? DiagnosisCode { get; init; }
    [JsonPropertyName("drug_items")] public required IReadOnlyList<DrugItem> DrugItems { get; init; }
}

/// <summary>Request payload for registering a paraclinic prescription.</summary>
public sealed class RegisterParaclinicPrescriptionRequest
{
    [JsonPropertyName("prescription_type")] public int PrescriptionType { get; init; } = (int)Tamin.PrescriptionType.Paraclinic;
    [JsonPropertyName("doctor_id")] public required string DoctorId { get; init; }
    [JsonPropertyName("patient_national_id")] public required string PatientNationalId { get; init; }
    [JsonPropertyName("visit_date")] public required string VisitDate { get; init; }
    [JsonPropertyName("service_items")] public required IReadOnlyList<ServiceItem> ServiceItems { get; init; }
}

/// <summary>Request payload for registering a medical service prescription.</summary>
public sealed class RegisterMedicalServicePrescriptionRequest
{
    [JsonPropertyName("prescription_type")] public int PrescriptionType { get; init; } = (int)Tamin.PrescriptionType.Service;
    [JsonPropertyName("doctor_id")] public required string DoctorId { get; init; }
    [JsonPropertyName("patient_national_id")] public required string PatientNationalId { get; init; }
    [JsonPropertyName("visit_date")] public required string VisitDate { get; init; }
    [JsonPropertyName("service_items")] public required IReadOnlyList<ServiceItem> ServiceItems { get; init; }
}

/// <summary>Request payload for registering a referral prescription.</summary>
public sealed class RegisterReferralPrescriptionRequest
{
    [JsonPropertyName("prescription_type")] public int PrescriptionType { get; init; } = (int)Tamin.PrescriptionType.Referral;
    [JsonPropertyName("doctor_id")] public required string DoctorId { get; init; }
    [JsonPropertyName("patient_national_id")] public required string PatientNationalId { get; init; }
    [JsonPropertyName("target_specialty")] public required string TargetSpecialty { get; init; }
    [JsonPropertyName("target_provider_type")] public required string TargetProviderType { get; init; }
    [JsonPropertyName("reason")] public required string Reason { get; init; }
    [JsonPropertyName("visit_date")] public required string VisitDate { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
}

/// <summary>Request payload for registering a physiotherapy prescription.</summary>
public sealed class RegisterPhysiotherapyPrescriptionRequest
{
    [JsonPropertyName("prescription_type")] public int PrescriptionType { get; init; } = (int)Tamin.PrescriptionType.Physiotherapy;
    [JsonPropertyName("doctor_id")] public required string DoctorId { get; init; }
    [JsonPropertyName("patient_national_id")] public required string PatientNationalId { get; init; }
    [JsonPropertyName("physiotherapy_items")] public required IReadOnlyList<PhysiotherapyItem> PhysiotherapyItems { get; init; }
    [JsonPropertyName("session_count")] public int SessionCount { get; init; }
    [JsonPropertyName("effective_date")] public string? EffectiveDate { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
}

/// <summary>Result returned after successfully registering any prescription type.</summary>
public sealed class PrescriptionResult
{
    [JsonPropertyName("prescription_id")] public string? PrescriptionId { get; init; }
    [JsonPropertyName("referral_prescription_id")] public string? ReferralPrescriptionId { get; init; }
    [JsonPropertyName("tracking_code")] public string? TrackingCode { get; init; }
    [JsonPropertyName("status")] public string? Status { get; init; }
    [JsonPropertyName("accepted_items")] public IReadOnlyList<object>? AcceptedItems { get; init; }
    [JsonPropertyName("rejected_items")] public IReadOnlyList<object>? RejectedItems { get; init; }
    [JsonPropertyName("warnings")] public IReadOnlyList<WarningItem>? Warnings { get; init; }
    [JsonPropertyName("new_version")] public string? NewVersion { get; init; }
    [JsonPropertyName("deleted_at")] public string? DeletedAt { get; init; }
}

// ── Prescription Query DTOs (Section 9) ──────────────────────────────────────

/// <summary>Filter parameters for querying a list of prescriptions.</summary>
public sealed class PrescriptionListFilter
{
    /// <summary>Filter by doctor ID.</summary>
    public string? DoctorId { get; init; }
    /// <summary>Filter by patient national ID.</summary>
    public string? PatientNationalId { get; init; }
    /// <summary>Start of date range.</summary>
    public string? FromDate { get; init; }
    /// <summary>End of date range.</summary>
    public string? ToDate { get; init; }
    /// <summary>Filter by prescription type.</summary>
    public PrescriptionType? PrescriptionType { get; init; }
    /// <summary>Filter by prescription status.</summary>
    public string? Status { get; init; }
}

// ── Prescription Mutation DTOs (Section 10) ───────────────────────────────────

/// <summary>Request payload for editing an existing prescription.</summary>
public sealed class EditPrescriptionRequest
{
    [JsonPropertyName("prescription_id")] public required string PrescriptionId { get; init; }
    [JsonPropertyName("tracking_code")] public required string TrackingCode { get; init; }
    [JsonPropertyName("edited_items")] public required IReadOnlyList<object> EditedItems { get; init; }
    [JsonPropertyName("edit_reason")] public required string EditReason { get; init; }
}

/// <summary>Request payload for deleting an existing prescription.</summary>
public sealed class DeletePrescriptionRequest
{
    [JsonPropertyName("prescription_id")] public required string PrescriptionId { get; init; }
    [JsonPropertyName("tracking_code")] public required string TrackingCode { get; init; }
    [JsonPropertyName("delete_reason")] public required string DeleteReason { get; init; }
}

// ── Reference Data DTOs (Section 11) ─────────────────────────────────────────

/// <summary>A drug entry from the official drug reference list.</summary>
public sealed class DrugReference
{
    [JsonPropertyName("drug_code")] public string? DrugCode { get; init; }
    [JsonPropertyName("drug_name")] public string? DrugName { get; init; }
    [JsonPropertyName("generic_name")] public string? GenericName { get; init; }
    [JsonPropertyName("form")] public string? Form { get; init; }
    [JsonPropertyName("strength")] public string? Strength { get; init; }
    [JsonPropertyName("unit")] public string? Unit { get; init; }
    [JsonPropertyName("active_flag")] public bool ActiveFlag { get; init; }
}

/// <summary>A service entry from the official service reference list.</summary>
public sealed class ServiceReference
{
    [JsonPropertyName("service_code")] public string? ServiceCode { get; init; }
    [JsonPropertyName("service_name")] public string? ServiceName { get; init; }
    [JsonPropertyName("service_group")] public string? ServiceGroup { get; init; }
    [JsonPropertyName("tariff_group")] public string? TariffGroup { get; init; }
    [JsonPropertyName("active_flag")] public bool ActiveFlag { get; init; }
}

/// <summary>Result of an allowed-count query for a drug or service.</summary>
public sealed class AllowedCountResult
{
    [JsonPropertyName("allowed_count")] public int AllowedCount { get; init; }
    [JsonPropertyName("used_count")] public int UsedCount { get; init; }
    [JsonPropertyName("remaining_count")] public int RemainingCount { get; init; }
    [JsonPropertyName("limitation_message")] public string? LimitationMessage { get; init; }
}

/// <summary>Result of a price query for a drug or service.</summary>
public sealed class PriceResult
{
    [JsonPropertyName("total_price")] public decimal TotalPrice { get; init; }
    [JsonPropertyName("insurance_share")] public decimal InsuranceShare { get; init; }
    [JsonPropertyName("patient_share")] public decimal PatientShare { get; init; }
    [JsonPropertyName("government_share")] public decimal GovernmentShare { get; init; }
    [JsonPropertyName("tariff_code")] public string? TariffCode { get; init; }
    [JsonPropertyName("message")] public string? Message { get; init; }
}

// ── Warning DTOs (Section 14) ─────────────────────────────────────────────────

/// <summary>A warning item returned by the API.</summary>
public sealed class WarningItem
{
    [JsonPropertyName("warning_code")] public string? WarningCode { get; init; }
    [JsonPropertyName("warning_type")] public string? WarningType { get; init; }
    [JsonPropertyName("severity")] public string? Severity { get; init; }
    [JsonPropertyName("message")] public string? Message { get; init; }
    [JsonPropertyName("can_continue_flag")] public bool CanContinueFlag { get; init; }
    [JsonPropertyName("requires_confirmation_flag")] public bool RequiresConfirmationFlag { get; init; }
}

/// <summary>Request payload for checking prescription warnings.</summary>
public sealed class CheckWarningRequest
{
    [JsonPropertyName("patient_national_id")] public required string PatientNationalId { get; init; }
    [JsonPropertyName("doctor_id")] public required string DoctorId { get; init; }
    [JsonPropertyName("prescription_items")] public required IReadOnlyList<object> PrescriptionItems { get; init; }
}

// ── Pharmacy Dispensing DTOs (Section 12) ────────────────────────────────────

/// <summary>Request to register a paper prescription in the pharmacy system.</summary>
public sealed class RegisterPaperPrescriptionRequest
{
    [JsonPropertyName("patient_national_id")] public required string PatientNationalId { get; init; }
    [JsonPropertyName("paper_prescription_number")] public required string PaperPrescriptionNumber { get; init; }
    [JsonPropertyName("doctor_id")] public string? DoctorId { get; init; }
    [JsonPropertyName("prescription_date")] public string? PrescriptionDate { get; init; }
    [JsonPropertyName("items")] public IReadOnlyList<object>? Items { get; init; }
}

/// <summary>Request to dispense items from a prescription.</summary>
public sealed class DispensePrescriptionRequest
{
    [JsonPropertyName("prescription_id")] public required string PrescriptionId { get; init; }
    [JsonPropertyName("tracking_code")] public required string TrackingCode { get; init; }
    [JsonPropertyName("dispensed_items")] public required IReadOnlyList<object> DispensedItems { get; init; }
    [JsonPropertyName("pharmacist_id")] public string? PharmacistId { get; init; }
}

/// <summary>Request to register a drug authenticity/tracking code.</summary>
public sealed class DrugAuthenticityRequest
{
    [JsonPropertyName("prescription_id")] public required string PrescriptionId { get; init; }
    [JsonPropertyName("drug_code")] public required string DrugCode { get; init; }
    [JsonPropertyName("authenticity_code")] public required string AuthenticityCode { get; init; }
}

/// <summary>Request to refer a prescription back to the doctor.</summary>
public sealed class ReferPrescriptionRequest
{
    [JsonPropertyName("prescription_id")] public required string PrescriptionId { get; init; }
    [JsonPropertyName("tracking_code")] public required string TrackingCode { get; init; }
    [JsonPropertyName("reason")] public required string Reason { get; init; }
}

// ── Paraclinic Dispensing DTOs (Section 13) ───────────────────────────────────

/// <summary>Request to register delivery of a paraclinic service.</summary>
public sealed class ProvideServiceRequest
{
    [JsonPropertyName("prescription_id")] public required string PrescriptionId { get; init; }
    [JsonPropertyName("tracking_code")] public required string TrackingCode { get; init; }
    [JsonPropertyName("delivered_items")] public required IReadOnlyList<object> DeliveredItems { get; init; }
    [JsonPropertyName("provider_id")] public string? ProviderId { get; init; }
}
