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
        var result = mapper.MapReferralCartable(Kiota("""{"status":200,"family":"SUCCESSFUL","reason":"OK","data":{"total":1,"list":[{"noteHeadId":"شناسه ی یکتا نسخه","referralId":"شناسه ارجاع ثبت شده","patientID":"کد ملی پزشک","docID":"شماره نظام پزشک","prescDate":"تاریخ نسخه","referralDate":"تاریخ ارجاع"}]}}""", SoaModels.ReferralCartableResponse.CreateFromDiscriminatorValue));
        Assert.Equal(200, result.Status); Assert.Equal("SUCCESSFUL", result.Family); Assert.Equal("OK", result.Reason); Assert.Null(result.TraceId);
        Assert.Equal(1, result.Data.Total); Assert.Single(result.Data.Items); Assert.Equal("شناسه ی یکتا نسخه", result.Data.Items[0].NoteHeadId);
        Assert.Equal("شناسه ارجاع ثبت شده", result.Data.Items[0].ReferralId); Assert.Equal("کد ملی پزشک", result.Data.Items[0].PatientId);
        Assert.Equal("شماره نظام پزشک", result.Data.Items[0].DoctorId); Assert.Equal("تاریخ نسخه", result.Data.Items[0].PrescriptionDate);
        Assert.Equal("تاریخ ارجاع", result.Data.Items[0].ReferralDate);
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
    public void Pilot_generated_variants_preserve_all_six_equivalent_success_contracts()
    {
        var cartable = mapper.MapReferralCartable(Kiota("""{"status":200,"family":"SUCCESSFUL","reason":"OK","data":{"total":1,"list":[{"noteHeadId":"head","referralId":"ref","patientID":"patient","docID":"doctor","prescDate":"date","referralDate":"ref-date"}]}}""", PilotSoaModels.ReferralCartableResponse.CreateFromDiscriminatorValue));
        Assert.Equal((200, "SUCCESSFUL", "OK", 1, 1), (cartable.Status, cartable.Family, cartable.Reason, cartable.Data.Total, cartable.Data.Items.Count));
        Assert.Equal("doctor", cartable.Data.Items[0].DoctorId);

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

    [Fact]
    public void Maps_hospitalization_orders_p57_patient_national_code_for_all_three_generated_variants()
    {
        const string payload = """{"status":200,"family":"SUCCESSFUL","reason":"OK","traceId":"trace","data":[{"id":4000026,"masterParent":4000025,"masterChild":null,"referralDate":170000721000,"docId":"0000000008","quantity":1,"specCodeReferral":"00446","specDescReferral":"a","specCodeReferred":"00446","specDescReferred":"b","icd10Referrals":null,"complaints":null,"message":"m","diagnosisIDs":"X46.43","docFamily":true,"patientNationalCode":"**********"}]}""";
        var expected = new HospitalizationOrderDto(4000026, 4000025, 170000721000, "0000000008", 1, true, "**********");
        Assert.Equal(expected, Assert.Single(mapper.MapHospitalizationOrders(Kiota(payload, SoaModels.HospitalizationOrderResponse.CreateFromDiscriminatorValue)).Data));
        Assert.Equal(expected, Assert.Single(mapper.MapHospitalizationOrders(Kiota(payload, ApiModels.HospitalizationOrderResponse.CreateFromDiscriminatorValue)).Data));
        Assert.Equal(expected, Assert.Single(mapper.MapHospitalizationOrders(Kiota(payload, PilotSoaModels.HospitalizationOrderResponse.CreateFromDiscriminatorValue)).Data));
    }

    private const string MedicationInstructionsPayload = """{"status":200,"family":"SUCCESSFUL","reason":"OK","traceId":"trace","data":{"methodUsages":[{"id":"307166007","name":"before exercise","code":"307166007","nameLatin":"Before exercise (qualifier value)"}],"drugUsages":[{"id":"231271000","name":"joint","code":"231271000","nameLatin":"Injection into joint (procedure)"}],"drugInstructions":[{"id":"2546009","name":"night","code":"2546009","nameLatin":"Night time (qualifier value)"}],"formAmounts":[{"id":"24","name":"each","code":"111","nameLatin":"Each","drugAmounts":[{"id":"580","name":"quarter","code":"580","nameLatin":"1/4 Each"}]}]}}""";

    [Fact]
    public void Maps_medication_instructions_by_dosage_form_for_production_and_pilot()
    {
        var expected = new MedicationInstructionsDto(
            [new("2546009", "2546009", "night", "Night time (qualifier value)")],
            [new("231271000", "231271000", "joint", "Injection into joint (procedure)")],
            [new("307166007", "307166007", "before exercise", "Before exercise (qualifier value)")],
            [new("24", "111", "each", "Each", [new("580", "580", "quarter", "1/4 Each")])]);
        foreach (var result in new[]
        {
            mapper.MapMedicationInstructionsByDosageForm(Kiota(MedicationInstructionsPayload, ApiModels.MedicationInstructionsResponse.CreateFromDiscriminatorValue)),
            mapper.MapMedicationInstructionsByDosageForm(Kiota(MedicationInstructionsPayload, PilotSoaModels.MedicationInstructionsResponse.CreateFromDiscriminatorValue))
        })
        {
            Assert.Equal((200, "SUCCESSFUL", "OK", "trace"), (result.Status, result.Family, result.Reason, result.TraceId));
            AssertMedication(expected, result.Data);
        }
    }

    private static void AssertMedication(MedicationInstructionsDto expected, MedicationInstructionsDto actual)
    {
        Assert.Equal(expected.DrugInstructions, actual.DrugInstructions); Assert.Equal(expected.DrugUsages, actual.DrugUsages); Assert.Equal(expected.MethodUsages, actual.MethodUsages);
        var form = Assert.Single(actual.FormAmounts);
        Assert.Equal(("24", "111", "each", "Each"), (form.Id, form.Code, form.Name, form.NameLatin));
        Assert.Equal(expected.FormAmounts[0].DrugAmounts, form.DrugAmounts);
    }

    private const string ReferralFeedbackEntriesPayload = """{"status":200,"family":"SUCCESSFUL","reason":"OK","data":{"total":2,"list":[{"id":327241062,"masterParent":327240269,"masterChild":327247893,"referralDate":1750517662000,"docId":"2000200090","quantity":1,"specCodeReferral":"00100","specDescReferral":"general","specCodeReferred":"00138","specDescReferred":"cardiology","icd10Referrals":[{"icdId":"W27.19","icdCode":"W27.19","icdName":"name"}],"complaints":[{"id":7,"code":9000000001,"displayName":"cough","englishName":"Cough","terminology":"SNOMEDCT","status":"1"}],"message":"note","diagnosisIDs":"W27.19"},{"id":0,"masterParent":327247893,"masterChild":null,"referralDate":1747839599371,"docId":null,"quantity":0,"specCodeReferral":"00138","specDescReferral":"cardiology","specCodeReferred":null,"specDescReferred":null,"icd10Referrals":null,"complaints":null,"message":null,"diagnosisIDs":null}]}}""";

    [Fact]
    public void Maps_referral_feedback_entries_for_production_and_pilot_including_null_members()
    {
        foreach (var result in new[]
        {
            mapper.MapReferralFeedbackEntries(Kiota(ReferralFeedbackEntriesPayload, SoaModels.ReferralListResponse.CreateFromDiscriminatorValue)),
            mapper.MapReferralFeedbackEntries(Kiota(ReferralFeedbackEntriesPayload, PilotSoaModels.ReferralListResponse.CreateFromDiscriminatorValue))
        })
        {
            Assert.Equal((200, "SUCCESSFUL", "OK", null), (result.Status, result.Family, result.Reason, result.TraceId));
            Assert.Equal(2, result.Data.Total); Assert.Equal(2, result.Data.Entries.Count);
            var first = result.Data.Entries[0];
            Assert.Equal((327241062L, 327240269L, 327247893L, 1750517662000L, "2000200090", 1, "note", "W27.19"), (first.Id, first.MasterParent, first.MasterChild, first.ReferralDate, first.DoctorId, first.Quantity, first.Message, first.DiagnosisIds));
            Assert.Equal(("00100", "general", "00138", "cardiology"), (first.SpecCodeReferral, first.SpecDescReferral, first.SpecCodeReferred, first.SpecDescReferred));
            Assert.Equal(new ReferralIcd10Dto("W27.19", "W27.19", "name"), Assert.Single(first.Icd10Referrals));
            Assert.Equal(new ReferralComplaintDto(7, 9000000001, "cough", "Cough", "SNOMEDCT", "1"), Assert.Single(first.Complaints));
            var second = result.Data.Entries[1];
            Assert.Equal((0L, 327247893L, (long?)null, (string?)null), (second.Id, second.MasterParent, second.MasterChild, second.DoctorId));
            Assert.Empty(second.Icd10Referrals); Assert.Empty(second.Complaints); Assert.Null(second.Message);
        }
    }

    private const string NursingPrescriptionsPayload = """{"total":1,"list":[{"noteHeadId":1343691,"prescCreationTime":1783941531069,"docId":"00000111111","patientFirstName":"****","patientLastName":"*****","docEprscId":111111,"clinicDocId":111111,"trackingCode":86073,"nationalCode":"0000000000","isOk":"3","docFirstName":"*****","docLastName":"******","prescType":5,"isSpecialPatient":1,"noteDetailEprscs":[{"noteDetailsEprscId":1343694,"noteDetailDrug":null,"srvId":{"srvId":172581,"srvType":{"srvType":"21","srvTypeDes":"nursing","status":"1","statusstDate":"14011203","custType":"4","prescTypeId":2,"headExpireDate":62},"srvCode":"0000903584","srvName":"nursing package","srvName2":null,"srvPrice":1230000,"doseCode":null,"formCode":null,"parTarefGrp":{"parGrpCode":"002","parGrpDesc":"","parGrpRem":"1","status":"1","statusStDate":"13830101"},"status":"1","terminology":null},"srvQty":1,"srvRem":1,"srvPrice":1230000,"timesAday":null,"dose":null,"doseCode":null,"drugInstruction":null,"illnessId":null,"planId":null,"drugAmntId":null,"drugInstId":null,"toothId":null,"referenceStatus":null,"isOk":"3","readOnly":false,"isBrand":null,"messages":null,"dialysisType":null}]}]}""";

    [Fact]
    public void Maps_doctor_nursing_prescriptions_without_an_envelope_for_production_and_pilot()
    {
        var production = Kiota(NursingPrescriptionsPayload, ApiModels.NursingPrescriptionListResponse.CreateFromDiscriminatorValue);
        var service = production.List![0].NoteDetailEprscs![0].SrvId!;
        Assert.Equal("002", service.ParTarefGrp!.ParGrpCode); Assert.Equal("13830101", service.ParTarefGrp.StatusStDate); Assert.Equal("21", service.SrvType!.SrvType);
        var expectedItem = new NursingPrescriptionItemDto(1343694, 172581, "0000903584", "nursing package", 1, 1, 1230000, "3", false);
        foreach (var result in new[] { mapper.MapDoctorNursingPrescriptions(production), mapper.MapDoctorNursingPrescriptions(Kiota(NursingPrescriptionsPayload, PilotSoaModels.NursingPrescriptionListResponse.CreateFromDiscriminatorValue)) })
        {
            Assert.Equal(1, result.Total);
            var prescription = Assert.Single(result.Prescriptions);
            Assert.Equal((1343691L, 111111L, 111111L, 86073, "0000000000", "00000111111"), (prescription.NoteHeadId, prescription.DocEprscId, prescription.ClinicDocId, prescription.TrackingCode, prescription.NationalCode, prescription.DoctorId));
            Assert.Equal((5, 1, "3", 1783941531069L), (prescription.PrescriptionType, prescription.IsSpecialPatient, prescription.IsOk, prescription.PrescriptionCreationTime));
            Assert.Equal(expectedItem, Assert.Single(prescription.Items));
        }
    }

    private const string ActiveCentersPayload = """{"status":200,"family":"SUCCESSFUL","reason":"OK","traceId":"trace","data":[{"dbLinkId":"1377","hosName":"center","siamId":"masked","provinceName":"province"}]}""";

    [Fact]
    public void Maps_doctor_active_hospitals_and_clinics_for_production_and_pilot()
    {
        var expected = new DoctorActiveCenterDto("1377", "center", "masked", "province");
        foreach (var result in new[]
        {
            mapper.MapDoctorActiveCenters(Kiota(ActiveCentersPayload, ApiModels.DoctorActiveCenterListResponse.CreateFromDiscriminatorValue)),
            mapper.MapDoctorActiveCenters(Kiota(ActiveCentersPayload, PilotSoaModels.DoctorActiveCenterListResponse.CreateFromDiscriminatorValue))
        })
        {
            Assert.Equal((200, "SUCCESSFUL", "OK", "trace"), (result.Status, result.Family, result.Reason, result.TraceId));
            Assert.Equal(expected, Assert.Single(result.Data));
        }
    }

    [Fact]
    public void Preserves_head_EPRSC_ID_above_int_max_and_maps_the_complemantary_Msg_wire_name()
    {
        const long headerId = 3_000_000_000;
        var payload = $$"""{"head_EPRSC_ID":{{headerId}},"complemantary_Msg":"note","error_Code":"E1","error_Msg":"failed","trackingCode":12}""";
        var expected = new PrescriptionMutationResult(headerId, "note", "E1", "failed", 12);
        Assert.Equal(expected, mapper.MapCreatePrescription(Kiota(payload, SoaModels.PrescriptionMutationResponse.CreateFromDiscriminatorValue)));
        Assert.Equal(expected, mapper.MapCreatePrescription(Kiota(payload, PilotSoaModels.PrescriptionMutationResponse.CreateFromDiscriminatorValue)));
        Assert.Equal(expected, mapper.MapMarkPatientDisease(Kiota(payload, ApiModels.PrescriptionMutationResponse.CreateFromDiscriminatorValue)));
        Assert.Equal(expected, mapper.MapMarkPatientDisease(Kiota(payload, PilotApiModels.PrescriptionMutationResponse.CreateFromDiscriminatorValue)));
    }

    [Fact]
    public void Does_not_read_the_pre_v1_10_1_complementary_Msg_spelling()
    {
        var result = mapper.MapCreatePrescription(Kiota("""{"head_EPRSC_ID":1,"complementary_Msg":"old"}""", SoaModels.PrescriptionMutationResponse.CreateFromDiscriminatorValue));
        Assert.Null(result.ComplementaryMessage);
    }

    private static T Kiota<T>(string value, ParsableFactory<T> factory) where T : IParsable
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(value));
        var root = new JsonParseNodeFactory().GetRootParseNodeAsync("application/json", stream, CancellationToken.None).GetAwaiter().GetResult();
        return root.GetObjectValue(factory)!;
    }
}
