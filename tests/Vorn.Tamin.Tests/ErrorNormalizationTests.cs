using System.Net;
using System.Text;

namespace Vorn.Tamin.Tests;

public sealed class ErrorNormalizationTests
{
    [Theory]
    [InlineData("{\"message\":\"invalid prescType and srvType pairing\"}", "invalid-prescription-service-pair", TaminErrorCategory.ClientPreventable)]
    [InlineData("{\"message\":\"doctor enrollment is not active\"}", "doctor-enrollment-or-activation", TaminErrorCategory.SupportRequired)]
    [InlineData("{\"message\":\"duplicate prescription already registered\"}", "duplicate-submission", TaminErrorCategory.Retryable)]
    [InlineData("{\"message\":\"id_client differs from client_id in this provider contract\"}", "provider-contract-mismatch", TaminErrorCategory.ProviderContractMismatch)]
    public void TaminErrorNormalizer_MapsKnownProviderFailures(string body, string code, TaminErrorCategory category)
    {
        var normalizer = new TaminErrorNormalizer();

        var error = normalizer.Normalize(
            "SendPrescription",
            TaminEndpoint.Sandbox,
            HttpStatusCode.UnprocessableEntity,
            "Unprocessable Entity",
            body);

        Assert.Equal(code, error.Code);
        Assert.Equal(category, error.Category);
        Assert.Equal("SendPrescription", error.OperationName);
        Assert.Equal(TaminEndpoint.Sandbox, error.Environment);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, error.StatusCode);
        Assert.Equal(body, error.ProviderBody);
    }

    [Fact]
    public void TaminErrorNormalizer_PreservesUnknownProviderFailures()
    {
        var normalizer = new TaminErrorNormalizer();

        var error = normalizer.Normalize(
            "GetServices",
            TaminEndpoint.Production,
            HttpStatusCode.BadGateway,
            "Bad Gateway",
            "unexpected gateway text");

        Assert.Equal("temporary-provider-service", error.Code);
        Assert.Equal(TaminErrorCategory.Retryable, error.Category);
        Assert.Equal("unexpected gateway text", error.ProviderMessage);
    }

    [Fact]
    public async Task GatewayErrors_ExposeNormalizedProviderFailureContext()
    {
        const string body = "{\"message\":\"invalid drugAmntId or drugInstId\"}";
        var session = new TaminSession(
            new HttpClient(new StubHandler((_, _) => Task.FromResult(JsonResponse(HttpStatusCode.UnprocessableEntity, body)))),
            oauthToken: null,
            baseUri: new Uri("https://example.test/"),
            needToken: false,
            endpoint: TaminEndpoint.Sandbox);

        var ex = await Assert.ThrowsAsync<ResourceInvalid>(() => session.Prescription.RegisterDrugPrescriptionAsync(
            new RegisterDrugPrescriptionRequest
            {
                DoctorId = "D-001",
                PatientNationalId = "1234567890",
                VisitDate = "14030601",
                DrugItems = [new DrugItem { DrugCode = "DR001", Quantity = 1 }]
            }));

        Assert.NotNull(ex.ProviderError);
        Assert.Equal("invalid-drug-amount-or-instruction", ex.ProviderError.Code);
        Assert.Equal(TaminErrorCategory.ClientPreventable, ex.ProviderError.Category);
        Assert.Equal("SendPrescription", ex.ProviderError.OperationName);
        Assert.Equal(TaminEndpoint.Sandbox, ex.ProviderError.Environment);
        Assert.Equal(body, ex.ProviderError.ProviderBody);
    }

    [Fact]
    public void ReadmeDocumentsErrorSupportAndCompatibilitySections()
    {
        var readme = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "README.md"));

        Assert.Contains("TaminProviderError", readme);
        Assert.Contains("Provider compatibility notes", readme);
        Assert.Contains("Prevent, normalize, escalate", readme);
        Assert.Contains("id_client", readme);
        Assert.Contains("client_id", readme);
        Assert.Contains("isDentalService", readme);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string content)
        => new(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request, cancellationToken);
    }
}
