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
        var failures = ValidatePrescriptionHeader(request.PrescriptionType, PrescriptionType.Visit, request.DoctorId, request.PatientNationalId, request.VisitDate);
        AddRequired(failures, request.ClinicId, "clinic_id");
        AddOptionalMobile(failures, request.MobileNumber, "mobile_number");
        return failures;
    }

    public IReadOnlyList<ValidationFailure> Validate(RegisterDrugPrescriptionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = ValidatePrescriptionHeader(request.PrescriptionType, PrescriptionType.Drug, request.DoctorId, request.PatientNationalId, request.VisitDate);
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
        var failures = ValidatePrescriptionHeader(request.PrescriptionType, PrescriptionType.Paraclinic, request.DoctorId, request.PatientNationalId, request.VisitDate);
        AddServiceItems(failures, request.ServiceItems, "service_items", requireGroup: true, requireEffectiveDate: false);
        return failures;
    }

    public IReadOnlyList<ValidationFailure> Validate(RegisterMedicalServicePrescriptionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = ValidatePrescriptionHeader(request.PrescriptionType, PrescriptionType.Service, request.DoctorId, request.PatientNationalId, request.VisitDate);
        AddServiceItems(failures, request.ServiceItems, "service_items", requireGroup: false, requireEffectiveDate: false);
        return failures;
    }

    public IReadOnlyList<ValidationFailure> Validate(RegisterReferralPrescriptionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = ValidatePrescriptionHeader(request.PrescriptionType, PrescriptionType.Referral, request.DoctorId, request.PatientNationalId, request.VisitDate);
        AddRequired(failures, request.TargetSpecialty, "target_specialty");
        AddRequired(failures, request.TargetProviderType, "target_provider_type");
        AddRequired(failures, request.Reason, "reason");
        return failures;
    }

    public IReadOnlyList<ValidationFailure> Validate(RegisterPhysiotherapyPrescriptionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = ValidateRequiredCodes(request.DoctorId, request.PatientNationalId);
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
        var failures = ValidateRequiredCodes(request.DoctorId, request.PatientNationalId);
        AddRequiredItems(failures, request.PrescriptionItems, "prescription_items");
        return failures;
    }

    internal void ThrowIfInvalid(IReadOnlyList<ValidationFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        if (failures.Count > 0)
            throw new TaminValidationException(failures);
    }

    private List<ValidationFailure> ValidatePrescriptionHeader(int actualPrescriptionType, PrescriptionType expectedPrescriptionType, string doctorId, string patientNationalId, string visitDate)
    {
        var failures = ValidateRequiredCodes(doctorId, patientNationalId);
        AddPrescriptionType(failures, actualPrescriptionType, expectedPrescriptionType);
        AddJalaliDate(failures, visitDate, "visit_date");
        return failures;
    }

    private List<ValidationFailure> ValidateRequiredCodes(string doctorId, string patientNationalId)
    {
        var failures = new List<ValidationFailure>();
        AddRequired(failures, doctorId, "doctor_id");
        AddRequired(failures, patientNationalId, "patient_national_id");
        if (!string.IsNullOrWhiteSpace(patientNationalId) && patientNationalId.Trim().Length != 10)
            failures.Add(new ValidationFailure("patient_national_id", "national-code-shape", "Patient national code must be exactly 10 characters."));
        return failures;
    }

    private List<ValidationFailure> ValidatePrescriptionIdentity(int headerId, string doctorNationalCode, string doctorId)
    {
        var failures = new List<ValidationFailure>();
        AddPositive(failures, headerId, "header_id");
        AddRequired(failures, doctorNationalCode, "doctor_national_code");
        if (!string.IsNullOrWhiteSpace(doctorNationalCode) && doctorNationalCode.Trim().Length != 10)
            failures.Add(new ValidationFailure("doctor_national_code", "national-code-shape", "Doctor national code must be exactly 10 characters."));
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
        try
        {
            _serializer.SerializeJalaliDate(value, field);
        }
        catch (TaminValidationException ex)
        {
            failures.AddRange(ex.Failures);
        }
    }

    private static void AddServiceItems(List<ValidationFailure> failures, IReadOnlyList<ServiceItem> items, string field, bool requireGroup, bool requireEffectiveDate)
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
            if (requireEffectiveDate && string.IsNullOrWhiteSpace(item.EffectiveDate))
                AddRequired(failures, item.EffectiveDate, $"{field}[{index}].effective_date");
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
        if (trimmed.Length != 11 || !trimmed.All(char.IsDigit) || !trimmed.StartsWith("09", StringComparison.Ordinal))
            failures.Add(new ValidationFailure(field, "mobile-shape", "Mobile numbers must be 11 digits and start with 09."));
    }
}
