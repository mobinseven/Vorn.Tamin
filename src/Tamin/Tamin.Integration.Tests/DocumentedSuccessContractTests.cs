using System.Text.Json;
using System.Text;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;
using Tamin.Integration.Mapping;
using ApiModels = Tamin.Client.Api.Prod.Models;
using SoaModels = Tamin.Client.Soa.Prod.Models;
using PilotApiModels = Tamin.Client.Api.Pilot.Models;
using PilotSoaModels = Tamin.Client.Soa.Pilot.Models;

namespace Tamin.Integration.Tests;

public sealed class DocumentedSuccessContractTests
{
    private readonly TaminMappingService mapper = new();

    [Fact]
    public void Maps_referral_cartable_documented_example()
    {
        var result = mapper.MapReferralCartable(Kiota("""{"status":200,"family":"SUCCESSFUL","reason":"OK","data":{"total":1,"list":[{"noteHeadId":"شناسه ی یکتا نسخه","referralId":"شناسه ارجاع ثبت شده","patientID":"کد ملی پزشک","docID":"شماره نظام پزشک","prescDate":"تاریخ نسخه","referralDate":"تاریخ ارجاع"}],"id":0,"masterParent":327247893,"masterChild":null,"referralDate":1747839599371}}""", SoaModels.ReferralCartableResponse.CreateFromDiscriminatorValue));
        Assert.Equal(200, result.Status); Assert.Equal("SUCCESSFUL", result.Family); Assert.Equal("OK", result.Reason); Assert.Null(result.TraceId);
        Assert.Equal(1, result.Data.Total); Assert.Single(result.Data.Items); Assert.Equal("شناسه ی یکتا نسخه", result.Data.Items[0].NoteHeadId);
        Assert.Equal("شناسه ارجاع ثبت شده", result.Data.Items[0].ReferralId); Assert.Equal("کد ملی پزشک", result.Data.Items[0].PatientId);
        Assert.Equal("شماره نظام پزشک", result.Data.Items[0].DoctorId); Assert.Equal("تاریخ نسخه", result.Data.Items[0].PrescriptionDate);
        Assert.Equal("تاریخ ارجاع", result.Data.Items[0].ReferralDate); Assert.Equal(0, result.Data.Id); Assert.Equal(327247893, result.Data.MasterParent);
        Assert.Null(result.Data.MasterChild); Assert.Equal(1747839599371, result.Data.ReferralDate);
    }

    [Fact]
    public void Maps_referral_feedback_details_documented_schema_examples()
    {
        var result = mapper.MapReferralFeedbackDetails(Kiota("""{"status":200,"family":"SUCCESSFUL","reason":"OK","data":{"total":1,"list":[{"noteHeadEprscId":9149887014,"patient":"0000000000","prescType":{"prescTypeId":7,"prescTypeCode":"07","prescTypeDesc":"referral"},"prescDate":null,"noteDetailEprscs":[],"noteDetailsReferralList":null}]}}""", SoaModels.ReferralFeedbackDetailResponse.CreateFromDiscriminatorValue));
        Assert.Equal(200, result.Status); Assert.Equal("SUCCESSFUL", result.Family); Assert.Equal("OK", result.Reason); Assert.Null(result.TraceId);
        Assert.Equal(1, result.Data.Total); Assert.Single(result.Data.Items); Assert.Equal(9149887014, result.Data.Items[0].NoteHeadEprscId);
        Assert.Equal("0000000000", result.Data.Items[0].Patient); Assert.Null(result.Data.Items[0].PrescriptionDate);
        Assert.Equal(JsonValueKind.Array, result.Data.Items[0].Items.ValueKind); Assert.Equal(0, result.Data.Items[0].Items.GetArrayLength()); Assert.Null(result.Data.Items[0].Referrals);
        Assert.Equal(new PrescriptionTypeDto(7, "07", "referral"), result.Data.Items[0].PrescriptionType);
    }

    [Fact]
    public void Maps_referral_feedback_prescription_type_from_raw_json_and_preserves_absence()
    {
        using var presentDocument = JsonDocument.Parse("""{"status":200,"data":{"total":1,"list":[{"prescType":{"prescTypeId":7,"prescTypeCode":"07","prescTypeDesc":"referral"},"noteDetailEprscs":[]}]}}""");
        var present = mapper.MapReferralFeedbackDetails(presentDocument.RootElement);
        Assert.Equal(new PrescriptionTypeDto(7, "07", "referral"), Assert.Single(present.Data.Items).PrescriptionType);

        using var absentDocument = JsonDocument.Parse("""{"status":200,"data":{"total":1,"list":[{"noteDetailEprscs":[]}]}}""");
        var absent = mapper.MapReferralFeedbackDetails(absentDocument.RootElement);
        Assert.Null(Assert.Single(absent.Data.Items).PrescriptionType);
    }

