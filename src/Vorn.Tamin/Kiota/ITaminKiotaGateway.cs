using System.Text.Json;
using Vorn.Tamin.Kiota.Models;

namespace Vorn.Tamin.Kiota;

/// <summary>
/// Executes friendly-layer operations through generated Kiota request builders without exposing those builders publicly.
/// </summary>
internal interface ITaminKiotaGateway
{
    Task<JsonElement> GetServicesAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken);

    Task<JsonElement> GetPrescriptionTypesAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken);

    Task<JsonElement> GetParaclinicTariffsAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken);

    Task<JsonElement> GetDrugAmountsAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken);

    Task<JsonElement> GetDrugInstructionsAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken);

    Task<JsonElement> SendPrescriptionAsync(SendEprescRequest request, CancellationToken cancellationToken);

    Task<JsonElement> GetPrescriptionAsync(int headerId, string doctorNationalCode, string doctorId, CancellationToken cancellationToken);

    Task<JsonElement> EditPrescriptionAsync(int headerId, string doctorNationalCode, string doctorId, IReadOnlyList<NoteDetailEprsc> details, CancellationToken cancellationToken);

    Task<JsonElement> RemovePrescriptionAsync(int headerId, string doctorNationalCode, string doctorId, CancellationToken cancellationToken);

    Task<JsonElement> CheckPrescriptionWarningAsync(DentistRuleRequest request, CancellationToken cancellationToken);

    Task<JsonElement> GetEligibilityAsync(string requestBy, string siamId, string doctorId, string patientNationalCode, CancellationToken cancellationToken);
}
