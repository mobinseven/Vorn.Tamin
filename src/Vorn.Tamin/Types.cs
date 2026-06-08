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
    /// <summary>Referral feedback prescription; official table lists this as 8.</summary>
    ReferralFeedback = 8,
    /// <summary>Hospitalization order prescription. XML note: some documents mention <c>prescTypeId</c> 8 near hospitalization samples, but the generated OpenAPI/model lists 8 as referral feedback and 9 as hospitalization order, so the SDK uses 9.</summary>
    HospitalizationOrder = 9,
}

