using System.Text.Json;
using ApiModels = Tamin.Client.Api.Prod.Models;
using SoaModels = Tamin.Client.Soa.Prod.Models;
using PilotApiModels = Tamin.Client.Api.Pilot.Models;
using PilotSoaModels = Tamin.Client.Soa.Pilot.Models;

namespace Tamin.Integration.Mapping;

public sealed record UnconstrainedResponse(JsonElement Payload);
public sealed record TaminEnvelope<T>(int? Status, string? Family, string? Reason, string? TraceId, T Data);
public sealed record ReferralCartableItemDto(string? NoteHeadId, string? ReferralId, string? PatientId, string? DoctorId, string? PrescriptionDate, string? ReferralDate);
public sealed record ReferralCartableDto(int? Total, IReadOnlyList<ReferralCartableItemDto> Items, long? Id, long? MasterParent, long? MasterChild, long? ReferralDate);
public sealed record PrescriptionTypeDto(int? Id, string? Code, string? Description);
public sealed record ReferralFeedbackPrescriptionDto(long? NoteHeadEprscId, string? Patient, string? PrescriptionDate, JsonElement Items, JsonElement? Referrals, PrescriptionTypeDto? PrescriptionType);
public sealed record ReferralFeedbackDetailsDto(int? Total, IReadOnlyList<ReferralFeedbackPrescriptionDto> Items);
public sealed record OpenReferralSpecialtyDto(string? SpecCode, string? SpecDesc, string? SpecGroup, string? Status, string? StatusDate, string? LStatus);
public sealed record PatientOpenReferralsDto(int? Count, IReadOnlyList<OpenReferralSpecialtyDto> Referrals);
public sealed record HospitalizationOrderDto(long? Id, long? MasterParent, long? ReferralDate, string? DoctorId, int? Quantity, bool? DoctorFamily);
public sealed record FamilyDoctorShareDto(string? Price, bool? FamilyDoctor);
public sealed record PatientDiseaseDto(int? Id, string? IllText, string? Status, string? Type, string? CodeMapping, string? IllName, int? PriorityOrder, string? StatusDate);
public sealed record PatientDiseaseListDto(int? Total, IReadOnlyList<PatientDiseaseDto> Diseases);
public sealed record PrescriptionMutationResult(long? HeaderId, string? ComplementaryMessage, string? ErrorCode, string? ErrorMessage, long? TrackingCode);

