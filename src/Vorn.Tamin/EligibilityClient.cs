using System.Text.Json;
using Vorn.Tamin.Kiota;

namespace Vorn.Tamin;

/// <summary>Orchestrates patient eligibility lookup without owning identifier validation rules.</summary>
public sealed class EligibilityClient
{
    private readonly ITaminKiotaGateway _gateway;
    private readonly EligibilityValidationRules _validationRules;

    internal EligibilityClient(ITaminKiotaGateway gateway, EligibilityValidationRules? validationRules = null)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _validationRules = validationRules ?? new EligibilityValidationRules();
    }

    /// <summary>Looks up private-practice patient eligibility.</summary>
    public Task<JsonElement> LookupPrivatePracticeAsync(EligibilityLookupRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var failures = _validationRules.ValidatePrivatePracticeIdentifiers(request.DoctorId, request.DoctorNationalCode, request.PatientNationalCode);
        if (failures.Count > 0)
            throw new TaminValidationException(failures);

        return _gateway.GetEligibilityAsync(request.RequestBy, request.SiamId, request.DoctorId, request.PatientNationalCode, cancellationToken);
    }
}
