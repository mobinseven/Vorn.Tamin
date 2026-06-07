using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Kiota.Abstractions;
using Vorn.Tamin.Kiota.Models;
using SandboxModels = Vorn.Tamin.Kiota.Sandbox.Models;

namespace Vorn.Tamin.Kiota;

/// <summary>Translates friendly SDK operations into generated sandbox Kiota request-builder calls and executes them with common headers.</summary>
internal sealed class TaminKiotaSandboxGateway : ITaminKiotaGateway
{
    private readonly HttpClient _httpClient;
    private readonly string? _clientId;
    private readonly string? _oauthToken;
    private readonly Sandbox.TaminKiotaSandboxClient _client;
    private readonly KiotaBodySerializer _bodySerializer = new();

    public TaminKiotaSandboxGateway(HttpClient httpClient, Uri baseUri, string? oauthToken, string? clientId, TaminKiotaSandboxClientFactory? clientFactory = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentNullException.ThrowIfNull(baseUri);
        _clientId = clientId;
        _oauthToken = oauthToken;

        _client = (clientFactory ?? new TaminKiotaSandboxClientFactory()).Create(_httpClient, baseUri);
    }

    public Task<JsonElement> GetServicesAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken)
    {
        var serviceType = ReadQueryValue(query, "service-type", "service_type", "serviceType");
        var requestInfo = _client.Api.V2.WsServices.ToGetRequestInformation(config =>
        {
            config.QueryParameters.ServiceType = serviceType;
        });
        AddQueryParameters(requestInfo, query, "service-type", "service_type", "serviceType");
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> GetPrescriptionTypesAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken)
    {
        var requestInfo = _client.Api.V2.WsPrescriptionType.ToGetRequestInformation();
        AddQueryParameters(requestInfo, query);
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> GetParaclinicTariffsAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken)
    {
        var requestInfo = _client.Api.V2.WsParTaref.ToGetRequestInformation();
        AddQueryParameters(requestInfo, query);
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> GetDrugAmountsAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken)
    {
        var requestInfo = _client.Api.V2.WsDrugAmoun.ToGetRequestInformation();
        AddQueryParameters(requestInfo, query);
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> GetDrugInstructionsAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken)
    {
        var requestInfo = _client.Api.V2.WsDrugInstruction.ToGetRequestInformation();
        AddQueryParameters(requestInfo, query);
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> SendPrescriptionAsync(SendEprescRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var requestInfo = _client.Api.V2.SendEpresc.ToPostRequestInformation(new SandboxModels.SendEprescRequest());
        ReplaceBody(requestInfo, _bodySerializer.Serialize(request));
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> GetPrescriptionAsync(int headerId, string doctorNationalCode, string doctorId, CancellationToken cancellationToken)
    {
        if (headerId <= 0)
            throw new ArgumentOutOfRangeException(nameof(headerId), headerId, "Prescription header ID must be positive.");
        if (string.IsNullOrWhiteSpace(doctorNationalCode))
            throw new ArgumentException("Doctor national code is required.", nameof(doctorNationalCode));
        if (string.IsNullOrWhiteSpace(doctorId))
            throw new ArgumentException("Doctor ID is required.", nameof(doctorId));

        var requestInfo = _client.Api.V2.Ep[headerId][doctorNationalCode][doctorId].Detail.ToGetRequestInformation();
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> EditPrescriptionAsync(int headerId, string doctorNationalCode, string doctorId, IReadOnlyList<NoteDetailEprsc> details, CancellationToken cancellationToken)
    {
        if (headerId <= 0)
            throw new ArgumentOutOfRangeException(nameof(headerId), headerId, "Prescription header ID must be positive.");
        if (string.IsNullOrWhiteSpace(doctorNationalCode))
            throw new ArgumentException("Doctor national code is required.", nameof(doctorNationalCode));
        if (string.IsNullOrWhiteSpace(doctorId))
            throw new ArgumentException("Doctor ID is required.", nameof(doctorId));
        ArgumentNullException.ThrowIfNull(details);

        var requestInfo = _client.Api.V2.Ep.Update[headerId][doctorNationalCode][doctorId].ToPostRequestInformation([]);
        ReplaceBody(requestInfo, _bodySerializer.SerializeCollection(details));
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> RemovePrescriptionAsync(int headerId, string doctorNationalCode, string doctorId, CancellationToken cancellationToken)
    {
        if (headerId <= 0)
            throw new ArgumentOutOfRangeException(nameof(headerId), headerId, "Prescription header ID must be positive.");
        if (string.IsNullOrWhiteSpace(doctorNationalCode))
            throw new ArgumentException("Doctor national code is required.", nameof(doctorNationalCode));
        if (string.IsNullOrWhiteSpace(doctorId))
            throw new ArgumentException("Doctor ID is required.", nameof(doctorId));

        var requestInfo = _client.Api.V2.Ep[headerId][doctorNationalCode][doctorId].ToPostRequestInformation();
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> CheckPrescriptionWarningAsync(DentistRuleRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var requestInfo = _client.Api.V2.CheckRulesInDetail.ToPostRequestInformation(new SandboxModels.DentistRuleRequest());
        ReplaceBody(requestInfo, _bodySerializer.Serialize(request));
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> GetEligibilityAsync(string requestBy, string siamId, string doctorId, string patientNationalCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestBy, nameof(requestBy));
        ArgumentException.ThrowIfNullOrWhiteSpace(siamId, nameof(siamId));
        ArgumentException.ThrowIfNullOrWhiteSpace(doctorId, nameof(doctorId));
        ArgumentException.ThrowIfNullOrWhiteSpace(patientNationalCode, nameof(patientNationalCode));

        var requestInfo = _client.Api.V2.Patients.DeserveInfo[requestBy][siamId][doctorId][patientNationalCode].ToGetRequestInformation();
        return SendAsync(requestInfo, cancellationToken);
    }

    private static void ReplaceBody(RequestInformation requestInfo, Stream content)
    {
        requestInfo.Content = content;
        requestInfo.Headers.Remove("Content-Type");
        requestInfo.Headers.TryAdd("Content-Type", "application/json");
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
            if (requestInfo.Headers.TryGetValue("Content-Type", out var contentTypes)
                && contentTypes.FirstOrDefault() is { Length: > 0 } contentType)
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            else
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var content = response.Content is not null
            ? await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)
            : string.Empty;
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
        if (uri is null)
            return;

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