public interface IPrescriptionReferenceDataService
{
    UnconstrainedResponse MapPrescriptionTypes(SoaModels.JsonObject response);
    UnconstrainedResponse MapPrescriptionServiceTypes(SoaModels.JsonObject response);
    UnconstrainedResponse MapServices(SoaModels.JsonObject response);
    UnconstrainedResponse MapLaboratoryTariffGroups(SoaModels.JsonObject response);
    UnconstrainedResponse MapDrugAmounts(SoaModels.JsonObject response);
    UnconstrainedResponse MapDrugUsages(SoaModels.JsonObject response);
    UnconstrainedResponse MapDrugInstructions(SoaModels.JsonObject response);
    UnconstrainedResponse MapPhysiotherapyPlans(SoaModels.JsonObject response);
    UnconstrainedResponse MapPhysiotherapyIllnesses(SoaModels.JsonObject response);
    UnconstrainedResponse MapComplaints(SoaModels.JsonObject response);
    UnconstrainedResponse MapIcd10Diagnoses(SoaModels.JsonObject response);
    UnconstrainedResponse MapDoctorSpecialties(SoaModels.JsonObject response);
    UnconstrainedResponse MapDentistRules(SoaModels.JsonObject response);
    UnconstrainedResponse MapDentistServicesWithoutTooth(SoaModels.JsonObject response);
    UnconstrainedResponse MapDentistServicesByTooth(SoaModels.JsonObject response);
    UnconstrainedResponse MapPilotReferenceData(PilotSoaModels.JsonObject response);
}
public interface IPrescriptionMutationService
{
    PrescriptionMutationResult MapCreatePrescription(SoaModels.PrescriptionMutationResponse response);
    UnconstrainedResponse MapPatientEntitlement(SoaModels.JsonObject response);
    UnconstrainedResponse MapPrescriptionDetails(SoaModels.JsonObject response);
    UnconstrainedResponse MapDeletePrescription(SoaModels.JsonObject response);
    UnconstrainedResponse MapUpdatePrescription(SoaModels.JsonObject response);
    PrescriptionMutationResult MapCreatePrescription(PilotSoaModels.PrescriptionMutationResponse response);
    UnconstrainedResponse MapPilotUndocumentedMutation(PilotSoaModels.JsonObject response);
}
public interface IReferralService
{
    TaminEnvelope<ReferralCartableDto> MapReferralCartable(SoaModels.ReferralCartableResponse response);
    TaminEnvelope<ReferralFeedbackDetailsDto> MapReferralFeedbackDetails(SoaModels.ReferralFeedbackDetailResponse response);
    TaminEnvelope<PatientOpenReferralsDto> MapPatientOpenReferrals(SoaModels.PatientOpenReferralsResponse response);
    TaminEnvelope<ReferralCartableDto> MapReferralCartable(JsonElement response);
    TaminEnvelope<ReferralFeedbackDetailsDto> MapReferralFeedbackDetails(JsonElement response);
    TaminEnvelope<PatientOpenReferralsDto> MapPatientOpenReferrals(JsonElement response);
    UnconstrainedResponse MapOpenReferralCount(SoaModels.JsonObject response);
    UnconstrainedResponse MapReferralPrescription(SoaModels.JsonObject response);
    UnconstrainedResponse MapReferralFeedbackPrescriptions(SoaModels.JsonObject response);
    UnconstrainedResponse MapRecentDoctorReferrals(SoaModels.JsonObject response);
    UnconstrainedResponse MapReferralFeedbackQuestions(ApiModels.JsonObject response);
    UnconstrainedResponse MapReferralFeedbackAnswers(ApiModels.JsonObject response);
    UnconstrainedResponse MapReferredServicePrescription(SoaModels.JsonObject response);
    TaminEnvelope<IReadOnlyList<HospitalizationOrderDto>> MapHospitalizationOrders(JsonElement response);
    TaminEnvelope<IReadOnlyList<HospitalizationOrderDto>> MapHospitalizationOrders(SoaModels.HospitalizationOrderResponse response);
    TaminEnvelope<FamilyDoctorShareDto> MapFamilyDoctorShare(JsonElement response);
    TaminEnvelope<FamilyDoctorShareDto> MapFamilyDoctorShare(ApiModels.FamilyDoctorShareResponse response);
    TaminEnvelope<ReferralCartableDto> MapReferralCartable(PilotSoaModels.ReferralCartableResponse response);
    TaminEnvelope<ReferralFeedbackDetailsDto> MapReferralFeedbackDetails(PilotSoaModels.ReferralFeedbackDetailResponse response);
    TaminEnvelope<PatientOpenReferralsDto> MapPatientOpenReferrals(PilotSoaModels.PatientOpenReferralsResponse response);
    TaminEnvelope<IReadOnlyList<HospitalizationOrderDto>> MapHospitalizationOrders(PilotSoaModels.HospitalizationOrderResponse response);
    TaminEnvelope<FamilyDoctorShareDto> MapFamilyDoctorShare(PilotApiModels.FamilyDoctorShareResponse response);
    UnconstrainedResponse MapPilotSoaUndocumented(PilotSoaModels.JsonObject response);
    UnconstrainedResponse MapPilotApiUndocumented(PilotApiModels.JsonObject response);
}
public interface INursingService
{
    UnconstrainedResponse MapUnclaimedPrescriptions(SoaModels.JsonObject response);
    UnconstrainedResponse MapSaveActions(SoaModels.JsonObject response);
    UnconstrainedResponse MapPilotNursingResponse(PilotSoaModels.JsonObject response);
}
public interface IPatientDiseaseService
{
    TaminEnvelope<PatientDiseaseListDto> MapPatientDiseases(JsonElement response);
    TaminEnvelope<PatientDiseaseListDto> MapPatientDiseases(ApiModels.PatientDiseaseListResponse response);
    PrescriptionMutationResult MapMarkPatientDisease(ApiModels.PrescriptionMutationResponse response);
    TaminEnvelope<PatientDiseaseListDto> MapPatientDiseases(PilotApiModels.PatientDiseaseListResponse response);
    PrescriptionMutationResult MapMarkPatientDisease(PilotApiModels.PrescriptionMutationResponse response);
}

