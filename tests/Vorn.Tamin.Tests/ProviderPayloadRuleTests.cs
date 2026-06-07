using System.Net;
using System.Text;
using Vorn.Tamin;

namespace Vorn.Tamin.Tests;

public sealed class ProviderPayloadRuleTests
{
    [Fact]
    public void ProviderSerializer_PreservesStringCodeCharacters()
    {
        var serializer = new TaminProviderSerializer();

        var result = serializer.SerializeStringCode(" 001-AB09 ", "srvCode");

        Assert.Equal("001-AB09", result);
    }

    [Fact]
    public void ProviderSerializer_RejectsIsoDateBeforeTransport()
    {
        var serializer = new TaminProviderSerializer();

        var ex = Assert.Throws<TaminValidationException>(() => serializer.SerializeJalaliDate("2026-06-01", "prescDate"));

        Assert.Contains(ex.Failures, failure => failure.Field == "prescDate" && failure.Code == "jalali-date-shape");
    }

    [Fact]
    public void ProfessionalIdentifierFormatter_AppliesMidwifeAndForeignDoctorRules()
    {
        var formatter = new ProfessionalIdentifierFormatter();

        Assert.Equal("12*345", formatter.FormatMidwifeDoctorId("12م345"));
        Assert.Equal("FIDA001-A", formatter.FormatForeignDoctorNationalCode("001-A"));
        Assert.Equal("FIDA009", formatter.FormatForeignDoctorNationalCode("fida009"));
    }

    [Fact]
    public void PrescriptionValidationRules_ReturnStructuredFailuresForDoctorEnrollmentFields()
    {
        var rules = new PrescriptionValidationRules();

        var failures = rules.ValidateDoctorEnrollmentFields("D-001", "123", "912");

        Assert.Contains(failures, failure => failure.Field == "docNationalCode" && failure.Code == "national-code-shape");
        Assert.Contains(failures, failure => failure.Field == "docMobileNo" && failure.Code == "mobile-shape");
    }

    [Fact]
    public void PrescriptionValidationRules_ReturnStructuredFailuresForMedicationRules()
    {
        var rules = new PrescriptionValidationRules();
        var request = new RegisterDrugPrescriptionRequest
        {
            DoctorId = "D-001",
            PatientNationalId = "1234567890",
            VisitDate = "14030601",
            DrugItems = [new DrugItem { DrugCode = "001", Quantity = 0, RepeatCount = -1 }]
        };

        var failures = rules.Validate(request);

        Assert.Contains(failures, failure => failure.Field == "drug_items[0].quantity" && failure.Code == "positive");
        Assert.Contains(failures, failure => failure.Field == "drug_items[0].repeat_count" && failure.Code == "non-negative");
    }

    [Fact]
    public void PrescriptionValidationRules_ReturnStructuredFailuresForParaclinicalLaboratoryRules()
    {
        var rules = new PrescriptionValidationRules();
        var request = new RegisterParaclinicPrescriptionRequest
        {
            DoctorId = "D-001",
            PatientNationalId = "1234567890",
            VisitDate = "14030601",
            ServiceItems = [new ServiceItem { ServiceCode = "LAB-001", Quantity = 1 }]
        };

        var failures = rules.Validate(request);

        Assert.Contains(failures, failure => failure.Field == "service_items[0].service_group" && failure.Code == "required");
    }

    [Fact]
    public void EligibilityValidationRules_ReturnStructuredFailuresForPrivatePracticeIdentifiers()
    {
        var rules = new EligibilityValidationRules();

        var failures = rules.ValidatePrivatePracticeIdentifiers("", "123", "1234567890");

        Assert.Contains(failures, failure => failure.Field == "doctor_id" && failure.Code == "required");
        Assert.Contains(failures, failure => failure.Field == "doctor_national_code" && failure.Code == "national-code-shape");
    }

    [Fact]
    public async Task PrescriptionClient_InvalidRequestDoesNotSendTransport()
    {
        var sent = false;
        var session = CreateSession((_, _) =>
        {
            sent = true;
            return Task.FromResult(JsonResponse());
        });

        var ex = await Assert.ThrowsAsync<TaminValidationException>(() => session.Prescription.RegisterDrugPrescriptionAsync(
            new RegisterDrugPrescriptionRequest
            {
                DoctorId = "D-001",
                PatientNationalId = "1234567890",
                VisitDate = "2026-06-01",
                DrugItems = [new DrugItem { DrugCode = "DR001", Quantity = 1 }]
            }));

        Assert.False(sent);
        Assert.Contains(ex.Failures, failure => failure.Field == "visit_date" && failure.Code == "jalali-date-shape");
    }

    [Fact]
    public async Task PrescriptionClient_SerializesProviderStringsWithoutNumericCoercion()
    {
        string? capturedBody = null;
        var session = CreateSession(async (request, _) =>
        {
            capturedBody = await request.Content!.ReadAsStringAsync();
            return JsonResponse();
        });

        await session.Prescription.RegisterDrugPrescriptionAsync(
            new RegisterDrugPrescriptionRequest
            {
                DoctorId = "000-DOC",
                PatientNationalId = "0012345678",
                VisitDate = "14030601",
                MobileNumber = "09123456789",
                DrugItems = [new DrugItem { DrugCode = "000-DR-A", Quantity = 1, RepeatCount = 2 }]
            });

        Assert.NotNull(capturedBody);
        Assert.Contains("\"docId\":\"000-DOC\"", capturedBody);
        Assert.Contains("\"patient\":\"0012345678\"", capturedBody);
        Assert.Contains("\"prescDate\":\"14030601\"", capturedBody);
        Assert.Contains("\"drugCode\":\"000-DR-A\"", capturedBody);
        Assert.Contains("\"repeat\":\"2\"", capturedBody);
    }

    private static TaminSession CreateSession(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        => new(new HttpClient(new StubHandler(handler)), oauthToken: null, baseUri: new Uri("https://example.test/"), needToken: false);

    private static HttpResponseMessage JsonResponse()
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"ok\":true}", Encoding.UTF8, "application/json")
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
