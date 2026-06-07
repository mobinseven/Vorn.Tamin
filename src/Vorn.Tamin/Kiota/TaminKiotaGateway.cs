using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Kiota.Abstractions;
using Vorn.Tamin.Kiota.Models;

namespace Vorn.Tamin.Kiota;

/// <summary>Translates friendly SDK operations into generated Kiota request-builder calls and executes them with common headers.</summary>
internal sealed class TaminKiotaGateway : ITaminKiotaGateway
{
    private readonly HttpClient _httpClient;
    private readonly string? _clientId;
    private readonly string? _oauthToken;
    private readonly TaminKiotaClient _client;

    public TaminKiotaGateway(HttpClient httpClient, Uri baseUri, string? oauthToken, string? clientId, TaminKiotaClientFactory? clientFactory = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentNullException.ThrowIfNull(baseUri);
        _clientId = clientId;
        _oauthToken = oauthToken;

        _client = (clientFactory ?? new TaminKiotaClientFactory()).Create(_httpClient, baseUri);
    }

    public Task<JsonElement> GetServicesAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken)
    {
        var serviceType = ReadQueryValue(query, "service-type", "service_type", "serviceType");
        var requestInfo = _client.Interface.Epresc.SendEpresc.V2.Services.ToGetRequestInformation(config =>
        {
            config.QueryParameters.ServiceType = serviceType;
        });
        AddQueryParameters(requestInfo, query, "service-type", "service_type", "serviceType");
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> GetPrescriptionTypesAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken)
    {
        var requestInfo = _client.Interface.Epresc.SendEpresc.V2.PrescriptionType.ToGetRequestInformation();
        AddQueryParameters(requestInfo, query);
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> GetParaclinicTariffsAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken)
    {
        var requestInfo = _client.Interface.Epresc.SendEpresc.V2.ParTaref.ToGetRequestInformation();
        AddQueryParameters(requestInfo, query);
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> GetDrugAmountsAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken)
    {
        var requestInfo = _client.Interface.Epresc.SendEpresc.V2.DrugAmount.ToGetRequestInformation();
        AddQueryParameters(requestInfo, query);
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> GetDrugInstructionsAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken)
    {
        var requestInfo = _client.Interface.Epresc.SendEpresc.V2.DrugInstruction.ToGetRequestInformation();
        AddQueryParameters(requestInfo, query);
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> SendPrescriptionAsync(SendEprescRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var requestInfo = _client.Interface.Epresc.SendEpresc.V2.ToPostRequestInformation(request);
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> GetPrescriptionAsync(int headerId, string doctorNationalCode, string doctorId, CancellationToken cancellationToken)
    {
        if (headerId <= 0)
            throw new ArgumentOutOfRangeException(nameof(headerId), headerId, "Prescription header ID must be positive.");
        if (string.IsNullOrWhiteSpace(doctorId))
            throw new ArgumentException("Doctor ID is required.", nameof(doctorId));

        var requestInfo = _client.Interface.Epresc.SendEpresc.V2[headerId][doctorId].ToGetRequestInformation();
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> EditPrescriptionAsync(int headerId, string doctorNationalCode, string doctorId, IReadOnlyList<NoteDetailEprsc> details, CancellationToken cancellationToken)
    {
        if (headerId <= 0)
            throw new ArgumentOutOfRangeException(nameof(headerId), headerId, "Prescription header ID must be positive.");
        if (string.IsNullOrWhiteSpace(doctorId))
            throw new ArgumentException("Doctor ID is required.", nameof(doctorId));
        ArgumentNullException.ThrowIfNull(details);

        var requestInfo = _client.Interface.Epresc.SendEpresc.V2.Edit[headerId][doctorId].ToPostRequestInformation(details.ToList());
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> RemovePrescriptionAsync(int headerId, string doctorNationalCode, string doctorId, CancellationToken cancellationToken)
    {
        if (headerId <= 0)
            throw new ArgumentOutOfRangeException(nameof(headerId), headerId, "Prescription header ID must be positive.");
        if (string.IsNullOrWhiteSpace(doctorId))
            throw new ArgumentException("Doctor ID is required.", nameof(doctorId));

        var requestInfo = _client.Interface.Epresc.SendEpresc.V2.Remove[headerId][doctorId].ToPostRequestInformation();
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> CheckPrescriptionWarningAsync(DentistRuleRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var requestInfo = _client.Interface.Epresc.SendEpresc.V2.CheckRulesInDetail.ToPostRequestInformation(request);
        return SendAsync(requestInfo, cancellationToken);
    }

    private async Task<JsonElement> SendAsync(RequestInformation requestInfo, CancellationToken cancellationToken)
    {
        AddCommonHeaders(requestInfo);

        using var request = new HttpRequestMessage(ToHttpMethod(requestInfo.HttpMethod), requestInfo.URI);
        foreach (var header in requestInfo.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (requestInfo.Content is not null)
        {
            requestInfo.Content.Position = 0;
            request.Content = new StreamContent(requestInfo.Content);
            if (requestInfo.Headers.TryGetValue("Content-Type", out var contentTypes))
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentTypes.First());
            else
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ResponseHandling.Handle(response.StatusCode, response.ReasonPhrase, content);
    }

    private void AddCommonHeaders(RequestInformation requestInfo)
    {
        requestInfo.Headers.TryAdd("Request-Id", Guid.NewGuid().ToString());
        if (!string.IsNullOrWhiteSpace(_clientId))
            requestInfo.Headers.TryAdd("Client-Id", _clientId);
        if (!string.IsNullOrWhiteSpace(_oauthToken))
            requestInfo.Headers.TryAdd("Authorization", $"Bearer {_oauthToken}");
    }

    private static void AddQueryParameters(RequestInformation requestInfo, IReadOnlyDictionary<string, string?>? query, params string[] excludedKeys)
    {
        if (query is null || query.Count == 0)
            return;

        var excluded = excludedKeys.Length == 0
            ? null
            : new HashSet<string>(excludedKeys, StringComparer.OrdinalIgnoreCase);

        var queryString = string.Join("&", query
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key)
                && !string.IsNullOrWhiteSpace(kvp.Value)
                && excluded?.Contains(kvp.Key) != true)
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}"));

        if (string.IsNullOrEmpty(queryString))
            return;

        var uri = requestInfo.URI;
        var separator = uri.Query.Length == 0 ? "?" : "&";
        requestInfo.URI = new Uri($"{uri}{separator}{queryString}");
    }

    private static string? ReadQueryValue(IReadOnlyDictionary<string, string?>? query, params string[] keys)
    {
        if (query is null)
            return null;

        foreach (var key in keys)
        {
            if (query.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static HttpMethod ToHttpMethod(Method method)
        => method switch
        {
            Method.GET => HttpMethod.Get,
            Method.POST => HttpMethod.Post,
            Method.PUT => HttpMethod.Put,
            Method.PATCH => HttpMethod.Patch,
            Method.DELETE => HttpMethod.Delete,
            _ => throw new NotSupportedException($"HTTP method '{method}' is not supported by the friendly Kiota gateway.")
        };
}
