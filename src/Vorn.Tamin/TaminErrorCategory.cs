namespace Vorn.Tamin;

/// <summary>Classifies a normalized EP.Tamin provider failure by the remediation path callers should take.</summary>
public enum TaminErrorCategory
{
    /// <summary>The provider failure did not match a known SDK catalog entry.</summary>
    UnknownProviderError = 0,

    /// <summary>The caller can normally prevent this failure by fixing request data before sending it.</summary>
    ClientPreventable = 1,

    /// <summary>The failure usually requires provider onboarding, enrollment, or support-team intervention.</summary>
    SupportRequired = 2,

    /// <summary>The failure is likely temporary; retry only according to an application retry policy.</summary>
    Retryable = 3,

    /// <summary>The provider response conflicts with the documented API contract or known specification notes.</summary>
    ProviderContractMismatch = 4
}
