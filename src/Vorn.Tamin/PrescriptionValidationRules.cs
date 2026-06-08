namespace Vorn.Tamin;

/// <summary>Owns pre-send prescription rule checks for currently supported prescription workflows.</summary>
public sealed class PrescriptionValidationRules
{
    private readonly TaminProviderSerializer _serializer;

    /// <inheritdoc />
    public PrescriptionValidationRules(TaminProviderSerializer? serializer = null)
    {
        _serializer = serializer ?? new TaminProviderSerializer();
    }

    public IReadOnlyList<ValidationFailure> ValidateDoctorEnrollmentFields(string docId, string docNationalCode, string docMobileNo)
    {
        var failures = new List<ValidationFailure>();
        AddRequired(failures, docId, "docId");
        AddRequired(failures, docNationalCode, "docNationalCode");
        if (!string.IsNullOrWhiteSpace(docNationalCode) && docNationalCode.Trim().Length != 10)
            failures.Add(new ValidationFailure("docNationalCode", "national-code-shape", "Doctor national code must be exactly 10 characters."));
        AddRequired(failures, docMobileNo, "docMobileNo");
        AddOptionalMobile(failures, docMobileNo, "docMobileNo");
        return failures;
    }

    public IReadOnlyList<ValidationFailure> Validate(RegisterVisitPrescriptionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = ValidatePrescriptionHeader(request.PrescriptionType, PrescriptionType.Visit, request, request.VisitDate);
        AddRequired(failures, request.ClinicId, "clinic_id");
        AddOptionalMobile(failures, request.MobileNumber, "mobile_number");
        return failures;
    }

    public IReadOnlyList<ValidationFailure> Validate(RegisterDrugPrescriptionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = ValidatePrescriptionHeader(request.PrescriptionType, PrescriptionType.Drug, request, request.VisitDate);
        AddOptionalMobile(failures, request.MobileNumber, "mobile_number");
        AddRequiredItems(failures, request.DrugItems, "drug_items");
        if (request.DrugItems is not null)
        {
            for (var index = 0; index < request.DrugItems.Count; index++)
            {
                var item = request.DrugItems[index];
                AddRequired(failures, item.DrugCode, $"drug_items[{index}].drug_code");
                AddPositive(failures, item.Quantity, $"drug_items[{index}].quantity");
                if (item.RepeatCount is < 0)
                    failures.Add(new ValidationFailure($"drug_items[{index}].repeat_count", "non-negative", "Repeat count cannot be negative."));
            }
        }

        return failures;
    }

    public IReadOnlyList<ValidationFailure> Validate(RegisterParaclinicPrescriptionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = ValidatePrescriptionHeader(request.PrescriptionType, PrescriptionType.Paraclinic, request, request.VisitDate);
        AddServiceItems(failures, request.ServiceItems, "service_items", requireGroup: true, requireEffectiveDate: false);
        return failures;
    }

    public IReadOnlyList<ValidationFailure> Validate(RegisterMedicalServicePrescriptionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = ValidatePrescriptionHeader(request.PrescriptionType, PrescriptionType.Service, request, request.VisitDate);
        AddServiceItems(failures, request.ServiceItems, "service_items", requireGroup: false, requireEffectiveDate: false);
        return failures;
    }

    public IReadOnlyList<ValidationFailure> Validate(RegisterReferralPrescriptionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = ValidatePrescriptionHeader(request.PrescriptionType, PrescriptionType.Referral, request, request.VisitDate);
        AddRequired(failures, request.TargetSpecialty, "target_specialty");
        AddRequired(failures, request.TargetProviderType, "target_provider_type");
        AddRequired(failures, request.Reason, "reason");
        return failures;
    }

