using System.Text.Json;

namespace Vorn.Tamin.Kiota;

/// <summary>
/// Executes friendly-layer operations through generated Kiota request builders without exposing those builders publicly.
/// </summary>
internal interface ITaminKiotaGateway
{
    Task<JsonElement> GetServicesAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken);

    Task<JsonElement> GetPrescriptionTypesAsync(CancellationToken cancellationToken);

    Task<JsonElement> GetParaclinicTariffsAsync(CancellationToken cancellationToken);

    Task<JsonElement> GetDrugAmountsAsync(CancellationToken cancellationToken);

    Task<JsonElement> GetDrugInstructionsAsync(CancellationToken cancellationToken);

    Task<JsonElement> GetAllowedCountAsync(IReadOnlyDictionary<string, string?> query, CancellationToken cancellationToken);

    Task<JsonElement> GetPriceAsync(IReadOnlyDictionary<string, string?> query, CancellationToken cancellationToken);

    Task<JsonElement> SendPrescriptionAsync<TPayload>(TPayload payload, CancellationToken cancellationToken);

    Task<JsonElement> GetPrescriptionAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken);

    Task<JsonElement> EditPrescriptionAsync(EditPrescriptionRequest request, CancellationToken cancellationToken);

    Task<JsonElement> RemovePrescriptionAsync(DeletePrescriptionRequest request, CancellationToken cancellationToken);

    Task<JsonElement> CheckPrescriptionWarningAsync(CheckWarningRequest request, CancellationToken cancellationToken);

    Task<JsonElement> GetAsync(TaminGatewayRoute route, IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken);

    Task<JsonElement> PostAsync<TPayload>(TaminGatewayRoute route, TPayload payload, CancellationToken cancellationToken);
}
