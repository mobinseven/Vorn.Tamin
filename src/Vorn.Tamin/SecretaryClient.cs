namespace Vorn.Tamin;

/// <summary>Exposes secretary-facing workflows without presenting them as doctor operations.</summary>
public sealed class SecretaryClient
{
    internal SecretaryClient(EligibilityClient eligibility, HospitalizationClient hospitalization)
    {
        Eligibility = eligibility ?? throw new ArgumentNullException(nameof(eligibility));
        Hospitalization = hospitalization ?? throw new ArgumentNullException(nameof(hospitalization));
    }

    /// <summary>Patient eligibility lookup workflow.</summary>
    public EligibilityClient Eligibility { get; }

    /// <summary>Hospitalization list and creation workflow.</summary>
    public HospitalizationClient Hospitalization { get; }
}
