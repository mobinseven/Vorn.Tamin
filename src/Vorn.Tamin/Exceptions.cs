using System.Net;

namespace Vorn.Tamin;

/// <summary>Thrown when an authorization token was required but not supplied.</summary>
public class AuthTokenNotSuppliedException : Exception
{
    /// <inheritdoc />
    public AuthTokenNotSuppliedException() : base("Authorization token not supplied") { }
}

/// <summary>Thrown when the API rejects the supplied credentials.</summary>
public class UserLoginException : Exception
{
    /// <summary>API status code from the response body.</summary>
    public int? Status { get; }
    /// <summary>Error family from the response body.</summary>
    public string? Family { get; }
    /// <summary>Reason text from the response body.</summary>
    public string? ReasonText { get; }

    /// <inheritdoc />
    public UserLoginException(string? data, int? status, string? family, string? reason)
        : base(data)
    {
        Status = status;
        Family = family;
        ReasonText = reason;
    }
}

/// <summary>Thrown when a prescription could not be created.</summary>
public class PrescriptionNotCreatedException : Exception
{
    /// <summary>Application-level error code returned by the API.</summary>
    public string? ErrorCode { get; }

    /// <inheritdoc />
    public PrescriptionNotCreatedException(string? message, string? errorCode = null)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>Thrown when a required parameter is missing.</summary>
public class MissingParamException : ArgumentException
{
    /// <inheritdoc />
    public MissingParamException(string paramName) : base($"Required parameter '{paramName}' is missing.", paramName) { }
}

/// <summary>Thrown when a required configuration value is missing.</summary>
public class MissingConfigException : Exception
{
    /// <inheritdoc />
    public MissingConfigException(string key) : base($"Required configuration key '{key}' is missing.") { }
}

/// <summary>Thrown when a configuration value is invalid.</summary>
public class InvalidConfigException : Exception
{
    /// <inheritdoc />
    public InvalidConfigException(string key, string reason) : base($"Configuration key '{key}' is invalid: {reason}") { }
}

// ── HTTP connection errors ──────────────────────────────────────────────────

/// <summary>Base class for all HTTP-level connection errors.</summary>
public class ConnectionError : Exception
{
    /// <summary>HTTP status code of the failed response.</summary>
    public HttpStatusCode? StatusCode { get; }
    /// <summary>Reason phrase of the failed response.</summary>
    public string? ReasonPhrase { get; }
    /// <summary>Raw response body.</summary>
    public string? Content { get; }

    /// <inheritdoc />
    public ConnectionError(HttpStatusCode? statusCode, string? reasonPhrase, string? content, string? message = null)
        : base(message ?? "Failed.")
    {
        StatusCode = statusCode;
        ReasonPhrase = reasonPhrase;
        Content = content;
    }
}

/// <summary>3xx redirection response.</summary>
public class Redirection : ConnectionError { public Redirection(HttpStatusCode? s, string? r, string? c) : base(s, r, c) { } }
/// <summary>4xx client error response.</summary>
public class ClientError : ConnectionError { public ClientError(HttpStatusCode? s, string? r, string? c) : base(s, r, c) { } }
/// <summary>400 Bad Request.</summary>
public class BadRequest : ClientError { public BadRequest(HttpStatusCode? s, string? r, string? c) : base(s, r, c) { } }
/// <summary>401 Unauthorized.</summary>
public class UnauthorizedAccess : ClientError { public UnauthorizedAccess(HttpStatusCode? s, string? r, string? c) : base(s, r, c) { } }
/// <summary>403 Forbidden.</summary>
public class ForbiddenAccess : ClientError { public ForbiddenAccess(HttpStatusCode? s, string? r, string? c) : base(s, r, c) { } }
/// <summary>404 Not Found.</summary>
public class ResourceNotFound : ClientError { public ResourceNotFound(HttpStatusCode? s, string? r, string? c) : base(s, r, c) { } }
/// <summary>405 Method Not Allowed.</summary>
public class MethodNotAllowed : ClientError { public MethodNotAllowed(HttpStatusCode? s, string? r, string? c) : base(s, r, c) { } }
/// <summary>409 Conflict.</summary>
public class ResourceConflict : ClientError { public ResourceConflict(HttpStatusCode? s, string? r, string? c) : base(s, r, c) { } }
/// <summary>410 Gone.</summary>
public class ResourceGone : ClientError { public ResourceGone(HttpStatusCode? s, string? r, string? c) : base(s, r, c) { } }
/// <summary>422 Unprocessable Entity.</summary>
public class ResourceInvalid : ClientError { public ResourceInvalid(HttpStatusCode? s, string? r, string? c) : base(s, r, c) { } }
/// <summary>5xx server error response.</summary>
public class ServerError : ConnectionError { public ServerError(HttpStatusCode? s, string? r, string? c) : base(s, r, c) { } }

// ── Domain / business error categories (Section 16 of EP-TAMIN-API.md) ─────

/// <summary>Invalid credentials, expired token, or missing OTP.</summary>
public class AuthenticationError : Exception
{
    /// <inheritdoc />
    public AuthenticationError(string? message = null, Exception? inner = null)
        : base(message ?? "Authentication failed.", inner) { }
}

/// <summary>Provider or user is not authorized to perform the requested operation.</summary>
public class AuthorizationError : Exception
{
    /// <inheritdoc />
    public AuthorizationError(string? message = null, Exception? inner = null)
        : base(message ?? "Authorization failed.", inner) { }
}

/// <summary>Patient identity could not be verified.</summary>
public class IdentityError : Exception
{
    /// <inheritdoc />
    public IdentityError(string? message = null, Exception? inner = null)
        : base(message ?? "Identity verification failed.", inner) { }
}

/// <summary>Patient does not have valid treatment entitlement.</summary>
public class EntitlementError : Exception
{
    /// <inheritdoc />
    public EntitlementError(string? message = null, Exception? inner = null)
        : base(message ?? "Treatment entitlement check failed.", inner) { }
}

/// <summary>Required field missing, invalid code, invalid quantity, or invalid date.</summary>
public class ValidationError : Exception
{
    /// <inheritdoc />
    public ValidationError(string? message = null, Exception? inner = null)
        : base(message ?? "Validation failed.", inner) { }
}

/// <summary>Prescription violates an insurance rule or medical service rule.</summary>
public class BusinessRuleError : Exception
{
    /// <inheritdoc />
    public BusinessRuleError(string? message = null, Exception? inner = null)
        : base(message ?? "Business rule violation.", inner) { }
}

/// <summary>Timeout or retry may have already created the prescription — check status before retrying.</summary>
public class DuplicateSubmissionRisk : Exception
{
    /// <inheritdoc />
    public DuplicateSubmissionRisk(string? message = null, Exception? inner = null)
        : base(message ?? "Duplicate submission risk detected.", inner) { }
}

/// <summary>External service is temporarily unavailable.</summary>
public class TemporaryServiceError : Exception
{
    /// <inheritdoc />
    public TemporaryServiceError(string? message = null, Exception? inner = null)
        : base(message ?? "External service temporarily unavailable.", inner) { }
}