public sealed class TaminMappingService : IPrescriptionReferenceDataService, IPrescriptionMutationService, IReferralService, INursingService, IPatientDiseaseService
{
    // The 29 undocumented response shapes deliberately remain an opaque DTO rather than generated JsonObject.
    public UnconstrainedResponse MapPrescriptionTypes(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapPrescriptionServiceTypes(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapServices(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapLaboratoryTariffGroups(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapDrugAmounts(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapDrugUsages(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapDrugInstructions(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapPhysiotherapyPlans(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapPhysiotherapyIllnesses(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapComplaints(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapIcd10Diagnoses(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapDoctorSpecialties(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapDentistRules(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapDentistServicesWithoutTooth(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapDentistServicesByTooth(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapPilotReferenceData(PilotSoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapPatientEntitlement(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapPrescriptionDetails(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapDeletePrescription(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapUpdatePrescription(SoaModels.JsonObject response) => Opaque(response); // Audit assumption 4: documented raw-array request does not define a result.
    public UnconstrainedResponse MapOpenReferralCount(SoaModels.JsonObject response) => Opaque(response); // D-19: no names, types, or envelope may be inferred.
    public UnconstrainedResponse MapReferralPrescription(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapReferralFeedbackPrescriptions(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapRecentDoctorReferrals(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapReferralFeedbackQuestions(ApiModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapReferralFeedbackAnswers(ApiModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapReferredServicePrescription(SoaModels.JsonObject response) => Opaque(response); // D-15/D-17/D-18: route quirks do not justify a response shape.
    public UnconstrainedResponse MapUnclaimedPrescriptions(SoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapSaveActions(SoaModels.JsonObject response) => Opaque(response); // D-16: the unusual /ep/api/ route does not imply a response schema.
    public UnconstrainedResponse MapPilotUndocumentedMutation(PilotSoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapPilotSoaUndocumented(PilotSoaModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapPilotApiUndocumented(PilotApiModels.JsonObject response) => Opaque(response);
    public UnconstrainedResponse MapPilotNursingResponse(PilotSoaModels.JsonObject response) => Opaque(response); // D-16: preserves the pilot route without inferring a body.

    public PrescriptionMutationResult MapCreatePrescription(SoaModels.PrescriptionMutationResponse response) => new(response.HeadEPRSCID, response.ComplementaryMsg, response.ErrorCode, response.ErrorMsg, response.TrackingCode);
    public PrescriptionMutationResult MapMarkPatientDisease(ApiModels.PrescriptionMutationResponse response) => new(response.HeadEPRSCID, response.ComplementaryMsg, response.ErrorCode, response.ErrorMsg, response.TrackingCode); // D-20: reuse only the documented optional mutation fields.
    public PrescriptionMutationResult MapCreatePrescription(PilotSoaModels.PrescriptionMutationResponse response) => new(response.HeadEPRSCID, response.ComplementaryMsg, response.ErrorCode, response.ErrorMsg, response.TrackingCode);
    public PrescriptionMutationResult MapMarkPatientDisease(PilotApiModels.PrescriptionMutationResponse response) => new(response.HeadEPRSCID, response.ComplementaryMsg, response.ErrorCode, response.ErrorMsg, response.TrackingCode); // D-20: pilot uses the same optional mutation fields.

    public TaminEnvelope<ReferralCartableDto> MapReferralCartable(JsonElement response)
    {
        var data = Object(response, "data");
        // D-12: patientID has an untrusted description, so it remains a string with no semantic reinterpretation.
        var items = Array(data, "list").Select(item => new ReferralCartableItemDto(String(item, "noteHeadId"), String(item, "referralId"), String(item, "patientID"), String(item, "docID"), String(item, "prescDate"), String(item, "referralDate"))).ToArray();
        return Envelope(response, new ReferralCartableDto(Int(data, "total"), items, Long(data, "id"), Long(data, "masterParent"), Long(data, "masterChild"), Long(data, "referralDate")));
    }
    public TaminEnvelope<ReferralCartableDto> MapReferralCartable(SoaModels.ReferralCartableResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var data = response.Data;
        // D-12: patientID has an untrusted description, so it remains a string with no semantic reinterpretation.
        var items = data?.List?.Select(item => new ReferralCartableItemDto(item.NoteHeadId, item.ReferralId, item.PatientID, item.DocID, item.PrescDate, item.ReferralDate)).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, null, new(data?.Total, items, data?.Id, data?.MasterParent, data?.MasterChild, data?.ReferralDate));
    }
    public TaminEnvelope<ReferralCartableDto> MapReferralCartable(PilotSoaModels.ReferralCartableResponse response)
    {
        var data = response.Data; // D-12: patientID semantics remain untrusted in pilot too.
        var items = data?.List?.Select(item => new ReferralCartableItemDto(item.NoteHeadId, item.ReferralId, item.PatientID, item.DocID, item.PrescDate, item.ReferralDate)).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, null, new(data?.Total, items, data?.Id, data?.MasterParent, data?.MasterChild, data?.ReferralDate));
    }
    public TaminEnvelope<ReferralFeedbackDetailsDto> MapReferralFeedbackDetails(JsonElement response)
    {
        // D-13: mapping follows the declared v7 operation, not the conflicting v2 invocation examples.
        var data = Object(response, "data");
        var items = Array(data, "list").Select(item => new ReferralFeedbackPrescriptionDto(Long(item, "noteHeadEprscId"), String(item, "patient"), String(item, "prescDate"), Property(item, "noteDetailEprscs"), OptionalProperty(item, "noteDetailsReferralList"), MapPrescriptionType(OptionalProperty(item, "prescType")))).ToArray();
        return Envelope(response, new ReferralFeedbackDetailsDto(Int(data, "total"), items));
    }
    public TaminEnvelope<ReferralFeedbackDetailsDto> MapReferralFeedbackDetails(SoaModels.ReferralFeedbackDetailResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        // D-13: mapping follows the declared v7 operation, not the conflicting v2 invocation examples.
        var items = response.Data?.List?.Select(item => new ReferralFeedbackPrescriptionDto(item.NoteHeadEprscId, item.Patient, item.PrescDate, JsonSerializer.SerializeToElement(item.NoteDetailEprscs), item.NoteDetailsReferralList is null ? null : JsonSerializer.SerializeToElement(item.NoteDetailsReferralList), item.PrescType is null ? null : new(item.PrescType.PrescTypeId, item.PrescType.PrescTypeCode, item.PrescType.PrescTypeDesc))).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, null, new(response.Data?.Total, items));
    }
    public TaminEnvelope<ReferralFeedbackDetailsDto> MapReferralFeedbackDetails(PilotSoaModels.ReferralFeedbackDetailResponse response)
    {
        // D-13: the pilot mapper follows its generated declared route rather than the conflicting production invocation examples.
        var items = response.Data?.List?.Select(item => new ReferralFeedbackPrescriptionDto(item.NoteHeadEprscId, item.Patient, item.PrescDate, JsonSerializer.SerializeToElement(item.NoteDetailEprscs), item.NoteDetailsReferralList is null ? null : JsonSerializer.SerializeToElement(item.NoteDetailsReferralList), item.PrescType is null ? null : new(item.PrescType.PrescTypeId, item.PrescType.PrescTypeCode, item.PrescType.PrescTypeDesc))).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, null, new(response.Data?.Total, items));
    }
    public TaminEnvelope<PatientOpenReferralsDto> MapPatientOpenReferrals(JsonElement response)
    {
        var data = Object(response, "data");
        var referrals = Array(data, "noteDetailsReferrals").Select(item => new OpenReferralSpecialtyDto(String(item, "specCode"), String(item, "specDesc"), String(item, "specGRP"), String(item, "status"), String(item, "statusstDate"), String(item, "lstatus"))).ToArray();
        return Envelope(response, new PatientOpenReferralsDto(Int(data, "count"), referrals));
    }
    public TaminEnvelope<PatientOpenReferralsDto> MapPatientOpenReferrals(SoaModels.PatientOpenReferralsResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var referrals = response.Data?.NoteDetailsReferrals?.Select(item => new OpenReferralSpecialtyDto(item.SpecCode, item.SpecDesc, item.SpecGRP, item.Status, item.StatusstDate, item.Lstatus)).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, null, new(response.Data?.Count, referrals));
    }
    public TaminEnvelope<PatientOpenReferralsDto> MapPatientOpenReferrals(PilotSoaModels.PatientOpenReferralsResponse response)
    {
        var referrals = response.Data?.NoteDetailsReferrals?.Select(item => new OpenReferralSpecialtyDto(item.SpecCode, item.SpecDesc, item.SpecGRP, item.Status, item.StatusstDate, item.Lstatus)).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, null, new(response.Data?.Count, referrals));
    }
    public TaminEnvelope<IReadOnlyList<HospitalizationOrderDto>> MapHospitalizationOrders(JsonElement response)
    {
        var data = Array(response, "data");
        return Envelope(response, (IReadOnlyList<HospitalizationOrderDto>)data.Select(item => new HospitalizationOrderDto(Long(item, "id"), Long(item, "masterParent"), Long(item, "referralDate"), String(item, "docId"), Int(item, "quantity"), Bool(item, "docFamily"))).ToArray());
    }
    public TaminEnvelope<IReadOnlyList<HospitalizationOrderDto>> MapHospitalizationOrders(SoaModels.HospitalizationOrderResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var data = response.Data?.Select(item => new HospitalizationOrderDto(item.Id, item.MasterParent, item.ReferralDate, item.DocId, item.Quantity, item.DocFamily)).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, response.TraceId, data);
    }
    public TaminEnvelope<IReadOnlyList<HospitalizationOrderDto>> MapHospitalizationOrders(PilotSoaModels.HospitalizationOrderResponse response)
    {
        var data = response.Data?.Select(item => new HospitalizationOrderDto(item.Id, item.MasterParent, item.ReferralDate, item.DocId, item.Quantity, item.DocFamily)).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, response.TraceId, data);
    }
    public TaminEnvelope<FamilyDoctorShareDto> MapFamilyDoctorShare(JsonElement response)
    {
        var data = Object(response, "data");
        return Envelope(response, new FamilyDoctorShareDto(String(data, "price"), Bool(data, "familyDoctor")));
    }
    public TaminEnvelope<FamilyDoctorShareDto> MapFamilyDoctorShare(ApiModels.FamilyDoctorShareResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new(response.Status, response.Family, response.Reason, response.TraceId, new(response.Data?.Price, response.Data?.FamilyDoctor));
    }
    public TaminEnvelope<FamilyDoctorShareDto> MapFamilyDoctorShare(PilotApiModels.FamilyDoctorShareResponse response) => new(response.Status, response.Family, response.Reason, response.TraceId, new(response.Data?.Price, response.Data?.FamilyDoctor));
    public TaminEnvelope<PatientDiseaseListDto> MapPatientDiseases(JsonElement response)
    {
        var data = Object(response, "data");
        var diseases = Array(data, "list").Select(item => new PatientDiseaseDto(Int(item, "id"), String(item, "illText"), String(item, "status"), String(item, "type"), String(item, "codeMapping"), String(item, "illName"), Int(item, "priorityOrder"), String(item, "statusDate"))).ToArray();
        return Envelope(response, new PatientDiseaseListDto(Int(data, "total"), diseases));
    }
    public TaminEnvelope<PatientDiseaseListDto> MapPatientDiseases(ApiModels.PatientDiseaseListResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var diseases = response.Data?.List?.Select(item => new PatientDiseaseDto(item.Id, item.IllText, item.Status, item.Type, item.CodeMapping, item.IllName, item.PriorityOrder, item.StatusDate)).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, response.TraceId, new(response.Data?.Total, diseases));
    }
    public TaminEnvelope<PatientDiseaseListDto> MapPatientDiseases(PilotApiModels.PatientDiseaseListResponse response)
    {
        var diseases = response.Data?.List?.Select(item => new PatientDiseaseDto(item.Id, item.IllText, item.Status, item.Type, item.CodeMapping, item.IllName, item.PriorityOrder, item.StatusDate)).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, response.TraceId, new(response.Data?.Total, diseases));
    }

    private static UnconstrainedResponse Opaque(SoaModels.JsonObject response) => new(JsonSerializer.SerializeToElement(response.AdditionalData));
    private static UnconstrainedResponse Opaque(ApiModels.JsonObject response) => new(JsonSerializer.SerializeToElement(response.AdditionalData));
    private static UnconstrainedResponse Opaque(PilotSoaModels.JsonObject response) => new(JsonSerializer.SerializeToElement(response.AdditionalData));
    private static UnconstrainedResponse Opaque(PilotApiModels.JsonObject response) => new(JsonSerializer.SerializeToElement(response.AdditionalData));
    private static PrescriptionMutationResult MapMutation(JsonElement response) => new(Long(response, "head_EPRSC_ID"), String(response, "complementary_Msg"), String(response, "error_Code"), String(response, "error_Msg"), Long(response, "trackingCode"));
    private static TaminEnvelope<T> Envelope<T>(JsonElement response, T data) => new(Int(response, "status"), String(response, "family"), String(response, "reason"), String(response, "traceId"), data);
    private static JsonElement Object(JsonElement element, string name) => Property(element, name);
    private static IEnumerable<JsonElement> Array(JsonElement element, string name) => OptionalProperty(element, name) is { ValueKind: JsonValueKind.Array } value ? value.EnumerateArray() : [];
    private static JsonElement Property(JsonElement element, string name) => element.TryGetProperty(name, out var value) ? value : default;
    private static JsonElement? OptionalProperty(JsonElement element, string name) => element.TryGetProperty(name, out var value) ? value.Clone() : null;
    private static string? String(JsonElement element, string name) => OptionalProperty(element, name) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;
    private static int? Int(JsonElement element, string name) => OptionalProperty(element, name) is { ValueKind: JsonValueKind.Number } value && value.TryGetInt32(out var result) ? result : null;
    private static long? Long(JsonElement element, string name) => OptionalProperty(element, name) is { ValueKind: JsonValueKind.Number } value && value.TryGetInt64(out var result) ? result : null;
    private static bool? Bool(JsonElement element, string name) => OptionalProperty(element, name) is { ValueKind: JsonValueKind.True } ? true : OptionalProperty(element, name) is { ValueKind: JsonValueKind.False } ? false : null;
    private static PrescriptionTypeDto? MapPrescriptionType(JsonElement? value) => value is { ValueKind: JsonValueKind.Object } item
        ? new(Int(item, "prescTypeId"), String(item, "prescTypeCode"), String(item, "prescTypeDesc"))
        : null;
}
