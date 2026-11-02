using System.Net;
using System.Text;
using System.Text.Json;
using Tamin.Integration.Http;
using Tamin.Integration.Mapping;

namespace Tamin.Integration.Tests;

/// <summary>Drives the public facade over a stub transport and compares each request with the routes transcribed from the guide.</summary>
public sealed class TaminClientFacadeTests
{
    private const string Siam = "SIAM1";
    private const string DocId = "0033333333";
    private const string DoctorNational = "0011111111";
    private const string PatientNational = "0022222222";
    private const string Secretary = "0044444444";
    private const long Header = 3_000_000_001;
    private const int Tracking = 55;

    // Every value a guide route can name. Leading zeros and the value above int.MaxValue must survive into the URL.
    private static readonly Dictionary<string, string> RouteValues = new()
    {
        ["siamId"] = Siam, ["siam-id"] = Siam, ["siamid"] = Siam, ["docId"] = DocId, ["doc-id"] = DocId,
        ["docNationalCode"] = DoctorNational, ["docNatioanlCode"] = DoctorNational, ["doctorNationalCode"] = DoctorNational, ["requestBy"] = DoctorNational,
        ["nationalCode"] = PatientNational, ["patientNationalCode"] = PatientNational, ["patient-id"] = PatientNational, ["patientId"] = PatientNational,
        ["secretaryNationalCode"] = Secretary, ["headerID"] = Header.ToString(), ["trackingCode"] = Tracking.ToString(), ["masterId"] = "77", ["id"] = "88", ["masterParent"] = "99",
        ["formCode"] = "F1", ["dentistType"] = "2", ["selectedTooth"] = "T1", ["toothType"] = "3", ["referralId"] = "R1"
    };

    private static readonly PrescriptionDetailInput Detail = new(null, null, null, null, null, DentalServiceFlag.Zero, null, null, null, null, null, null, null, 1, null, null);
    private static readonly PrescriptionCreateInput Prescription = new(null, null, DocId, null, DoctorNational, null, null, [Detail], null, null, PatientNational, null, null, null, null, null);
    private static readonly DentistRuleInput DentistRule = new(null, DocId, PatientNational, null, null);
    private static readonly NursingActionInput Nursing = new(Siam, [1], Secretary);
    private static readonly PatientIllnessInput Illness = new(PatientNational, 1, "09120000000", Siam);

