using System.Net;
using System.Text.Json;

namespace Vorn.Tamin;

/// <summary>Translates HTTP responses into <see cref="JsonElement"/> results or typed exceptions.</summary>
internal static class ResponseHandling
{
    /// <summary>
    /// Parses <paramref name="content"/> and returns the unwrapped <c>data</c> (or <c>data.list</c>) element,
    /// or throws a typed exception for non-2xx responses.
    /// </summary>
    public static JsonElement Handle(HttpStatusCode statusCode, string? reasonPhrase, string content)
    {
        var status = (int)statusCode;
        if (status is 301 or 302 or 303 or 307)
            throw new Redirection(statusCode, reasonPhrase, content);

        if (status is >= 200 and <= 299)
        {
            if (string.IsNullOrWhiteSpace(content))
                return default;

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("list", out var list))
                    return list.Clone();

                return data.Clone();
            }

            return doc.RootElement.Clone();
        }

        if (status == 400) throw new BadRequest(statusCode, reasonPhrase, content);
        if (status == 401) throw new UnauthorizedAccess(statusCode, reasonPhrase, content);
        if (status == 403) throw new ForbiddenAccess(statusCode, reasonPhrase, content);
        if (status == 404) throw new ResourceNotFound(statusCode, reasonPhrase, content);
        if (status == 405) throw new MethodNotAllowed(statusCode, reasonPhrase, content);
        if (status == 409) throw new ResourceConflict(statusCode, reasonPhrase, content);
        if (status == 410) throw new ResourceGone(statusCode, reasonPhrase, content);
        if (status == 422) throw new ResourceInvalid(statusCode, reasonPhrase, content);
        if (status is >= 402 and <= 499) throw new ClientError(statusCode, reasonPhrase, content);
        if (status is >= 500 and <= 599) throw new ServerError(statusCode, reasonPhrase, content);

        throw new ConnectionError(statusCode, reasonPhrase, content, "Unknown response code");
    }
}