    [Fact]
    public void Maps_patient_open_referrals_documented_example()
    {
        var result = mapper.MapPatientOpenReferrals(Kiota("""{"status":200,"family":"SUCCESSFUL","reason":"OK","data":{"noteDetailsReferrals":[{"specCode":"00199","specDesc":"پزشکی-بیولوژی","specGRP":"6","status":"1","statusstDate":"13900101","lstatus":"0"}],"count":1}}""", SoaModels.PatientOpenReferralsResponse.CreateFromDiscriminatorValue));
        Assert.Equal(200, result.Status); Assert.Equal("SUCCESSFUL", result.Family); Assert.Equal("OK", result.Reason); Assert.Null(result.TraceId);
        Assert.Equal(1, result.Data.Count); Assert.Single(result.Data.Referrals); Assert.Equal("00199", result.Data.Referrals[0].SpecCode);
        Assert.Equal("پزشکی-بیولوژی", result.Data.Referrals[0].SpecDesc); Assert.Equal("6", result.Data.Referrals[0].SpecGroup);
        Assert.Equal("1", result.Data.Referrals[0].Status); Assert.Equal("13900101", result.Data.Referrals[0].StatusDate); Assert.Equal("0", result.Data.Referrals[0].LStatus);
    }

    [Fact]
    public void Maps_hospitalization_orders_documented_example()
    {
        var result = mapper.MapHospitalizationOrders(Kiota("""{"status":200,"family":"SUCCESSFUL","reason":"OK","traceId":"1392563c-f998-4ab3-ae25-800554c9b1e3","data":[{"id":4000026,"masterParent":4000025,"referralDate":170000721000,"docId":"00000000008","quantity":1,"docFamily":true}]}""", SoaModels.HospitalizationOrderResponse.CreateFromDiscriminatorValue));
        Assert.Equal(200, result.Status); Assert.Equal("SUCCESSFUL", result.Family); Assert.Equal("OK", result.Reason);
        Assert.Equal("1392563c-f998-4ab3-ae25-800554c9b1e3", result.TraceId); Assert.Single(result.Data);
        Assert.Equal(4000026, result.Data[0].Id); Assert.Equal(4000025, result.Data[0].MasterParent); Assert.Equal(170000721000, result.Data[0].ReferralDate);
        Assert.Equal("00000000008", result.Data[0].DoctorId); Assert.Equal(1, result.Data[0].Quantity); Assert.True(result.Data[0].DoctorFamily);
    }

    [Theory]
    [InlineData("386,865", true)]
    [InlineData("مراجعه خارج از پزشک خانواده", false)]
    public void Maps_both_documented_family_doctor_success_variants(string price, bool familyDoctor)
    {
        var payload = JsonSerializer.Serialize(new { status = 200, family = "SUCCESSFUL", reason = "OK", traceId = "63854023-1eb1-4f68-a2c6-9d00d9a83c65", data = new { price, familyDoctor } });
        var result = mapper.MapFamilyDoctorShare(Kiota(payload, ApiModels.FamilyDoctorShareResponse.CreateFromDiscriminatorValue));
        Assert.Equal(200, result.Status); Assert.Equal("SUCCESSFUL", result.Family); Assert.Equal("OK", result.Reason);
        Assert.Equal("63854023-1eb1-4f68-a2c6-9d00d9a83c65", result.TraceId); Assert.Equal(price, result.Data.Price); Assert.Equal(familyDoctor, result.Data.FamilyDoctor);
    }

    [Fact]
    public void Maps_patient_diseases_documented_example()
    {
        var result = mapper.MapPatientDiseases(Kiota("""{"status":200,"family":"SUCCESSFUL","reason":"OK","traceId":"2bccffbca-f1ee-4e7e-9da0-470ffd7dd6c6","data":{"total":1,"list":[{"id":86,"illText":"رئینوپاتی دیابتی","status":"1","statusDate":null,"type":"1","priorityOrder":null,"codeMapping":"H36.0","illName":"H36.0"}]}}""", ApiModels.PatientDiseaseListResponse.CreateFromDiscriminatorValue));
        Assert.Equal(200, result.Status); Assert.Equal("SUCCESSFUL", result.Family); Assert.Equal("OK", result.Reason); Assert.Equal("2bccffbca-f1ee-4e7e-9da0-470ffd7dd6c6", result.TraceId);
        Assert.Equal(1, result.Data.Total); Assert.Single(result.Data.Diseases); Assert.Equal(86, result.Data.Diseases[0].Id);
        Assert.Equal("رئینوپاتی دیابتی", result.Data.Diseases[0].IllText); Assert.Equal("1", result.Data.Diseases[0].Status); Assert.Null(result.Data.Diseases[0].StatusDate);
        Assert.Equal("1", result.Data.Diseases[0].Type); Assert.Null(result.Data.Diseases[0].PriorityOrder); Assert.Equal("H36.0", result.Data.Diseases[0].CodeMapping); Assert.Equal("H36.0", result.Data.Diseases[0].IllName);
    }

