using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Kiota.Abstractions;
using Vorn.Tamin.Mapping;

namespace Vorn.Tamin.Kiota;

/// <summary>Translates friendly SDK operations into generated Kiota request-builder calls and executes them with common headers.</summary>
internal sealed class TaminKiotaGateway : ITaminKiotaGateway
{
    private readonly HttpClient _httpClient;
    private readonly Uri _baseUri;
    private readonly string? _clientId;
    private readonly TaminOpenAPIClient _client;

    public TaminKiotaGateway(HttpClient httpClient, Uri baseUri, string? oauthToken, string? clientId, TaminKiotaClientFactory? clientFactory = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
        _clientId = clientId;

        if (!string.IsNullOrWhiteSpace(oauthToken))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oauthToken);

        if (!string.IsNullOrWhiteSpace(clientId))
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Client-Id", clientId);

        _client = (clientFactory ?? new TaminKiotaClientFactory()).Create(_httpClient, _baseUri);
    }

    public Task<JsonElement> GetServicesAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken)
    {
        var serviceType = ReadQueryValue(query, "service-type", "service_type", "serviceType");
        var requestInfo = _client.Interface.Epresc.SendEpresc.V2.Services.ToGetRequestInformation(config =>
        {
            config.QueryParameters.ServiceType = serviceType;
        });
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> GetPrescriptionTypesAsync(CancellationToken cancellationToken)
    {
        var requestInfo = _client.Interface.Epresc.SendEpresc.V2.PrescriptionType.ToGetRequestInformation();
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> GetParaclinicTariffsAsync(CancellationToken cancellationToken)
    {
        var requestInfo = _client.Interface.Epresc.SendEpresc.V2.ParTaref.ToGetRequestInformation();
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> GetDrugAmountsAsync(CancellationToken cancellationToken)
    {
        var requestInfo = _client.Interface.Epresc.SendEpresc.V2.DrugAmount.ToGetRequestInformation();
        return SendAsync(requestInfo, cancellationToken);
    }

    public Task<JsonElement> GetDrugInstructionsAsync(CancellationToken cancellationToken)
    {
        var requestInfo = _client.Interface.Epresc.SendEpresc.V2.DrugInstruction.ToGetRequestInformation();
        return SendAsync(requestInfo, cancellationToken);
    }


    public Task<JsonElement> GetAllowedCountAsync(IReadOnlyDictionary<string, string?> query, CancellationToken cancellationToken)
        => GetAsync(TaminGatewayRoute.AllowedCount, query, cancellationToken);

    public Task<JsonElement> GetPriceAsync(IReadOnlyDictionary<string, string?> query, CancellationToken cancellationToken)
        => GetAsync(TaminGatewayRoute.Price, query, cancellationToken);

    public Task<JsonElement> SendPrescriptionAsync<TPayload>(TPayload payload, CancellationToken cancellationToken)
    {
        var request = TaminRequestMapper.ToSendEprescRequest(payload);
        var requestInfo = _client.Interface.Epresc.SendEpresc.V2.ToPostRequestInformation(request);
        return SendAsync(requestInfo, cancellationToken);
    }


    public Task<JsonElement> GetPrescriptionAsync(IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken)
        => GetAsync(TaminGatewayRoute.PrescriptionDetail, query, cancellationToken);

    public Task<JsonElement> EditPrescriptionAsync(EditPrescriptionRequest request, CancellationToken cancellationToken)
        => PostAsync(TaminGatewayRoute.PrescriptionEdit, request, cancellationToken);

    public Task<JsonElement> RemovePrescriptionAsync(DeletePrescriptionRequest request, CancellationToken cancellationToken)
        => PostAsync(TaminGatewayRoute.PrescriptionRemove, request, cancellationToken);

    public Task<JsonElement> CheckPrescriptionWarningAsync(CheckWarningRequest request, CancellationToken cancellationToken)
        => PostAsync(TaminGatewayRoute.PrescriptionWarning, request, cancellationToken);

    public Task<JsonElement> GetAsync(TaminGatewayRoute route, IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken)
        => GetEndpointAsync(ResolveEndpoint(route), query, cancellationToken);

    public Task<JsonElement> PostAsync<TPayload>(TaminGatewayRoute route, TPayload payload, CancellationToken cancellationToken)
        => PostEndpointAsync(ResolveEndpoint(route), payload, cancellationToken);

    private Task<JsonElement> GetEndpointAsync(string endpoint, IReadOnlyDictionary<string, string?>? query, CancellationToken cancellationToken)
    {
        var requestInfo = CreateRequestInformation<object>(Method.GET, endpoint, query, payload: null);
        return SendAsync(requestInfo, cancellationToken);
    }

    private Task<JsonElement> PostEndpointAsync<TPayload>(string endpoint, TPayload payload, CancellationToken cancellationToken)
    {
        if (payload is null)
            throw new ArgumentNullException(nameof(payload));

        var requestInfo = CreateRequestInformation(Method.POST, endpoint, query: null, payload);
        return SendAsync(requestInfo, cancellationToken);
    }

    private static string ResolveEndpoint(TaminGatewayRoute route)
        => route switch
        {
            TaminGatewayRoute.VerifyIdentity => "ws-verify-identity",
            TaminGatewayRoute.CheckEntitlement => "ws-check-entitlement",
            TaminGatewayRoute.AllowedCount => "ws-allowed-count",
            TaminGatewayRoute.Price => "ws-price",
            TaminGatewayRoute.PrescriptionDetail => "interface/epresc/SendEpresc/v2",
            TaminGatewayRoute.PrescriptionEdit => "interface/epresc/SendEpresc/v2/edit",
            TaminGatewayRoute.PrescriptionRemove => "interface/epresc/SendEpresc/v2/remove",
            TaminGatewayRoute.PrescriptionWarning => "interface/epresc/SendEpresc/v2/check-rules-in-detail",
            TaminGatewayRoute.PharmacyCheckEntitlement => "darman/check-entitlement",
            TaminGatewayRoute.PharmacyRegisterPaper => "darman/register-paper",
            TaminGatewayRoute.PharmacyPrescriptionList => "darman/prescription-list",
            TaminGatewayRoute.PharmacyPrescriptionDetails => "darman/prescription-details",
            TaminGatewayRoute.PharmacyReferToDoctor => "darman/refer-to-doctor",
            TaminGatewayRoute.PharmacyCheckWarnings => "darman/check-warnings",
            TaminGatewayRoute.PharmacyDispensePaper => "darman/dispense-paper",
            TaminGatewayRoute.PharmacyDispenseElectronic => "darman/dispense-electronic",
            TaminGatewayRoute.PharmacyDispenseWithWarning => "darman/dispense-with-warning",
            TaminGatewayRoute.PharmacyRegisterAuthenticityCode => "darman/register-authenticity-code",
            TaminGatewayRoute.PharmacyActivateAuthenticityCode => "darman/activate-authenticity-code",
            TaminGatewayRoute.PharmacyTwoStepDispense => "darman/two-step-dispense",
            TaminGatewayRoute.PharmacyActivatedBarcode => "darman/activated-barcode",
            TaminGatewayRoute.PharmacyPrice => "darman/price",
            TaminGatewayRoute.PharmacyDeleteDispensing => "darman/delete-dispensing",
            TaminGatewayRoute.ParaclinicCheckEntitlement => "paraclinic/check-entitlement",
            TaminGatewayRoute.ParaclinicRegisterPaper => "paraclinic/register-paper",
            TaminGatewayRoute.ParaclinicPrescriptionList => "paraclinic/prescription-list",
            TaminGatewayRoute.ParaclinicPrescriptionDetails => "paraclinic/prescription-details",
            TaminGatewayRoute.ParaclinicProvidePaper => "paraclinic/provide-paper",
            TaminGatewayRoute.ParaclinicProvideElectronic => "paraclinic/provide-electronic",
            TaminGatewayRoute.ParaclinicProvideWithWarning => "paraclinic/provide-with-warning",
            TaminGatewayRoute.ParaclinicPrice => "paraclinic/price",
            TaminGatewayRoute.ParaclinicDeleteDelivery => "paraclinic/delete-delivery",
            _ => throw new ArgumentOutOfRangeException(nameof(route), route, "Unknown Tamin gateway route.")
        };

    private RequestInformation CreateRequestInformation<TPayload>(
        Method method,
        string endpoint,
        IReadOnlyDictionary<string, string?>? query,
        TPayload? payload)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint is required for operations that are not represented by the generated Kiota client.", nameof(endpoint));

        var requestInfo = new RequestInformation
        {
            HttpMethod = method,
            URI = BuildUri(endpoint, query)
        };
        requestInfo.Headers.TryAdd("Accept", "application/json");

        if (payload is not null)
        {
            requestInfo.Headers.TryAdd("Content-Type", "application/json");
            requestInfo.Content = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        }

        return requestInfo;
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
    }

    private Uri BuildUri(string endpoint, IReadOnlyDictionary<string, string?>? query)
    {
        var absolute = new Uri(_baseUri, endpoint.TrimStart('/'));
        if (query is null || query.Count == 0)
            return absolute;

        var queryString = string.Join("&", query
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}"));

        if (string.IsNullOrEmpty(queryString))
            return absolute;

        var separator = absolute.Query.Length == 0 ? "?" : "&";
        return new Uri($"{absolute}{separator}{queryString}");
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
