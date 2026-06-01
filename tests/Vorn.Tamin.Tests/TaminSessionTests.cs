using System.Net;
using System.Text;
using System.Text.Json;
using Vorn.Tamin;

namespace Vorn.Tamin.Tests;

public class TaminSessionTests
{
    [Fact]
    public void Constructor_WhenTokenRequiredWithoutToken_Throws()
    {
        var client = new HttpClient(new StubHandler());

        Assert.Throws<AuthTokenNotSuppliedException>(() => new TaminSession(client));
    }

    [Fact]
    public async Task ServiceClient_UsesExpectedEndpointAndQuery()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"list\":[{\"id\":1}]}}", Encoding.UTF8, "application/json")
            });
        });

        var session = new TaminSession(new HttpClient(handler), "token");
        var result = await session.Service.GetAllServicesAsync(new Dictionary<string, string?> { ["serviceType"] = "17" });

        Assert.NotNull(captured);
        Assert.Equal("https://ep-test.tamin.ir/api/ws-services?serviceType=17", captured!.RequestUri!.ToString());
        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal(1, result[0].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task PrescriptionClient_UsesExpectedEndpointForCreate()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"list\":[{\"ok\":true}]}}", Encoding.UTF8, "application/json")
            });
        });

        var session = new TaminSession(new HttpClient(handler), "token");
        var result = await session.Prescription.CreatePrescriptionAsync(new { patient = "x" });

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("https://ep-test.tamin.ir/api/interface/epresc/SendEpresc", captured.RequestUri!.ToString());
        Assert.True(result[0].GetProperty("ok").GetBoolean());
    }

    [Fact]
    public async Task ServiceClient_On400_ThrowsBadRequest()
    {
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"bad\"}", Encoding.UTF8, "application/json")
        }));

        var session = new TaminSession(new HttpClient(handler), "token");

        await Assert.ThrowsAsync<BadRequest>(() => session.Service.GetAllServicesAsync());
    }

    // ── Common headers ────────────────────────────────────────────────────────

    [Fact]
    public async Task Session_WithClientId_SetsClientIdHeader()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token", clientId: "my-client");
        await session.Service.GetAllServicesAsync();

        Assert.NotNull(captured);
        Assert.True(captured!.Headers.TryGetValues("Client-Id", out _));
    }

    [Fact]
    public async Task Request_AlwaysIncludesRequestIdHeader()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token");
        await session.Service.GetAllServicesAsync();

        Assert.NotNull(captured);
        Assert.True(captured!.Headers.TryGetValues("Request-Id", out var values));
        Assert.True(Guid.TryParse(values.First(), out _));
    }

    // ── New sub-clients ───────────────────────────────────────────────────────

    [Fact]
    public async Task IdentityClient_VerifyIdentity_PostsToExpectedEndpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token");
        await session.Identity.VerifyIdentityAsync(new VerifyIdentityRequest { NationalId = "1234567890" });

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Contains("ws-verify-identity", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task IdentityClient_CheckEntitlement_PostsToExpectedEndpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token");
        await session.Identity.CheckEntitlementAsync(new CheckEntitlementRequest
        {
            NationalId = "1234567890",
            ProviderId = "p1",
            VisitDate = "2024-01-01",
            ServiceType = "clinic"
        });

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Contains("ws-check-entitlement", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task PrescriptionClient_RegisterDrugPrescription_PostsToExpectedEndpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token");
        await session.Prescription.RegisterDrugPrescriptionAsync(new RegisterDrugPrescriptionRequest
        {
            DoctorId = "d1",
            PatientNationalId = "1234567890",
            VisitDate = "2024-01-01",
            DrugItems = [new DrugItem { DrugCode = "DR001", Quantity = 1 }]
        });

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Contains("SendEpresc", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task PrescriptionClient_RegisterPrescriptionOverloads_IncludeTypeDiscriminator()
    {
        var prescriptionTypes = new List<int>();
        var handler = new StubHandler(async (request, _) =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            prescriptionTypes.Add(document.RootElement.GetProperty("prescription_type").GetInt32());
            return OkResponse();
        });

        var session = new TaminSession(new HttpClient(handler), "token");

        await session.Prescription.RegisterVisitPrescriptionAsync(new RegisterVisitPrescriptionRequest
        {
            DoctorId = "D1",
            PatientNationalId = "1234567890",
            VisitDate = "2024-01-01",
            ClinicId = "C1"
        });
        await session.Prescription.RegisterDrugPrescriptionAsync(new RegisterDrugPrescriptionRequest
        {
            DoctorId = "D1",
            PatientNationalId = "1234567890",
            VisitDate = "2024-01-01",
            DrugItems = [new DrugItem { DrugCode = "DR001", Quantity = 1 }]
        });
        await session.Prescription.RegisterParaclinicPrescriptionAsync(new RegisterParaclinicPrescriptionRequest
        {
            DoctorId = "D1",
            PatientNationalId = "1234567890",
            VisitDate = "2024-01-01",
            ServiceItems = [new ServiceItem { ServiceCode = "LAB001", Quantity = 1 }]
        });
        await session.Prescription.RegisterMedicalServicePrescriptionAsync(new RegisterMedicalServicePrescriptionRequest
        {
            DoctorId = "D1",
            PatientNationalId = "1234567890",
            VisitDate = "2024-01-01",
            ServiceItems = [new ServiceItem { ServiceCode = "SVC001", Quantity = 1 }]
        });
        await session.Prescription.RegisterReferralPrescriptionAsync(new RegisterReferralPrescriptionRequest
        {
            DoctorId = "D1",
            PatientNationalId = "1234567890",
            TargetSpecialty = "cardiology",
            TargetProviderType = "clinic",
            Reason = "consult",
            VisitDate = "2024-01-01"
        });
        await session.Prescription.RegisterPhysiotherapyPrescriptionAsync(new RegisterPhysiotherapyPrescriptionRequest
        {
            DoctorId = "D1",
            PatientNationalId = "1234567890",
            PhysiotherapyItems = [new PhysiotherapyItem { ServiceCode = "PHY001" }],
            SessionCount = 5
        });

        Assert.Equal(
            [
                (int)PrescriptionType.Visit,
                (int)PrescriptionType.Drug,
                (int)PrescriptionType.Paraclinic,
                (int)PrescriptionType.Service,
                (int)PrescriptionType.Referral,
                (int)PrescriptionType.Physiotherapy
            ],
            prescriptionTypes);
    }

    [Fact]
    public async Task PrescriptionClient_EditPrescription_PostsToEditEndpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token");
        await session.Prescription.EditElectronicPrescriptionAsync(new EditPrescriptionRequest
        {
            PrescriptionId = "P001",
            TrackingCode = "T001",
            EditedItems = [],
            EditReason = "correction"
        });

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Contains("edit", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task PrescriptionClient_DeletePrescription_PostsToDeleteEndpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token");
        await session.Prescription.DeleteElectronicPrescriptionAsync(new DeletePrescriptionRequest
        {
            PrescriptionId = "P001",
            TrackingCode = "T001",
            DeleteReason = "error"
        });

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Contains("delete", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task PrescriptionClient_GetPrescriptionList_BuildsQueryCorrectly()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token");
        await session.Prescription.GetPrescriptionListAsync(new PrescriptionListFilter
        {
            DoctorId = "D1",
            PatientNationalId = "1234567890",
            PrescriptionType = PrescriptionType.Drug
        });

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured!.Method);
        var uri = captured.RequestUri!.ToString();
        Assert.Contains("doctor_id=D1", uri);
        Assert.Contains("prescription_type=1", uri);
    }

    [Fact]
    public async Task ServiceClient_GetDrugList_BuildsQueryCorrectly()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token");
        await session.Service.GetDrugListAsync(searchText: "aspirin", activeOnly: true);

        Assert.NotNull(captured);
        var uri = captured!.RequestUri!.ToString();
        Assert.Contains("ws-drug-amount", uri);
        Assert.Contains("search_text=aspirin", uri);
        Assert.Contains("active_only=true", uri);
    }

    [Fact]
    public async Task ServiceClient_GetAllowedCount_UsesCorrectEndpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token");
        await session.Service.GetAllowedCountAsync("1234567890", "DR001", "drug", "D1");

        Assert.NotNull(captured);
        var uri = captured!.RequestUri!.ToString();
        Assert.Contains("ws-allowed-count", uri);
        Assert.Contains("item_code=DR001", uri);
    }

    [Fact]
    public async Task ServiceClient_GetPrice_UsesCorrectEndpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token");
        await session.Service.GetPriceAsync("DR001", "drug", 2);

        Assert.NotNull(captured);
        var uri = captured!.RequestUri!.ToString();
        Assert.Contains("ws-price", uri);
        Assert.Contains("item_code=DR001", uri);
        Assert.Contains("quantity=2", uri);
    }

    [Fact]
    public async Task PharmacyClient_DispenseElectronic_UsesCorrectEndpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token");
        await session.Pharmacy.DispenseElectronicPrescriptionAsync(new DispensePrescriptionRequest
        {
            PrescriptionId = "P001",
            TrackingCode = "T001",
            DispensedItems = []
        });

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Contains("darman/dispense-electronic", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task ParaclinicClient_ProvideElectronic_UsesCorrectEndpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token");
        await session.Paraclinic.ProvideElectronicPrescriptionServiceAsync(new ProvideServiceRequest
        {
            PrescriptionId = "P001",
            TrackingCode = "T001",
            DeliveredItems = []
        });

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Contains("paraclinic/provide-electronic", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task PrescriptionClient_CheckWarning_PostsToWarningEndpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token");
        await session.Prescription.CheckPrescriptionWarningAsync(new CheckWarningRequest
        {
            PatientNationalId = "1234567890",
            DoctorId = "D1",
            PrescriptionItems = []
        });

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Contains("warning", captured.RequestUri!.ToString());
    }

    // ── Exception completeness ────────────────────────────────────────────────

    [Fact]
    public void MissingParamException_ContainsParamName()
    {
        var ex = new MissingParamException("nationalId");
        Assert.Contains("nationalId", ex.Message);
    }

    [Fact]
    public void MissingConfigException_ContainsKey()
    {
        var ex = new MissingConfigException("BaseUrl");
        Assert.Contains("BaseUrl", ex.Message);
    }

    [Fact]
    public void InvalidConfigException_ContainsKeyAndReason()
    {
        var ex = new InvalidConfigException("BaseUrl", "must be https");
        Assert.Contains("BaseUrl", ex.Message);
        Assert.Contains("must be https", ex.Message);
    }

    [Fact]
    public void PrescriptionNotCreatedException_StoresErrorCode()
    {
        var ex = new PrescriptionNotCreatedException("failed", "ERR001");
        Assert.Equal("ERR001", ex.ErrorCode);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static HttpResponseMessage OkResponse() =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":{\"list\":[]}}", Encoding.UTF8, "application/json")
        };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? handler = null)
        {
            _handler = handler ?? ((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":{\"list\":[]}}", Encoding.UTF8, "application/json")
            }));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request, cancellationToken);
    }
}