    [Fact]
    public void All_29_schema_less_operations_remain_opaque()
    {
        var environmentAdapters = new HashSet<string>(StringComparer.Ordinal) { "MapPilotUndocumentedMutation", "MapPilotSoaUndocumented", "MapPilotApiUndocumented", "MapPilotNursingResponse" };
        var count = typeof(TaminMappingService).GetMethods().Count(method => method.IsPublic && method.DeclaringType == typeof(TaminMappingService) && method.ReturnType == typeof(UnconstrainedResponse) && !environmentAdapters.Contains(method.Name));
        Assert.Equal(29, count);
    }

    [Fact]
    public void Pilot_generated_variants_preserve_all_six_equivalent_success_contracts()
    {
        var cartable = mapper.MapReferralCartable(Kiota("""{"status":200,"family":"SUCCESSFUL","reason":"OK","data":{"total":1,"list":[{"noteHeadId":"head","referralId":"ref","patientID":"patient","docID":"doctor","prescDate":"date","referralDate":"ref-date"}],"id":0,"masterParent":1,"masterChild":null,"referralDate":2}}""", PilotSoaModels.ReferralCartableResponse.CreateFromDiscriminatorValue));
        Assert.Equal((200, "SUCCESSFUL", "OK", 1, 1), (cartable.Status, cartable.Family, cartable.Reason, cartable.Data.Total, cartable.Data.Items.Count));
        Assert.Null(cartable.Data.MasterChild); Assert.Equal("doctor", cartable.Data.Items[0].DoctorId);

        var feedback = mapper.MapReferralFeedbackDetails(Kiota("""{"status":200,"family":"SUCCESSFUL","reason":"OK","data":{"total":1,"list":[{"noteHeadEprscId":9,"patient":"patient","prescType":{"prescTypeId":7,"prescTypeCode":"07","prescTypeDesc":"referral"},"prescDate":null,"noteDetailEprscs":[],"noteDetailsReferralList":null}]}}""", PilotSoaModels.ReferralFeedbackDetailResponse.CreateFromDiscriminatorValue));
        Assert.Equal(1, feedback.Data.Total); Assert.Single(feedback.Data.Items); Assert.Null(feedback.Data.Items[0].PrescriptionDate); Assert.Null(feedback.Data.Items[0].Referrals);
        Assert.Equal(new PrescriptionTypeDto(7, "07", "referral"), feedback.Data.Items[0].PrescriptionType);

        var open = mapper.MapPatientOpenReferrals(Kiota("""{"status":200,"family":"SUCCESSFUL","reason":"OK","data":{"noteDetailsReferrals":[{"specCode":"1","specDesc":"desc","specGRP":"g","status":"s","statusstDate":"d","lstatus":"l"}],"count":1}}""", PilotSoaModels.PatientOpenReferralsResponse.CreateFromDiscriminatorValue));
        Assert.Equal(1, open.Data.Count); Assert.Single(open.Data.Referrals); Assert.Equal("l", open.Data.Referrals[0].LStatus);

        var hospitalization = mapper.MapHospitalizationOrders(Kiota("""{"status":200,"family":"SUCCESSFUL","reason":"OK","traceId":"trace","data":[{"id":1,"masterParent":2,"referralDate":3,"docId":"doctor","quantity":4,"docFamily":true}]}""", PilotSoaModels.HospitalizationOrderResponse.CreateFromDiscriminatorValue));
        Assert.Equal("trace", hospitalization.TraceId); Assert.Single(hospitalization.Data); Assert.True(hospitalization.Data[0].DoctorFamily);

        foreach (var familyDoctor in new[] { true, false })
        {
            var share = mapper.MapFamilyDoctorShare(Kiota(JsonSerializer.Serialize(new { status = 200, family = "SUCCESSFUL", reason = "OK", traceId = "trace", data = new { price = familyDoctor ? "386,865" : "outside", familyDoctor } }), PilotApiModels.FamilyDoctorShareResponse.CreateFromDiscriminatorValue));
            Assert.Equal(familyDoctor, share.Data.FamilyDoctor); Assert.Equal("trace", share.TraceId);
        }

        var diseases = mapper.MapPatientDiseases(Kiota("""{"status":200,"family":"SUCCESSFUL","reason":"OK","traceId":"trace","data":{"total":1,"list":[{"id":86,"illText":"text","status":"1","statusDate":null,"type":"1","priorityOrder":null,"codeMapping":"H36.0","illName":"H36.0"}]}}""", PilotApiModels.PatientDiseaseListResponse.CreateFromDiscriminatorValue));
        Assert.Equal(1, diseases.Data.Total); Assert.Single(diseases.Data.Diseases); Assert.Null(diseases.Data.Diseases[0].StatusDate); Assert.Null(diseases.Data.Diseases[0].PriorityOrder);
    }

    private static T Kiota<T>(string value, ParsableFactory<T> factory) where T : IParsable
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(value));
        var root = new JsonParseNodeFactory().GetRootParseNodeAsync("application/json", stream, CancellationToken.None).GetAwaiter().GetResult();
        return root.GetObjectValue(factory)!;
    }
}
