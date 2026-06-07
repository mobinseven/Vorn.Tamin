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
        Assert.Equal("https://soa.tamin.ir/interface/epresc/SendEpresc/v2/services?service-type=17", captured!.RequestUri!.ToString());
        Assert.Equal(JsonValueKind.Array, result.ValueKind);
        Assert.Equal(1, result[0].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task PrescriptionClient_UsesExpectedEndpointForRegisterVisit()
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
        var result = await session.Prescription.RegisterVisitPrescriptionAsync(new RegisterVisitPrescriptionRequest
        {
            DoctorId = "D1",
            PatientNationalId = "1234567890",
            VisitDate = "2024-01-01",
            ClinicId = "C1"
        });

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("https://soa.tamin.ir/interface/epresc/SendEpresc/v2", captured.RequestUri!.ToString());
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


    [Fact]
    public async Task Session_WithSandboxEndpoint_UsesSandboxClientEndpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token", endpoint: TaminEndpoint.Sandbox);
        await session.Service.GetAllServicesAsync(new Dictionary<string, string?> { ["serviceType"] = "17" });

        Assert.NotNull(captured);
        Assert.Equal("https://ep-test.tamin.ir/api/v2/ws-services?serviceType=17", captured!.RequestUri!.ToString());
    }

    [Fact]
    public async Task PrescriptionClient_WithSandboxEndpoint_PostsThroughSandboxClientEndpoint()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token", endpoint: TaminEndpoint.Sandbox);
        await session.Prescription.RegisterDrugPrescriptionAsync(new RegisterDrugPrescriptionRequest
        {
            DoctorId = "D1",
            PatientNationalId = "1234567890",
            VisitDate = "2024-01-01",
            DrugItems = []
        });

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("https://ep-test.tamin.ir/api/v2/SendEpresc", captured.RequestUri!.ToString());
    }


    [Fact]
    public async Task PrescriptionClient_WithSandboxEndpoint_GetsPrescriptionWithNationalCodeAndNpi()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token", endpoint: TaminEndpoint.Sandbox);
        await session.Prescription.GetRegisteredPrescriptionAsync(1001, "NAT1", "D1");

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured!.Method);
        Assert.Equal("https://ep-test.tamin.ir/api/v2/ep/1001/NAT1/D1/detail", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task PrescriptionClient_WithSandboxEndpoint_EditsPrescriptionWithNationalCodeAndNpi()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token", endpoint: TaminEndpoint.Sandbox);
        await session.Prescription.EditElectronicPrescriptionAsync(new EditPrescriptionRequest
        {
            HeaderId = 1001,
            DoctorNationalCode = "NAT1",
            DoctorId = "D1",
            EditedItems = []
        });

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("https://ep-test.tamin.ir/api/v2/ep/update/1001/NAT1/D1", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task PrescriptionClient_WithSandboxEndpoint_DeletesPrescriptionWithNationalCodeAndNpi()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token", endpoint: TaminEndpoint.Sandbox);
        await session.Prescription.DeleteElectronicPrescriptionAsync(new DeletePrescriptionRequest
        {
            HeaderId = 1001,
            DoctorNationalCode = "NAT1",
            DoctorId = "D1"
        });

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("https://ep-test.tamin.ir/api/v2/ep/1001/NAT1/D1", captured.RequestUri!.ToString());
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
        Assert.True(captured!.Headers.TryGetValues("Client-Id", out var values));
        Assert.Equal("my-client", values.Single());
    }

    [Fact]
    public async Task Session_DoesNotMutateDefaultRequestHeaders()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });
        var httpClient = new HttpClient(handler);

        var session = new TaminSession(httpClient, "token", clientId: "my-client");
        await session.Service.GetAllServicesAsync();

        Assert.Null(httpClient.DefaultRequestHeaders.Authorization);
        Assert.False(httpClient.DefaultRequestHeaders.Contains("Client-Id"));
        Assert.NotNull(captured);
        Assert.Equal("Bearer", captured!.Headers.Authorization?.Scheme);
        Assert.Equal("token", captured.Headers.Authorization?.Parameter);
        Assert.True(captured.Headers.TryGetValues("Client-Id", out var values));
        Assert.Equal("my-client", values.Single());
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
            prescriptionTypes.Add(document.RootElement.GetProperty("prescType").GetProperty("prescTypeId").GetInt32());
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
            HeaderId = 1001,
            DoctorNationalCode = "NAT1",
            DoctorId = "D1",
            EditedItems = []
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
            HeaderId = 1001,
            DoctorNationalCode = "NAT1",
            DoctorId = "D1"
        });

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Contains("remove", captured.RequestUri!.ToString());
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
        Assert.Contains("interface/epresc/SendEpresc/v2/drug-amount", uri);
        Assert.Contains("search_text=aspirin", uri);
        Assert.Contains("active_only=true", uri);
    }

    [Fact]
    public async Task ServiceClient_GetServiceList_BuildsQueryCorrectly()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token");
        await session.Service.GetServiceListAsync(
            serviceType: "1",
            serviceGroup: "lab",
            searchText: "cbc",
            page: 2,
            pageSize: 25,
            activeOnly: true);

        Assert.NotNull(captured);
        var uri = captured!.RequestUri!.ToString();
        Assert.Contains("interface/epresc/SendEpresc/v2/services", uri);
        Assert.Contains("service-type=1", uri);
        Assert.Contains("service_group=lab", uri);
        Assert.Contains("search_text=cbc", uri);
        Assert.Contains("page=2", uri);
        Assert.Contains("page_size=25", uri);
        Assert.Contains("active_only=true", uri);
    }

    [Fact]
    public async Task ServiceClient_LegacyReferenceData_ForwardsQueryParameters()
    {
        HttpRequestMessage? captured = null;
        var handler = new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(OkResponse());
        });

        var session = new TaminSession(new HttpClient(handler), "token");
        await session.Service.GetDrugInstructionAsync(new Dictionary<string, string?> { ["search_text"] = "daily" });

        Assert.NotNull(captured);
        var uri = captured!.RequestUri!.ToString();
        Assert.Contains("interface/epresc/SendEpresc/v2/drug-instruction", uri);
        Assert.Contains("search_text=daily", uri);
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
        Assert.Contains("check-rules-in-detail", captured.RequestUri!.ToString());
    }


    [Fact]
    public void UnsupportedFriendlyProviderMethods_AreRemoved()
    {
        Assert.Null(typeof(ServiceClient).GetMethod("GetAllowedCountAsync"));
        Assert.Null(typeof(ServiceClient).GetMethod("GetPriceAsync"));
        Assert.DoesNotContain(typeof(IdentityClient).GetMethods(), IsDeclaredPublicInstanceMethod);
        Assert.DoesNotContain(typeof(PharmacyClient).GetMethods(), IsDeclaredPublicInstanceMethod);
        Assert.DoesNotContain(typeof(ParaclinicClient).GetMethods(), IsDeclaredPublicInstanceMethod);
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

    private static bool IsDeclaredPublicInstanceMethod(System.Reflection.MethodInfo method)
        => method.DeclaringType != typeof(object)
            && method.IsPublic
            && !method.IsStatic
            && !method.IsSpecialName;

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