    public IReadOnlyList<ValidationFailure> Validate(RegisterPhysiotherapyPrescriptionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = ValidateProviderIdentity(request);
        AddPrescriptionType(failures, request.PrescriptionType, PrescriptionType.Physiotherapy);
        AddJalaliDate(failures, request.EffectiveDate, "effective_date");
        AddPositive(failures, request.SessionCount, "session_count");
        AddRequiredItems(failures, request.PhysiotherapyItems, "physiotherapy_items");
        if (request.PhysiotherapyItems is not null)
        {
            for (var index = 0; index < request.PhysiotherapyItems.Count; index++)
                AddRequired(failures, request.PhysiotherapyItems[index].ServiceCode, $"physiotherapy_items[{index}].service_code");
        }

        return failures;
    }

    public IReadOnlyList<ValidationFailure> ValidateRegisteredPrescriptionIdentity(int headerId, string doctorNationalCode, string doctorId)
        => ValidatePrescriptionIdentity(headerId, doctorNationalCode, doctorId);

    public IReadOnlyList<ValidationFailure> Validate(EditPrescriptionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = ValidatePrescriptionIdentity(request.HeaderId, request.DoctorNationalCode, request.DoctorId);
        AddRequiredItems(failures, request.EditedItems, "edited_items");
        return failures;
    }

    public IReadOnlyList<ValidationFailure> Validate(DeletePrescriptionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ValidatePrescriptionIdentity(request.HeaderId, request.DoctorNationalCode, request.DoctorId);
    }

    public IReadOnlyList<ValidationFailure> Validate(CheckWarningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = new List<ValidationFailure>();
        AddRequired(failures, request.DoctorId, "doctor_id");
        AddNationalCode(failures, request.PatientNationalId, "patient_national_id");
        AddRequiredItems(failures, request.PrescriptionItems, "prescription_items");
        return failures;
    }


    public IReadOnlyList<ValidationFailure> Validate(HospitalizationCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = ValidatePrescriptionHeader(request.PrescriptionType, PrescriptionType.HospitalizationOrder, request, request.PrescriptionDate);
        AddRequired(failures, request.SiamId, "siam_id");
        AddRequiredItems(failures, request.ReferralDetails, "note_details_referral_list");
        if (request.ReferralDetails is not null)
        {
            for (var index = 0; index < request.ReferralDetails.Count; index++)
            {
                var detail = request.ReferralDetails[index];
                AddRequired(failures, detail.PatientNationalCode, $"note_details_referral_list[{index}].patient_national_code");
                AddJalaliDate(failures, detail.ReferralHijriDate, $"note_details_referral_list[{index}].referral_hijri_date");
                AddRequired(failures, detail.SiamId, $"note_details_referral_list[{index}].siam_id");
                AddRequiredItems(failures, detail.Icd10Items, $"note_details_referral_list[{index}].icd10s");
                if (detail.Icd10Items is not null)
                {
                    for (var icdIndex = 0; icdIndex < detail.Icd10Items.Count; icdIndex++)
                        AddRequired(failures, detail.Icd10Items[icdIndex].IcdCode, $"note_details_referral_list[{index}].icd10s[{icdIndex}].icd_code");
                }
            }
        }
        return failures;
    }

    public IReadOnlyList<ValidationFailure> Validate(HospitalizationSecretaryListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = new List<ValidationFailure>();
        AddRequired(failures, request.SiamId, "siam_id");
        AddNationalCode(failures, request.SecretaryNationalCode, "secretary_national_code");
        return failures;
    }

    public IReadOnlyList<ValidationFailure> Validate(NurseTodoListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = new List<ValidationFailure>();
        AddRequired(failures, request.SiamId, "siam_id");
        AddNationalCode(failures, request.NurseNationalCode ?? request.PatientNationalCode, "nurse_national_code");
        return failures;
    }

