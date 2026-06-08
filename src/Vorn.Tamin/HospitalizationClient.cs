using System.Text.Json;
using Vorn.Tamin.Kiota;
using Vorn.Tamin.Mapping;

namespace Vorn.Tamin;

/// <summary>Orchestrates hospitalization commands and queries through provider gateway contracts.</summary>
public sealed class HospitalizationClient
{
    private readonly ITaminKiotaGateway _gateway;
    private readonly PrescriptionValidationRules _validationRules;

    internal HospitalizationClient(ITaminKiotaGateway gateway, PrescriptionValidationRules? validationRules = null)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _validationRules = validationRules ?? new PrescriptionValidationRules();
    }

    /// <summary>Creates a documented hospitalization SendEpresc command payload.</summary>
    public Task<JsonElement> CreateAsync(HospitalizationCreateRequest request, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.Validate(request));
        return _gateway.SendPrescriptionAsync(TaminRequestMapper.ToSendEprescRequest(request, _gateway.Endpoint), cancellationToken);
    }

    /// <summary>Returns hospitalization orders for a secretary and SIAM center.</summary>
    public Task<JsonElement> GetSecretaryListAsync(HospitalizationSecretaryListRequest request, CancellationToken cancellationToken = default)
    {
        _validationRules.ThrowIfInvalid(_validationRules.Validate(request));
        return _gateway.GetHospitalizationSecretaryListAsync(request.SiamId, request.SecretaryNationalCode, cancellationToken);
    }
}
