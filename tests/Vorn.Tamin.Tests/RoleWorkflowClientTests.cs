using System.Net;
using System.Text;
using System.Text.Json;
using Vorn.Tamin;

namespace Vorn.Tamin.Tests;

public sealed class RoleWorkflowClientTests
{
    [Fact]
    public void TaminClient_ExposesRoleAwareWorkflowClients()
    {
        var session = new TaminSession(new HttpClient(new StubHandler()), "token");
        var client = new TaminClient(session);

        Assert.Same(session, client.Session);
        Assert.Same(session.ReferenceData, client.ReferenceData);
        Assert.Same(session.Prescription, client.Prescriptions);
        Assert.Same(session.Doctor.Prescriptions, client.Doctor.Prescriptions);
        Assert.Same(session.Secretary.Eligibility, client.Secretary.Eligibility);
        Assert.Same(session.Nurse, client.Nurse);
    }

    [Fact]
    public async Task DoctorPrescriptionWorkflow_UsesExistingPrescriptionPipeline()
    {
        HttpRequestMessage? captured = null;
        var session = new TaminSession(new HttpClient(new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(JsonResponse());
        })), "token");

        await session.Doctor.Prescriptions.RegisterDrugPrescriptionAsync(new RegisterDrugPrescriptionRequest
        {
            DoctorId = "D1",
            PatientNationalId = "1234567890",
            VisitDate = "14030101",
            DrugItems = [new DrugItem { DrugCode = "DR001", Quantity = 1 }]
        });

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("https://soa.tamin.ir/interface/epresc/SendEpresc/v2", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task SecretaryEligibilityWorkflow_ValidatesThenUsesEligibilityEndpoint()
    {
        HttpRequestMessage? captured = null;
        var session = new TaminSession(new HttpClient(new StubHandler((request, _) =>
        {
            captured = request;
            return Task.FromResult(JsonResponse());
        })), "token");

        JsonElement result = await session.Secretary.Eligibility.LookupPrivatePracticeAsync(new EligibilityLookupRequest
        {
            SiamId = "S1",
            DoctorId = "D1",
            DoctorNationalCode = "1234567890",
            PatientNationalCode = "0987654321"
        });

        Assert.True(result.GetProperty("ok").GetBoolean());
        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Get, captured!.Method);
        Assert.Equal("https://soa.tamin.ir/interface/epresc/patient/v2/deserve-info/S1/D1/0987654321", captured.RequestUri!.ToString());
    }

    [Fact]
    public async Task SecretaryEligibilityWorkflow_InvalidInputDoesNotSendTransport()
    {
        var sent = false;
        var session = new TaminSession(new HttpClient(new StubHandler((_, _) =>
        {
            sent = true;
            return Task.FromResult(JsonResponse());
        })), "token");

        await Assert.ThrowsAsync<TaminValidationException>(() => session.Secretary.Eligibility.LookupPrivatePracticeAsync(new EligibilityLookupRequest
        {
            SiamId = "S1",
            DoctorId = "D1",
            DoctorNationalCode = "123",
            PatientNationalCode = "0987654321"
        }));

        Assert.False(sent);
    }

    [Fact]
    public async Task NurseUnavailableWorkflow_FailsExplicitly()
    {
        var session = new TaminSession(new HttpClient(new StubHandler()), "token");

        var ex = await Assert.ThrowsAsync<TaminWorkflowNotImplementedException>(() => session.Nurse.GetTodoListAsync(new NurseTodoListRequest
        {
            SiamId = "S1",
            PatientNationalCode = "0987654321"
        }));

        Assert.Equal("nurse to-do list", ex.WorkflowName);
    }

    private static HttpResponseMessage JsonResponse()
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"ok\":true}", Encoding.UTF8, "application/json")
        };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? handler = null)
        {
            _handler = handler ?? ((_, _) => Task.FromResult(JsonResponse()));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request, cancellationToken);
    }
}
