using System.Text.RegularExpressions;

namespace Vorn.Tamin;

/// <summary>Converts SDK request values into provider-safe string codes and Jalali date shapes.</summary>
public sealed partial class TaminProviderSerializer
{
    /// <summary>Returns a trimmed provider code while preserving leading zeros, dashes, and alphabetic markers.</summary>
    public string SerializeStringCode(string? value, string field)
    {
        var failures = ValidateText(value, field);
        if (failures.Count > 0)
            throw new TaminValidationException(failures);

        return value!.Trim();
    }

    /// <summary>Checks whether the value is an eight-character Jalali date string.</summary>
    public bool IsValidJalaliDate(string? value)
        => !string.IsNullOrWhiteSpace(value) && JalaliDateRegex().IsMatch(value.Trim());

    /// <summary>Returns an eight-character Jalali date string after rejecting ISO/Gregorian-looking dates.</summary>
    public string SerializeJalaliDate(string? value, string field)
    {
        if (IsValidJalaliDate(value))
            return value!.Trim();

        var failures = ValidateText(value, field);
        if (failures.Count == 0)
        {
            failures.Add(new ValidationFailure(
                field,
                "jalali-date-shape",
                "Provider dates must be eight Jalali digits such as 14030501; ISO dates must be converted before calling the SDK."));
        }

        throw new TaminValidationException(failures);
    }

    /// <summary>Returns an optional provider code, preserving the original code characters when present.</summary>
    public string? SerializeOptionalStringCode(string? value, string field)
        => string.IsNullOrWhiteSpace(value) ? null : SerializeStringCode(value, field);

    /// <summary>Returns an optional Jalali date string when a provider date was supplied.</summary>
    public string? SerializeOptionalJalaliDate(string? value, string field)
        => string.IsNullOrWhiteSpace(value) ? null : SerializeJalaliDate(value, field);

    internal static List<ValidationFailure> ValidateText(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(field))
            throw new ArgumentException("Field name is required.", nameof(field));

        return string.IsNullOrWhiteSpace(value)
            ? [new ValidationFailure(field, "required", "A provider-bound string value is required.")]
            : [];
    }

    [GeneratedRegex("^[0-9]{8}$", RegexOptions.CultureInvariant)]
    private static partial Regex JalaliDateRegex();
}
