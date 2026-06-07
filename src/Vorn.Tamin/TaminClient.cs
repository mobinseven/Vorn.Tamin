namespace Vorn.Tamin;

/// <summary>Exposes the SDK as role-aware workflow clients from one entry point.</summary>
public sealed class TaminClient
{
    /// <summary>Creates a role-aware facade over an authenticated session.</summary>
    public TaminClient(TaminSession session)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        ReferenceData = session.ReferenceData;
        Prescriptions = session.Prescription;
        Dentistry = session.Dentistry;
        Referrals = session.Referrals;
        Eligibility = session.Eligibility;
        Hospitalization = session.Hospitalization;
        Doctor = session.Doctor;
        Secretary = session.Secretary;
        Nurse = session.Nurse;
    }

    /// <summary>The underlying session that owns authentication and transport state.</summary>
    public TaminSession Session { get; }

    /// <summary>Doctor-facing workflows.</summary>
    public DoctorClient Doctor { get; }

    /// <summary>Secretary-facing workflows.</summary>
    public SecretaryClient Secretary { get; }

    /// <summary>Nurse-facing workflows.</summary>
    public NurseClient Nurse { get; }

    /// <summary>Reference-data queries.</summary>
    public ReferenceDataClient ReferenceData { get; }

    /// <summary>Prescription commands and queries.</summary>
    public PrescriptionClient Prescriptions { get; }

    /// <summary>Dental rule-check workflows.</summary>
    public DentistryClient Dentistry { get; }

    /// <summary>Referral workflows.</summary>
    public ReferralClient Referrals { get; }

    /// <summary>Eligibility lookup workflows.</summary>
    public EligibilityClient Eligibility { get; }

    /// <summary>Hospitalization workflows.</summary>
    public HospitalizationClient Hospitalization { get; }
}
