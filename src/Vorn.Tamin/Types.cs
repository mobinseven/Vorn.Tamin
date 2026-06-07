namespace Vorn.Tamin;

/// <summary>Identifies the category of items in a prescription.</summary>
public enum PrescriptionType
{
    /// <summary>Drug / medication prescription.</summary>
    Drug = 1,
    /// <summary>Paraclinic (lab, imaging, diagnostic) prescription.</summary>
    Paraclinic = 2,
    /// <summary>Visit-only prescription.</summary>
    Visit = 3,
    /// <summary>Visit plus services prescription.</summary>
    VisitService = 4,
    /// <summary>Medical service prescription.</summary>
    Service = 5,
    /// <summary>Referral prescription.</summary>
    Referral = 6,
    /// <summary>Physiotherapy prescription.</summary>
    Physiotherapy = 7,
}

