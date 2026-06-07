using System.Text.Json;
using Vorn.Tamin.Kiota;
using Vorn.Tamin.Mapping;

namespace Vorn.Tamin;

/// <summary>
/// Provides e-prescription operations that are backed by generated Kiota request builders.
/// </summary>
public sealed class PrescriptionClient
{
    private readonly ITaminKiotaGateway _gateway;
    private readonly PrescriptionValidationRules _validationRules;

    internal PrescriptionClient(ITaminKiotaGateway gateway, PrescriptionValidationRules? validationRules = null)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _validationRules = validationRules ?? new PrescriptionValidationRules();
    }

    // ── Section 8: E-Prescription Writing ────────────────────────────────────

    /// <summary>Registers a visit-only prescription or encounter through the generated SendEpresc builder.</summary>
    public Task<JsonElement> RegisterVisitPrescriptionAsync(RegisterVisitPrescriptionRequest request, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.Validate(request));
        return _gateway.SendPrescriptionAsync(TaminRequestMapper.ToSendEprescRequest(request), cancellationToken);
    }

    /// <summary>Submits prescribed drug items through the generated SendEpresc builder.</summary>
    public Task<JsonElement> RegisterDrugPrescriptionAsync(RegisterDrugPrescriptionRequest request, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.Validate(request));
        return _gateway.SendPrescriptionAsync(TaminRequestMapper.ToSendEprescRequest(request), cancellationToken);
    }

    /// <summary>Submits laboratory, imaging, diagnostic, or other paraclinic orders through the generated SendEpresc builder.</summary>
    public Task<JsonElement> RegisterParaclinicPrescriptionAsync(RegisterParaclinicPrescriptionRequest request, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.Validate(request));
        return _gateway.SendPrescriptionAsync(TaminRequestMapper.ToSendEprescRequest(request), cancellationToken);
    }

    /// <summary>Submits physician-provided services or other medical service orders through the generated SendEpresc builder.</summary>
    public Task<JsonElement> RegisterMedicalServicePrescriptionAsync(RegisterMedicalServicePrescriptionRequest request, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.Validate(request));
        return _gateway.SendPrescriptionAsync(TaminRequestMapper.ToSendEprescRequest(request), cancellationToken);
    }

    /// <summary>Registers a referral to another provider, specialty, or service centre through the generated SendEpresc builder.</summary>
    public Task<JsonElement> RegisterReferralPrescriptionAsync(RegisterReferralPrescriptionRequest request, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.Validate(request));
        return _gateway.SendPrescriptionAsync(TaminRequestMapper.ToSendEprescRequest(request), cancellationToken);
    }

    /// <summary>Registers a physiotherapy prescription through the generated SendEpresc builder.</summary>
    public Task<JsonElement> RegisterPhysiotherapyPrescriptionAsync(RegisterPhysiotherapyPrescriptionRequest request, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.Validate(request));
        return _gateway.SendPrescriptionAsync(TaminRequestMapper.ToSendEprescRequest(request), cancellationToken);
    }

    // ── Section 9: Prescription Query ────────────────────────────────────────

    /// <summary>Retrieves a registered prescription by generated Kiota header, doctor national code, and NPI path parameters.</summary>
    public Task<JsonElement> GetRegisteredPrescriptionAsync(int headerId, string doctorNationalCode, string doctorId, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.ValidateRegisteredPrescriptionIdentity(headerId, doctorNationalCode, doctorId));
        return _gateway.GetPrescriptionAsync(headerId, doctorNationalCode, doctorId, cancellationToken);
    }

    // ── Section 10: Prescription Mutation ────────────────────────────────────

    /// <summary>Edits an already-registered electronic prescription through the generated edit builder.</summary>
    public Task<JsonElement> EditElectronicPrescriptionAsync(EditPrescriptionRequest request, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.Validate(request));
        return _gateway.EditPrescriptionAsync(request.HeaderId, request.DoctorNationalCode, request.DoctorId, TaminRequestMapper.ToNoteDetails(request.EditedItems), cancellationToken);
    }

    /// <summary>Cancels or deletes a registered electronic prescription through the generated remove builder.</summary>
    public Task<JsonElement> DeleteElectronicPrescriptionAsync(DeletePrescriptionRequest request, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.Validate(request));
        return _gateway.RemovePrescriptionAsync(request.HeaderId, request.DoctorNationalCode, request.DoctorId, cancellationToken);
    }

    // ── Section 14: Warning Services ─────────────────────────────────────────

    /// <summary>Returns dental prescription warnings through the generated check-rules-in-detail builder.</summary>
    public Task<JsonElement> CheckPrescriptionWarningAsync(CheckWarningRequest request, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.Validate(request));
        return _gateway.CheckPrescriptionWarningAsync(TaminRequestMapper.ToDentistRuleRequest(request), cancellationToken);
    }
}