    public IReadOnlyList<ValidationFailure> Validate(NurseActionWorkflowRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = new List<ValidationFailure>(Validate(NurseTodoListRequestFrom(request)));
        AddRequiredItems(failures, request.NoteDetailsEprscIds, "note_details_eprsc_ids");
        if (request.NoteDetailsEprscIds is not null)
            for (var index = 0; index < request.NoteDetailsEprscIds.Count; index++)
                if (request.NoteDetailsEprscIds[index] <= 0) failures.Add(new ValidationFailure($"note_details_eprsc_ids[{index}]", "positive", "Value must be greater than zero."));
        return failures;
    }

    public IReadOnlyList<ValidationFailure> Validate(ReferralCountRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = new List<ValidationFailure>();
        AddNationalCode(failures, request.PatientNationalCode, "patient_national_code");
        AddRequired(failures, request.DoctorId, "doctor_id");
        return failures;
    }

    public IReadOnlyList<ValidationFailure> Validate(ReferralFeedbackRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = new List<ValidationFailure>();
        AddPositive(failures, request.Id, "id");
        AddPositive(failures, request.MasterParent, "master_parent");
        return failures;
    }

    public IReadOnlyList<ValidationFailure> ValidateReferralTracking(long masterId, string doctorId, string trackingCode)
    {
        var failures = new List<ValidationFailure>();
        AddPositive(failures, masterId, "master_id");
        AddRequired(failures, doctorId, "doctor_id");
        AddRequired(failures, trackingCode, "tracking_code");
        return failures;
    }

    public IReadOnlyList<ValidationFailure> ValidateReferralCartable(string doctorNationalCode, string patientNationalCode, string trackingCode)
    {
        var failures = new List<ValidationFailure>();
        AddNationalCode(failures, doctorNationalCode, "doctor_national_code");
        AddNationalCode(failures, patientNationalCode, "patient_national_code");
        AddRequired(failures, trackingCode, "tracking_code");
        return failures;
    }

    public IReadOnlyList<ValidationFailure> ValidatePositiveIdentifier(long value, string field)
    {
        var failures = new List<ValidationFailure>();
        AddPositive(failures, value, field);
        return failures;
    }

    public IReadOnlyList<ValidationFailure> ValidatePatientNationalCode(string patientNationalCode)
    {
        var failures = new List<ValidationFailure>();
        AddNationalCode(failures, patientNationalCode, "patient_national_code");
        return failures;
    }

    internal void ThrowIfInvalid(IReadOnlyList<ValidationFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        if (failures.Count > 0)
            throw new TaminValidationException(failures);
    }

    private List<ValidationFailure> ValidatePrescriptionHeader(int actualPrescriptionType, PrescriptionType expectedPrescriptionType, ProviderPrescriptionIdentity provider, string visitDate)
    {
        var failures = ValidateProviderIdentity(provider);
        AddPrescriptionType(failures, actualPrescriptionType, expectedPrescriptionType);
        AddJalaliDate(failures, visitDate, "visit_date");
        return failures;
    }

    private List<ValidationFailure> ValidateProviderIdentity(ProviderPrescriptionIdentity provider)
    {
        var failures = new List<ValidationFailure>();
        AddRequired(failures, provider.DoctorId, "doctor_id");
        if (!string.IsNullOrWhiteSpace(provider.DoctorNationalCode))
            AddDoctorNationalCode(failures, provider.DoctorNationalCode, "doctor_national_code");
        AddOptionalMobile(failures, provider.DoctorMobileNumber, "doctor_mobile_number");
        AddNationalCode(failures, provider.PatientNationalId, "patient_national_id");
        return failures;
    }

    private List<ValidationFailure> ValidatePrescriptionIdentity(int headerId, string? doctorNationalCode, string doctorId)
    {
        var failures = new List<ValidationFailure>();
        AddPositive(failures, headerId, "header_id");
        AddDoctorNationalCode(failures, doctorNationalCode, "doctor_national_code");
        AddRequired(failures, doctorId, "doctor_id");
        return failures;
    }

