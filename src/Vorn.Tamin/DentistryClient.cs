using System.Text.Json;

namespace Vorn.Tamin;

/// <summary>Orchestrates dental rule checks without owning dental validation or transport details.</summary>
public sealed class DentistryClient
{
    private readonly PrescriptionClient _prescriptions;

    internal DentistryClient(PrescriptionClient prescriptions)
    {
        _prescriptions = prescriptions ?? throw new ArgumentNullException(nameof(prescriptions));
    }

    /// <summary>Returns dental prescription warnings.</summary>
    public Task<JsonElement> CheckRulesAsync(CheckWarningRequest request, CancellationToken cancellationToken = default)
        => _prescriptions.CheckPrescriptionWarningAsync(request, cancellationToken);
}
