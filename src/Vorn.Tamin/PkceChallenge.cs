using System.Security.Cryptography;
using System.Text;

namespace Vorn.Tamin;

/// <summary>Represents a validated PKCE verifier and its derived SHA-256 challenge.</summary>
public sealed record PkceChallenge
{
    private const int MinimumVerifierLength = 43;
    private const int MaximumVerifierLength = 128;

    private PkceChallenge(string verifier, string challenge)
    {
        Verifier = verifier;
        Challenge = challenge;
    }

    /// <summary>PKCE code verifier.</summary>
    public string Verifier { get; }

    /// <summary>Base64URL-encoded SHA-256 challenge derived from <see cref="Verifier"/>.</summary>
    public string Challenge { get; }

    /// <summary>Provider challenge method for this value object.</summary>
    public string Method => "S256";

    /// <summary>Generates a cryptographically random verifier and matching challenge.</summary>
    public static PkceChallenge Create()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return FromVerifier(Base64UrlEncode(bytes));
    }

    /// <summary>Validates an existing verifier and derives its S256 challenge.</summary>
    public static PkceChallenge FromVerifier(string verifier)
    {
        if (string.IsNullOrWhiteSpace(verifier))
            throw new ArgumentException("PKCE verifier is required.", nameof(verifier));
        if (verifier.Length is < MinimumVerifierLength or > MaximumVerifierLength)
            throw new ArgumentOutOfRangeException(nameof(verifier), verifier.Length, "PKCE verifier length must be between 43 and 128 characters.");
        if (verifier.Any(static c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '.' or '_' or '~')))
            throw new ArgumentException("PKCE verifier contains characters outside RFC 7636 unreserved characters.", nameof(verifier));

        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return new PkceChallenge(verifier, Base64UrlEncode(hash));
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