    /// <summary>Manifest service key to the facade call, for every operation that sends a request through the pipeline.</summary>
    private static readonly Dictionary<string, Func<ITaminClient, Task>> Calls = new()
    {
        ["auth-token"] = c => c.ExchangeAuthorizationCodeAsync("code", "https://app.example/cb", new string('a', 43)),
        ["create-prescription"] = c => c.CreatePrescriptionAsync(Prescription),
        ["patient-entitlement"] = c => c.GetPatientEntitlementAsync(Siam, DocId, PatientNational, DoctorNational),
        ["get-prescription"] = c => c.GetPrescriptionDetailsAsync(Header, DoctorNational, DocId),
        ["delete-prescription"] = c => c.DeletePrescriptionAsync(Header, DoctorNational, DocId),
        ["edit-prescription"] = c => c.UpdatePrescriptionAsync(Header, DoctorNational, DocId, [Detail]),
        ["list-prescription-types"] = c => c.ListPrescriptionTypesAsync(),
        ["list-service-types"] = c => c.ListPrescriptionServiceTypesAsync(),
        ["list-services"] = c => c.ListServicesAsync("ST1"),
        ["list-par-taref"] = c => c.ListLaboratoryTariffGroupsAsync(),
        ["list-drug-amounts"] = c => c.ListDrugAmountsAsync(),
        ["list-drug-usages"] = c => c.ListDrugUsagesAsync(),
        ["list-drug-instructions"] = c => c.ListDrugInstructionsAsync(),
        ["list-physio-plans"] = c => c.ListPhysiotherapyPlansAsync(),
        ["list-physio-illnesses"] = c => c.ListPhysiotherapyIllnessesAsync(),
        ["list-complaints"] = c => c.ListComplaintsAsync(),
        ["list-icd10"] = c => c.ListIcd10DiagnosesAsync(),
        ["list-specialties"] = c => c.ListDoctorSpecialtiesAsync(),
        ["list-dosage-forms"] = c => c.ListDosageFormsAsync(),
        ["medication-instructions-by-dosage-form"] = c => c.GetMedicationInstructionsByDosageFormAsync("F1"),
        ["check-dentist-rules"] = c => c.CheckDentistRulesAsync(DentistRule),
        ["dentist-services-without-tooth"] = c => c.ListDentistServicesWithoutToothAsync(2),
        ["dentist-services-by-tooth"] = c => c.ListDentistServicesByToothAsync("T1", PatientNational, 3),
        ["open-referral-count"] = c => c.GetOpenReferralCountAsync(PatientNational, DocId),
        ["get-referral-prescription"] = c => c.GetReferralPrescriptionAsync(PatientNational, DocId, Tracking),
        ["list-referral-feedback-prescriptions"] = c => c.ListReferralFeedbackPrescriptionsAsync(77, PatientNational),
        ["list-recent-referrals"] = c => c.ListRecentDoctorReferralsAsync(PatientNational),
        ["referral-cartable"] = c => c.GetReferralCartableAsync(DoctorNational, PatientNational, Tracking),
        ["referral-feedback-entries"] = c => c.ListReferralFeedbackEntriesAsync("R1"),
        ["referral-feedback-details"] = c => c.GetReferralFeedbackDetailsAsync(88, 99),
        ["patient-open-referrals"] = c => c.ListPatientOpenReferralsAsync(PatientNational),
        ["referral-feedback-questions"] = c => c.ListReferralFeedbackQuestionsAsync(),
        ["referral-feedback-answers"] = c => c.ListReferralFeedbackAnswersAsync(),
        ["nurse-cartable"] = c => c.ListUnclaimedNursingPrescriptionsAsync(Siam, PatientNational),
        ["save-nursing-action"] = c => c.SaveNursingActionsAsync(Nursing),
        ["doctor-nursing-prescriptions"] = c => c.ListDoctorNursingPrescriptionsAsync(Siam, DoctorNational, PatientNational),
        ["referred-service-prescription"] = c => c.GetReferredServicePrescriptionAsync(PatientNational, DoctorNational, DocId, Tracking),
        ["hospitalization-orders"] = c => c.ListHospitalizationOrdersAsync(Siam, Secretary),
        ["patient-hospitalization-orders"] = c => c.ListPatientHospitalizationOrdersAsync(Siam, Secretary, PatientNational),
        ["doctor-active-hospitals"] = c => c.ListDoctorActiveHospitalsAsync(),
        ["family-doctor-share"] = c => c.CalculateFamilyDoctorPatientShareAsync(Siam, PatientNational, DocId),
        ["patient-diseases"] = c => c.ListPatientDiseasesAsync(Siam, PatientNational),
        ["mark-patient-disease"] = c => c.MarkPatientDiseaseAsync(Illness),
        ["doctor-active-clinics"] = c => c.ListDoctorActiveClinicsAsync()
    };

    private static readonly IReadOnlySet<string> AccountOperations = new HashSet<string> { "auth-authorize", "auth-token", "auth-signout", "auth-token-v2" };

    public static TheoryData<string, string, string, string, string> GuideRoutes()
    {
        var data = new TheoryData<string, string, string, string, string>();
        foreach (var route in Manifest())
            data.Add(route.GetProperty("env").GetString()!, route.GetProperty("service").GetString()!, route.GetProperty("host").GetString()!, route.GetProperty("method").GetString()!, route.GetProperty("path").GetString()!);
        return data;
    }

    [Theory]
    [MemberData(nameof(GuideRoutes))]
    public async Task Every_guide_route_is_reached_with_its_values_and_expected_credentials(string environment, string service, string host, string method, string path)
    {
        var transport = new StubTransport();
        var client = Create(environment, transport);
        var expected = RouteParameter().Replace(path, m => RouteValues[m.Groups[1].Value]);

        switch (service)
        {
            case "auth-authorize":
                AssertEndpoint(host, path, client.CreateAuthorizationUri("https://app.example/cb", "challenge", "state"));
                return;
            case "auth-signout":
                AssertEndpoint(host, path, client.CreateSignOutUri("https://app.example/cb"));
                return;
            case "auth-token-v2":
                await client.CompleteAuthorizationAsync("code", "https://app.example/cb", new string('a', 43));
                var token = Assert.Single(transport.Requests);
                Assert.Equal((host, method, path), (token.Uri.Host, token.Method, token.Uri.AbsolutePath));
                return;
        }

        await client.CompleteAuthorizationAsync("code", "https://app.example/cb", new string('a', 43));
        transport.Requests.Clear();
        await Calls[service](client);

        var request = Assert.Single(transport.Requests);
        Assert.Equal((host, method, expected), (request.Uri.Host, request.Method, request.Uri.AbsolutePath));
        Assert.Equal(AccountOperations.Contains(service) ? null : "Bearer access", request.Authorization);
    }

