namespace Vorn.Tamin;

/// <summary>Owns pre-send eligibility identifier checks.</summary>
public sealed class EligibilityValidationRules
{
    /// <summary>Validates identifiers required for private-practice eligibility lookup.</summary>
    public IReadOnlyList<ValidationFailure> ValidatePrivatePracticeIdentifiers(string doctorId, string doctorNationalCode, string patientNationalCode)
    {
        var failures = new List<ValidationFailure>();
        AddRequired(failures, doctorId, "doctor_id");
        AddNationalCode(failures, doctorNationalCode, "doctor_national_code");
        AddNationalCode(failures, patientNationalCode, "patient_national_code");
        return failures;
    }

    private static void AddNationalCode(List<ValidationFailure> failures, string? value, string field)
    {
        AddRequired(failures, value, field);
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length != 10)
            failures.Add(new ValidationFailure(field, "national-code-shape", "National code must be exactly 10 characters."));
    }

    private static void AddRequired(List<ValidationFailure> failures, string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            failures.Add(new ValidationFailure(field, "required", "A value is required."));
    }
}
