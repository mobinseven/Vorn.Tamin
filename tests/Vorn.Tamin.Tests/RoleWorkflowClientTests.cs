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
    public async Task NurseTodoWorkflow_UsesGateway()
    {
        var session = new TaminSession(new HttpClient(new StubHandler()), "token");

        var result = await session.Nurse.GetTodoListAsync(new NurseTodoListRequest
        {
            SiamId = "S1",
            NurseNationalCode = "0987654321"
        });

        Assert.True(result.GetProperty("ok").GetBoolean());
    }

    [Fact]
    public async Task HospitalizationCreateWorkflow_UsesGateway()
    {
        var session = new TaminSession(new HttpClient(new StubHandler()), "token");

        var result = await session.Hospitalization.CreateAsync(new HospitalizationCreateRequest
        {
            DoctorId = "D1",
            PatientNationalId = "1234567890",
            PrescriptionDate = "14030101",
            SiamId = "S1",
            ReferralDetails =
            [
                new HospitalizationReferralDetail
                {
                    PatientNationalCode = "1234567890",
                    ReferralHijriDate = "14030101",
                    SiamId = "S1",
                    Icd10Items = [new HospitalizationIcd10Item { IcdCode = "A00" }]
                }
            ]
        });

        Assert.True(result.GetProperty("ok").GetBoolean());
    }

    [Fact]
    public async Task HospitalizationCreateWorkflow_InvalidNestedInputDoesNotSendTransport()
    {
        var sent = false;
        var session = new TaminSession(new HttpClient(new StubHandler((_, _) =>
        {
            sent = true;
            return Task.FromResult(JsonResponse());
        })), "token");

        var ex = await Assert.ThrowsAsync<TaminValidationException>(() => session.Hospitalization.CreateAsync(new HospitalizationCreateRequest
        {
            DoctorId = "D1",
            PatientNationalId = "1234567890",
            PrescriptionDate = "14030101",
            SiamId = "S1",
            ReferralDetails =
            [
                null!,
                new HospitalizationReferralDetail
                {
                    PatientNationalCode = "123",
                    ReferralHijriDate = "14030101",
                    SiamId = "S1",
                    Icd10Items = [null!]
                }
            ]
        }));

        Assert.False(sent);
        Assert.Contains(ex.Failures, failure => failure.Field == "note_details_referral_list[0]" && failure.Code == "required");
        Assert.Contains(ex.Failures, failure => failure.Field == "note_details_referral_list[1].patient_national_code" && failure.Code == "national-code-shape");
        Assert.Contains(ex.Failures, failure => failure.Field == "note_details_referral_list[1].icd10s[0]" && failure.Code == "required");
    }

    [Fact]
    public async Task ReferralCountsWorkflow_UsesGateway()
    {
        var session = new TaminSession(new HttpClient(new StubHandler()), "token");

        var result = await session.Referrals.GetCountsAsync(new ReferralCountRequest
        {
            PatientNationalCode = "1234567890",
            DoctorId = "D1"
        });

        Assert.True(result.GetProperty("ok").GetBoolean());
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
