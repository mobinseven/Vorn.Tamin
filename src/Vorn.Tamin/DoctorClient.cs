namespace Vorn.Tamin;

/// <summary>Exposes doctor-facing workflows without owning validation, serialization, transport, or error rules.</summary>
public sealed class DoctorClient
{
    internal DoctorClient(ReferenceDataClient referenceData, PrescriptionClient prescriptions, DentistryClient dentistry, ReferralClient referrals)
    {
        ReferenceData = referenceData ?? throw new ArgumentNullException(nameof(referenceData));
        Prescriptions = prescriptions ?? throw new ArgumentNullException(nameof(prescriptions));
        Dentistry = dentistry ?? throw new ArgumentNullException(nameof(dentistry));
        Referrals = referrals ?? throw new ArgumentNullException(nameof(referrals));
    }

    /// <summary>Reference-data queries available to doctor workflows.</summary>
    public ReferenceDataClient ReferenceData { get; }

    /// <summary>Prescription commands and queries available to doctor workflows.</summary>
    public PrescriptionClient Prescriptions { get; }

    /// <summary>Dental rule-check workflow available to doctor workflows.</summary>
    public DentistryClient Dentistry { get; }

    /// <summary>Referral workflows available to doctor workflows.</summary>
    public ReferralClient Referrals { get; }
}
