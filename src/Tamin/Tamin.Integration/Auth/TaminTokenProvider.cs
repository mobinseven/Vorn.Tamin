using System.Security.Cryptography;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Tamin.Integration.Auth;

public sealed record DoctorAuthorizationCode(string Code, string ClientId, string Audience, string RedirectUri, string CodeVerifier);
public sealed record DoctorToken(string AccessToken, string? RefreshToken, DateTimeOffset? ExpiresAt);

public interface IDoctorTokenExchange
{
    Task<DoctorToken> ExchangeAuthorizationCodeAsync(DoctorAuthorizationCode grant, CancellationToken cancellationToken);
    Task<DoctorToken> RefreshAsync(string refreshToken, string clientId, string audience, CancellationToken cancellationToken);
}

public sealed class TaminReauthorizationRequiredException(string message, Exception? innerException = null) : Exception(message, innerException);

/// <summary>Owns PKCE validation, grant selection and the single cached doctor token.</summary>
public sealed class TaminTokenProvider : IAccessTokenProvider
{
    private readonly IDoctorTokenExchange exchange;
    private readonly string clientId;
    private readonly string audience;
    private readonly SemaphoreSlim gate = new(1, 1);
    private DoctorToken? token;

    public TaminTokenProvider(IDoctorTokenExchange exchange, string clientId, string audience, IEnumerable<string> allowedHosts)
    {
        this.exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
        this.clientId = string.IsNullOrWhiteSpace(clientId) ? throw new ArgumentException("Client ID is required.", nameof(clientId)) : clientId;
        this.audience = string.IsNullOrWhiteSpace(audience) ? throw new ArgumentException("Audience is required.", nameof(audience)) : audience;
        AllowedHostsValidator = new AllowedHostsValidator(allowedHosts ?? throw new ArgumentNullException(nameof(allowedHosts)));
    }

    public AllowedHostsValidator AllowedHostsValidator { get; }

    // D-04: intentionally generates only the documented RFC 7636 unreserved character set.
    public static string CreateCodeVerifier(int length = 64)
    {
        if (length is < 43 or > 128) throw new ArgumentOutOfRangeException(nameof(length), "PKCE verifier length must be 43 through 128.");
        Span<byte> random = stackalloc byte[length];
        RandomNumberGenerator.Fill(random);
        return string.Create(length, random.ToArray(), static (destination, bytes) =>
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
            for (var i = 0; i < destination.Length; i++) destination[i] = chars[bytes[i] % chars.Length];
        });
    }

    public async Task CompleteAuthorizationAsync(string code, string redirectUri, string codeVerifier, CancellationToken cancellationToken = default)
    {
        ValidateCodeVerifier(codeVerifier);
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Authorization code is required.", nameof(code));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { token = await exchange.ExchangeAuthorizationCodeAsync(new(code, clientId, audience, redirectUri, codeVerifier), cancellationToken).ConfigureAwait(false); }
        finally { gate.Release(); }
    }

    public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        if (!AllowedHostsValidator.IsUrlHostValid(uri)) return string.Empty;
        if (token is { } current && !NeedsRefresh(current)) return current.AccessToken;
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (token is { } cached && !NeedsRefresh(cached)) return cached.AccessToken;
            if (token?.RefreshToken is not { Length: > 0 } refreshToken)
            {
                var cause = new InvalidOperationException("The cached doctor token has no usable refresh token.");
                token = null;
                throw new TaminReauthorizationRequiredException("Doctor authorization must be completed again.", cause);
            }
            try
            {
                var refreshed = await exchange.RefreshAsync(refreshToken, clientId, audience, cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(refreshed.AccessToken))
                    throw new InvalidOperationException("The refresh response had no usable access token.");
                token = refreshed;
                return refreshed.AccessToken;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                token = null;
                throw new TaminReauthorizationRequiredException("Doctor token refresh failed; authorization must be completed again.", exception);
            }
        }
        finally { gate.Release(); }
    }

    // Without an expiry the token cannot safely be considered fresh.
    private static bool NeedsRefresh(DoctorToken value) => value.ExpiresAt is not { } expiry || expiry <= DateTimeOffset.UtcNow.AddMinutes(2);
    private static void ValidateCodeVerifier(string verifier)
    {
        if (verifier.Length is < 43 or > 128 || verifier.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '~' or '-')))
            throw new ArgumentException("PKCE verifier must be 43-128 RFC 7636 unreserved characters.", nameof(verifier));
    }
}
