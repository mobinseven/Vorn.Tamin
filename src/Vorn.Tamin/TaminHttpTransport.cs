using System.Net;

namespace Vorn.Tamin;

/// <summary>Represents the result of one provider HTTP request.</summary>
internal sealed record TaminHttpTransportResponse(HttpStatusCode StatusCode, string? ReasonPhrase, string Content);

/// <summary>Executes provider HTTP requests for application clients.</summary>
internal interface ITaminHttpTransport
{
    Task<TaminHttpTransportResponse> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}

/// <summary>Executes provider HTTP requests with a supplied <see cref="HttpClient"/>.</summary>
internal sealed class TaminHttpClientTransport : ITaminHttpTransport
{
    private readonly HttpClient _httpClient;

    public TaminHttpClientTransport(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<TaminHttpTransportResponse> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var content = response.Content is null
            ? string.Empty
            : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return new TaminHttpTransportResponse(response.StatusCode, response.ReasonPhrase, content);
    }
}