    [Fact]
    public void The_call_table_covers_every_guide_service()
    {
        var services = Manifest().Select(r => r.GetProperty("service").GetString()!).ToHashSet();
        Assert.Equal(47, services.Count);
        services.ExceptWith(Calls.Keys);
        services.ExceptWith(AccountOperations);
        Assert.Empty(services);
    }

    [Fact]
    public void Exactly_the_29_schema_less_operations_stay_opaque()
    {
        var opaque = typeof(ITaminClient).GetMethods().Count(m => m.ReturnType == typeof(Task<UnconstrainedResponse>));
        Assert.Equal(29, opaque);
    }

    [Theory]
    [InlineData("prod", "service-type")]
    [InlineData("pilot", "serviceType")]
    public async Task Services_use_each_environments_query_parameter(string environment, string name)
    {
        var transport = new StubTransport();
        var client = Create(environment, transport);
        await client.CompleteAuthorizationAsync("code", "https://app.example/cb", new string('a', 43));
        await client.ListServicesAsync("ST1");
        Assert.EndsWith($"?{name}=ST1", transport.Requests[^1].Uri.Query);
    }

    [Theory]
    [InlineData("prod", "client-1")]
    [InlineData("pilot", "portal-js")]
    public void Authorization_url_uses_pkce_s256_and_the_environments_client(string environment, string clientId)
    {
        var client = Create(environment, new StubTransport());
        var query = client.CreateAuthorizationUri("https://app.example/cb", "challenge", "state").Query;
        foreach (var part in new[] { "code_challenge=challenge", "code_challenge_method=S256", "response_type=code", "state=state", $"client_id={clientId}", "redirect_uri=https%3A%2F%2Fapp.example%2Fcb" })
            Assert.Contains(part, query);
    }

    [Fact]
    public void Code_challenge_matches_the_rfc_7636_appendix_b_vector()
    {
        Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", TaminClient.CreateCodeChallenge("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"));
    }

