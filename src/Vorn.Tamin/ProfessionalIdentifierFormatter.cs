namespace Vorn.Tamin;

/// <summary>Applies midwife and foreign-doctor professional identifier formatting rules.</summary>
public sealed class ProfessionalIdentifierFormatter
{
    private readonly TaminProviderSerializer _serializer;

    /// <inheritdoc />
    public ProfessionalIdentifierFormatter(TaminProviderSerializer? serializer = null)
    {
        _serializer = serializer ?? new TaminProviderSerializer();
    }

    /// <summary>Formats a medical council identifier for a midwife by using the provider's asterisk marker for the Persian letter م.</summary>
    public string FormatMidwifeDoctorId(string doctorId)
        => _serializer.SerializeStringCode(doctorId, nameof(doctorId)).Replace('م', '*');

    /// <summary>Formats a foreign doctor's national identifier as the provider-required FIDA-prefixed code.</summary>
    public string FormatForeignDoctorNationalCode(string fidaCode)
    {
        var value = _serializer.SerializeStringCode(fidaCode, nameof(fidaCode));
        return value.StartsWith("FIDA", StringComparison.OrdinalIgnoreCase)
            ? $"FIDA{value[4..]}"
            : $"FIDA{value}";
    }
}
