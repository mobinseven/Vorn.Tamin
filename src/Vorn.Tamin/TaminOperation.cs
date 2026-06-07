namespace Vorn.Tamin;

/// <summary>Names provider operations used for route lookup and failure context.</summary>
public enum TaminOperation
{
    /// <summary>Build the PKCE authorization redirect URL.</summary>
    Authorize,

    /// <summary>Exchange an authorization code for a token.</summary>
    TokenExchange,

    /// <summary>Refresh an access token through the v2 token endpoint.</summary>
    RefreshTokenV2,

    /// <summary>Sign the user out through the provider sign-out endpoint.</summary>
    SignOut,

    /// <summary>Retrieve provider service reference data.</summary>
    GetServices,

    /// <summary>Submit an electronic prescription.</summary>
    SendPrescription,

    /// <summary>Retrieve a previously registered prescription.</summary>
    GetPrescription,

    /// <summary>Edit a previously registered prescription.</summary>
    EditPrescription,

    /// <summary>Remove a previously registered prescription.</summary>
    RemovePrescription,

    /// <summary>Check prescription warning rules without creating a prescription.</summary>
    CheckPrescriptionWarning
}