    [Theory]
    [InlineData("prod")]
    [InlineData("pilot")]
    public async Task Opaque_operations_keep_array_roots_and_values(string environment)
    {
        var transport = new StubTransport { Body = """[{"code":"001","price":12.5,"on":true,"none":null}]""" };
        var client = Create(environment, transport);
        await client.CompleteAuthorizationAsync("code", "https://app.example/cb", new string('a', 43));
        var result = await client.ListComplaintsAsync();
        var item = Assert.Single(result.Payload.EnumerateArray());
        Assert.Equal("001", item.GetProperty("code").GetString());
        Assert.Equal(12.5, item.GetProperty("price").GetDouble());
        Assert.True(item.GetProperty("on").GetBoolean());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("none").ValueKind);
    }

    [Fact]
    public async Task Failures_carry_the_operation_id_status_and_body()
    {
        var transport = new StubTransport { Status = HttpStatusCode.BadRequest, Body = """{"error":"x"}""" };
        var client = Create("prod", transport, tokenStatus: HttpStatusCode.OK);
        await client.CompleteAuthorizationAsync("code", "https://app.example/cb", new string('a', 43));
        var failure = await Assert.ThrowsAsync<TaminApiException>(() => client.GetReferralPrescriptionAsync(PatientNational, DocId, Tracking));
        Assert.Equal(("getReferralPrescription", 400, """{"error":"x"}"""), (failure.OperationId, failure.StatusCode, failure.RawBody));
        Assert.DoesNotContain(PatientNational, failure.Message);
    }

    [Fact]
    public async Task Prescription_create_is_not_retried_but_a_read_is()
    {
        var transport = new StubTransport { Status = HttpStatusCode.ServiceUnavailable };
        var client = Create("prod", transport);
        await client.CompleteAuthorizationAsync("code", "https://app.example/cb", new string('a', 43));

        transport.Requests.Clear();
        await Assert.ThrowsAsync<TaminApiException>(() => client.CreatePrescriptionAsync(Prescription));
        Assert.Single(transport.Requests);

        transport.Requests.Clear();
        await Assert.ThrowsAsync<TaminApiException>(() => client.ListComplaintsAsync());
        Assert.Equal(3, transport.Requests.Count);
    }

    [Theory]
    [InlineData("<html>not json</html>")]
    [InlineData("")]
    public async Task Typed_responses_that_are_unreadable_or_empty_fail_with_the_operation_id(string body)
    {
        var transport = new StubTransport { Body = body };
        var client = Create("prod", transport);
        await client.CompleteAuthorizationAsync("code", "https://app.example/cb", new string('a', 43));
        var failure = await Assert.ThrowsAsync<TaminApiException>(() => client.CreatePrescriptionAsync(Prescription));
        Assert.Equal("createPrescription", failure.OperationId);
        Assert.Null(failure.StatusCode);
    }

    [Fact]
    public async Task Calls_before_authorization_require_reauthorization_and_send_nothing()
    {
        var transport = new StubTransport();
        var client = Create("prod", transport);
        await Assert.ThrowsAsync<Auth.TaminReauthorizationRequiredException>(() => client.ListComplaintsAsync());
        Assert.Empty(transport.Requests);
    }

    [Fact]
    public async Task Blank_route_values_and_invalid_mutations_are_rejected_before_sending()
    {
        var transport = new StubTransport();
        var client = Create("pilot", transport);
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetOpenReferralCountAsync(" ", DocId));
        await Assert.ThrowsAsync<TaminRequestValidationException>(() => client.SaveNursingActionsAsync(Nursing with { NoteDetailsEprscIds = [] }));
        await Assert.ThrowsAsync<TaminRequestValidationException>(() => client.MarkPatientDiseaseAsync(Illness with { MobileNumber = "" }));
        Assert.Empty(transport.Requests);
    }

    [Fact]
    public async Task Route_values_cannot_change_the_route()
    {
        var transport = new StubTransport();
        var client = Create("prod", transport);
        await client.CompleteAuthorizationAsync("code", "https://app.example/cb", new string('a', 43));
        await client.ListRecentDoctorReferralsAsync("a/../b");
        Assert.EndsWith("/referredList/a%2F..%2Fb", transport.Requests[^1].Uri.OriginalString);
    }

    [Fact]
    public async Task Pilot_rejects_a_client_id_the_contract_does_not_allow_on_authorization()
    {
        var client = Create("pilot", new StubTransport(), clientId: "other");
        await Assert.ThrowsAsync<ArgumentException>(() => client.CompleteAuthorizationAsync("code", "https://app.example/cb", new string('a', 43)));
    }

    private static ITaminClient Create(string environment, StubTransport transport, string? clientId = null, HttpStatusCode tokenStatus = HttpStatusCode.OK)
    {
        transport.TokenStatus = tokenStatus;
        var options = new TaminClientOptions(clientId ?? (environment == "pilot" ? "portal-js" : "client-1"), "audience", transport);
        return environment == "pilot" ? TaminClient.CreatePilot(options) : TaminClient.CreateProduction(options);
    }

    private static void AssertEndpoint(string host, string path, Uri actual) => Assert.Equal((host, path), (actual.Host, actual.AbsolutePath));

    private static System.Text.RegularExpressions.Regex RouteParameter() => new(@"\{([^}]+)\}");

    private static JsonElement[] Manifest()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "openapi", "tamin-route-manifest.json"))) directory = directory.Parent;
        Assert.NotNull(directory);
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory.FullName, "openapi", "tamin-route-manifest.json")));
        return document.RootElement.GetProperty("routes").EnumerateArray().Select(r => r.Clone()).ToArray();
    }

    private sealed class StubTransport : HttpMessageHandler
    {
        public sealed record Sent(Uri Uri, string Method, string? Authorization);
        public List<Sent> Requests { get; } = [];
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;
        public HttpStatusCode TokenStatus { get; set; } = HttpStatusCode.OK;
        public string Body { get; init; } = "{}";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (Requests) Requests.Add(new(request.RequestUri!, request.Method.Method, request.Headers.Authorization?.ToString()));
            var isDoctorToken = request.RequestUri!.AbsolutePath == "/auth/server/v2/token";
            return Task.FromResult(new HttpResponseMessage(isDoctorToken ? TokenStatus : Status)
            {
                Content = new StringContent(isDoctorToken ? """{"access_token":"access","refresh_token":"refresh","expires_in":3600}""" : Body, Encoding.UTF8, "application/json")
            });
        }
    }
}
