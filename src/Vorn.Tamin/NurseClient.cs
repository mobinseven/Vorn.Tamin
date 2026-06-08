using System.Text.Json;
using Vorn.Tamin.Kiota;
using KiotaModels = Vorn.Tamin.Kiota.Models;

namespace Vorn.Tamin;

/// <summary>Exposes nurse-facing to-do retrieval queries and action-recording commands.</summary>
public sealed class NurseClient
{
    private readonly ITaminKiotaGateway _gateway;
    private readonly PrescriptionValidationRules _validationRules;

    internal NurseClient(ITaminKiotaGateway gateway, PrescriptionValidationRules? validationRules = null)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _validationRules = validationRules ?? new PrescriptionValidationRules();
    }

    /// <summary>Returns the nurse to-do list for a SIAM center and nurse national code.</summary>
    public Task<JsonElement> GetTodoListAsync(NurseTodoListRequest request, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.Validate(request));
        return _gateway.GetNurseTodoListAsync(request.SiamId, request.NurseNationalCode ?? request.PatientNationalCode!, cancellationToken);
    }

    /// <summary>Records completed nursing actions for prescription detail identifiers.</summary>
    public Task<JsonElement> RecordActionAsync(NurseActionWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.Validate(request));
        return _gateway.SaveNurseActionAsync(new KiotaModels.NurseActionRequest
        {
            SiamId = request.SiamId,
            NurseNationalCode = request.NurseNationalCode,
            NoteDetailsEprscIds = request.NoteDetailsEprscIds.Select(id => (long?)id).ToList()
        }, cancellationToken);
    }
}
