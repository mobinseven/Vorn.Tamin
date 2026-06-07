using System.Text.Json;

namespace Vorn.Tamin;

/// <summary>Orchestrates referral workflows without owning provider validation or transport details.</summary>
public sealed class ReferralClient
{
    private readonly PrescriptionClient _prescriptions;

    internal ReferralClient(PrescriptionClient prescriptions)
    {
        _prescriptions = prescriptions ?? throw new ArgumentNullException(nameof(prescriptions));
    }

    /// <summary>Registers a referral prescription command.</summary>
    public Task<JsonElement> RegisterPrescriptionAsync(RegisterReferralPrescriptionRequest request, CancellationToken cancellationToken = default)
        => _prescriptions.RegisterReferralPrescriptionAsync(request, cancellationToken);

    /// <summary>Returns referral counts when the provider builder is wired.</summary>
    public Task<JsonElement> GetCountsAsync(ReferralCountRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        throw TaminWorkflowNotImplementedException.For("referral counts", "The provider request builder is not wired into the role workflow surface yet.");
    }

    /// <summary>Records referral feedback when the provider builder is wired.</summary>
    public Task<JsonElement> RecordFeedbackAsync(ReferralFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        throw TaminWorkflowNotImplementedException.For("referral feedback", "The provider request builder is not wired into the role workflow surface yet.");
    }
}
