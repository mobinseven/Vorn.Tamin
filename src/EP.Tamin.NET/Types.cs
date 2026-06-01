namespace EP.Tamin.NET;

/// <summary>
/// Represents a generic prescription payload as a key-value dictionary.
/// Prefer the strongly-typed request DTOs in <see cref="PrescriptionClient"/> where possible.
/// </summary>
public sealed class Prescription
{
    /// <summary>Arbitrary prescription fields.</summary>
    public required Dictionary<string, object?> Values { get; init; }
}

/// <summary>
/// Represents the doctor type in a prescription.
/// The default value <c>"1"</c> denotes a regular physician.
/// </summary>
public sealed record DocEprsc(string DocType = "1");

/// <summary>Status values for a prescription through its lifecycle.</summary>
public enum PrescriptionStatus
{
    /// <summary>Created locally but not yet submitted.</summary>
    Draft,
    /// <summary>Sent to the official API.</summary>
    Submitted,
    /// <summary>Accepted by the official API.</summary>
    Accepted,
    /// <summary>Rejected by the official API.</summary>
    Rejected,
    /// <summary>Accepted conditionally or requires user attention.</summary>
    Warning,
    /// <summary>Changed after original registration.</summary>
    Edited,
    /// <summary>Deleted or cancelled where allowed.</summary>
    Deleted,
    /// <summary>Waiting for retry after temporary failure.</summary>
    PendingSync,
    /// <summary>Submission failed and requires support review.</summary>
    Failed,
}

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

