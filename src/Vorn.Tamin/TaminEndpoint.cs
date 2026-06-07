namespace Vorn.Tamin;

/// <summary>Identifies which generated EP.Tamin endpoint surface a session uses.</summary>
public enum TaminEndpoint
{
    /// <summary>Use the production EP.Tamin generated client and production base URL.</summary>
    Production = 0,

    /// <summary>Use the sandbox EP.Tamin generated client and sandbox base URL.</summary>
    Sandbox = 1
}