    private static void AddPrescriptionType(List<ValidationFailure> failures, int actual, PrescriptionType expected)
    {
        if (actual != (int)expected)
            failures.Add(new ValidationFailure("prescription_type", "prescription-type-mismatch", $"This request must use prescription type {(int)expected} ({expected})."));
    }

    private void AddJalaliDate(List<ValidationFailure> failures, string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add(new ValidationFailure(field, "required", "A provider-bound string value is required."));
            return;
        }

        if (!_serializer.IsValidJalaliDate(value))
        {
            failures.Add(new ValidationFailure(
                field,
                "jalali-date-shape",
                "Provider dates must be eight Jalali digits such as 14030501; ISO dates must be converted before calling the SDK."));
        }
    }

    private void AddServiceItems(List<ValidationFailure> failures, IReadOnlyList<ServiceItem> items, string field, bool requireGroup, bool requireEffectiveDate)
    {
        AddRequiredItems(failures, items, field);
        if (items is null)
            return;

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            AddRequired(failures, item.ServiceCode, $"{field}[{index}].service_code");
            AddPositive(failures, item.Quantity, $"{field}[{index}].quantity");
            if (requireGroup)
                AddRequired(failures, item.ServiceGroup, $"{field}[{index}].service_group");
            if (requireEffectiveDate)
                AddRequired(failures, item.EffectiveDate, $"{field}[{index}].effective_date");
            if (!string.IsNullOrWhiteSpace(item.EffectiveDate))
                AddJalaliDate(failures, item.EffectiveDate, $"{field}[{index}].effective_date");
        }
    }

    private static void AddRequiredItems<T>(List<ValidationFailure> failures, IReadOnlyList<T>? items, string field)
    {
        if (items is null || items.Count == 0)
            failures.Add(new ValidationFailure(field, "required-items", "At least one item is required."));
    }

    private static void AddPositive(List<ValidationFailure> failures, int value, string field)
    {
        if (value <= 0)
            failures.Add(new ValidationFailure(field, "positive", "Value must be greater than zero."));
    }

    private static void AddPositive(List<ValidationFailure> failures, long value, string field)
    {
        if (value <= 0)
            failures.Add(new ValidationFailure(field, "positive", "Value must be greater than zero."));
    }

    private static void AddNationalCode(List<ValidationFailure> failures, string? value, string field)
    {
        AddRequired(failures, value, field);
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length != 10)
            failures.Add(new ValidationFailure(field, "national-code-shape", "National code must be exactly 10 characters."));
    }

    private static void AddDoctorNationalCode(List<ValidationFailure> failures, string? value, string field)
    {
        AddRequired(failures, value, field);
        if (string.IsNullOrWhiteSpace(value)) return;
        var trimmed = value.Trim();
        if (trimmed.Length != 10 && !trimmed.StartsWith("FIDA", StringComparison.OrdinalIgnoreCase) && !trimmed.StartsWith("FDA", StringComparison.OrdinalIgnoreCase))
            failures.Add(new ValidationFailure(field, "doctor-national-code-shape", "Doctor national code must be 10 characters or documented FDA/FIDA format for foreign doctors."));
    }

    private static NurseTodoListRequest NurseTodoListRequestFrom(NurseActionWorkflowRequest request)
        => new() { SiamId = request.SiamId, NurseNationalCode = request.NurseNationalCode };

    private static void AddRequired(List<ValidationFailure> failures, string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            failures.Add(new ValidationFailure(field, "required", "A value is required."));
    }

    private static void AddOptionalMobile(List<ValidationFailure> failures, string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var trimmed = value.Trim();
        if (trimmed.Length != 11 || !trimmed.All(IsLatinDigit) || !trimmed.StartsWith("09", StringComparison.Ordinal))
            failures.Add(new ValidationFailure(field, "mobile-shape", "Mobile numbers must be 11 digits and start with 09."));
    }

    private static bool IsLatinDigit(char value)
        => value is >= '0' and <= '9';
}
