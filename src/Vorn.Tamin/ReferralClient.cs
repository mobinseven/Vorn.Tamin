using System.Text.Json;
using Vorn.Tamin.Kiota;

namespace Vorn.Tamin;

/// <summary>Orchestrates referral commands separately from referral query workflows.</summary>
public sealed class ReferralClient
{
    private readonly PrescriptionClient _prescriptions;
    private readonly ITaminKiotaGateway _gateway;
    private readonly PrescriptionValidationRules _validationRules;

    internal ReferralClient(PrescriptionClient prescriptions, ITaminKiotaGateway gateway, PrescriptionValidationRules? validationRules = null)
    {
        _prescriptions = prescriptions ?? throw new ArgumentNullException(nameof(prescriptions));
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _validationRules = validationRules ?? new PrescriptionValidationRules();
    }

    /// <summary>Registers a referral prescription command.</summary>
    public Task<JsonElement> RegisterPrescriptionAsync(RegisterReferralPrescriptionRequest request, CancellationToken cancellationToken = default)
        => _prescriptions.RegisterReferralPrescriptionAsync(request, cancellationToken);

    /// <summary>Returns referral counts for a patient and doctor.</summary>
    public Task<JsonElement> GetCountsAsync(ReferralCountRequest request, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.Validate(request));
        return _gateway.GetReferralCountAsync(request.PatientNationalCode, request.DoctorId, cancellationToken);
    }

    /// <summary>Finds a physician referral prescription by master identifier, doctor identifier, and tracking code.</summary>
    public Task<JsonElement> FindNoteReferralAsync(long masterId, string doctorId, string trackingCode, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.ValidateReferralTracking(masterId, doctorId, trackingCode));
        return _gateway.FindNoteReferralAsync(masterId, doctorId, trackingCode, cancellationToken);
    }

    /// <summary>Fetches the referral cartable row for a doctor, patient, and tracking code.</summary>
    public Task<JsonElement> FetchCartableAsync(string doctorNationalCode, string patientNationalCode, string trackingCode, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.ValidateReferralCartable(doctorNationalCode, patientNationalCode, trackingCode));
        return _gateway.FetchReferralCartableAsync(doctorNationalCode, patientNationalCode, trackingCode, cancellationToken);
    }

    /// <summary>Returns registered referral feedback prescriptions for a referral identifier.</summary>
    public Task<JsonElement> GetReferralListAsync(long referralId, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.ValidatePositiveIdentifier(referralId, "referral_id"));
        return _gateway.GetReferralListAsync(referralId, cancellationToken);
    }

    /// <summary>Returns referral feedback note detail.</summary>
    public Task<JsonElement> GetNoteDetailAsync(ReferralFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.Validate(request));
        return _gateway.GetReferralNoteDetailAsync(request.Id, request.MasterParent, cancellationToken);
    }

    /// <summary>Returns the open referral count visible for a patient.</summary>
    public Task<JsonElement> GetPatientOpenCountAsync(string patientNationalCode, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.ValidatePatientNationalCode(patientNationalCode));
        return _gateway.GetPatientOpenReferralCountAsync(patientNationalCode, cancellationToken);
    }
}
