using System.Text.Json;
using System.Text.Json.Serialization;
using PilotModels = Tamin.Client.Soa.Pilot.Models;
using ProdModels = Tamin.Client.Soa.Prod.Models;

namespace Tamin.Integration.Mapping;

[JsonConverter(typeof(DentalServiceFlagJsonConverter))]
public enum DentalServiceFlag { Zero, One }

public sealed class TaminRequestValidationException(string fieldName, string diagnostic, Exception? innerException = null)
    : Exception($"Tamin request field '{fieldName}' is invalid: {diagnostic}", innerException)
{
    public string FieldName { get; } = fieldName;
    public string Diagnostic { get; } = diagnostic;
}

public sealed record PrescriptionTypeInput(int? PrescTypeId);
public sealed record DiagnosisIdInput(int? IcdId);
public sealed record ReferralIcd10Input(string? IcdId);
public sealed record ComplaintInput(string? ComplaintIDs);
public sealed record ReferralComplaintInput(string? ComplaintIDs, string? Id);
public sealed record SpecialInput(string? SpecCode);
public sealed record ReferralFeedbackAnswerInput(int? DrugAnswerId);
public sealed record ReferralFeedbackItemInput(ReferralFeedbackAnswerInput? AnswerId, string? Comments, int? QuestionId);
public sealed record ServiceInput(string? SrvCode, string? Terminology);
public sealed record DrugInstructionInput(int? DrugInstId);
public sealed record DrugAmountInput(int? DrugAmntId);
public sealed record PrescriptionDetailInput(
    string? DateDo, DiagnosisIdInput? DiagnosisID, string? Dose, DrugInstructionInput? DrugInstruction,
    string? IllnessId, DentalServiceFlag IsDentalService, IReadOnlyDictionary<string, object?>? NoteDetailDrug,
    long? NoteDetailsEprscId, string? PlanId, int? ReferenceStatus, string? Repeat, ServiceInput? SrvId,
    int? SrvQty, DrugAmountInput? TimesAday, string? ToothId);
public sealed record PrescriptionReferralInput(
    IReadOnlyList<ReferralComplaintInput>? Complaints, SpecialInput? DocSpecReferred,
    IReadOnlyList<ReferralIcd10Input>? Icd10s, long? Id, string? Message, long? NoteDetailsEprscId,
    string? PatientNationalCode, int? Quantity, int? ReferenceStatus,
    IReadOnlyList<ReferralFeedbackItemInput>? ReferralFeedbackList, string? ReferralHijriDate, string? SiamId);
public sealed record PrescriptionCreateInput(
    string? Comments, ComplaintInput? Complaint, string? CreatorType, string? DocId, string? DocMobileNo,
    string? DocNationalCode, string? ExpireDate, string Mobile,
    IReadOnlyList<PrescriptionDetailInput>? NoteDetailEprscs,
    IReadOnlyList<PrescriptionReferralInput>? NoteDetailsReferralList,
    string? Patient, string? PrescDate, PrescriptionTypeInput? PrescType, long? ReferralFeedbackId);

public sealed record DentistGridInput(ServiceInput? SrvId, string? ToothId);
public sealed record DentistRuleInput(IReadOnlyList<DentistGridInput>? AllGridData, string? DocId, string? PatientId, ServiceInput? SrvId, string? ToothId);

public interface IPrescriptionRequestMapper
{
    ProdModels.PrescriptionCreateRequest MapProduction(PrescriptionCreateInput input);
    PilotModels.PrescriptionCreateRequest MapPilot(PrescriptionCreateInput input);
    ProdModels.DentistRuleRequest MapProduction(DentistRuleInput input);
    PilotModels.DentistRuleRequest MapPilot(DentistRuleInput input);
}

public sealed class PrescriptionRequestMapper : IPrescriptionRequestMapper
{
    public ProdModels.PrescriptionCreateRequest MapProduction(PrescriptionCreateInput input)
    {
        Validate(input);
        return new()
        {
            Comments = input.Comments, Complaint = Map(input.Complaint), CreatorType = input.CreatorType,
            DocId = input.DocId, DocMobileNo = input.DocMobileNo, DocNationalCode = input.DocNationalCode,
            ExpireDate = input.ExpireDate, Mobile = input.Mobile,
            // D-05: these are the only two canonical collection names emitted.
            NoteDetailEprscs = input.NoteDetailEprscs?.Select(Map).ToList(),
            NoteDetailsReferralList = input.NoteDetailsReferralList?.Select(Map).ToList(),
            Patient = input.Patient, PrescDate = input.PrescDate,
            PrescType = input.PrescType is null ? null : new() { PrescTypeId = input.PrescType.PrescTypeId },
            ReferralFeedbackId = input.ReferralFeedbackId
        };
    }

    public PilotModels.PrescriptionCreateRequest MapPilot(PrescriptionCreateInput input)
    {
        Validate(input);
        return new()
        {
            // Pilot contract requires the doctor's national code as clientId.
            ClientId = input.DocNationalCode,
            Comments = input.Comments, Complaint = MapPilot(input.Complaint), CreatorType = input.CreatorType,
            DocId = input.DocId, DocMobileNo = input.DocMobileNo, DocNationalCode = input.DocNationalCode,
            ExpireDate = input.ExpireDate, Mobile = input.Mobile,
            // D-05: these are the only two canonical collection names emitted.
            NoteDetailEprscs = input.NoteDetailEprscs?.Select(MapPilot).ToList(),
            NoteDetailsReferralList = input.NoteDetailsReferralList?.Select(MapPilot).ToList(),
            Patient = input.Patient, PrescDate = input.PrescDate,
            PrescType = input.PrescType is null ? null : new() { PrescTypeId = input.PrescType.PrescTypeId },
            ReferralFeedbackId = input.ReferralFeedbackId
        };
    }

    public ProdModels.DentistRuleRequest MapProduction(DentistRuleInput input) => new()
    {
        // D-08: allGridData is a collection, as stated by the provider prose.
        AllGridData = input.AllGridData?.Select(x => new ProdModels.GridData { SrvId = Map(x.SrvId), ToothId = x.ToothId }).ToList(),
        DocId = input.DocId, PatientId = input.PatientId, SrvId = Map(input.SrvId), ToothId = input.ToothId
    };

    public PilotModels.DentistRuleRequest MapPilot(DentistRuleInput input) => new()
    {
        // D-08: allGridData is a collection, as stated by the provider prose.
        AllGridData = input.AllGridData?.Select(x => new PilotModels.GridData { SrvId = MapPilot(x.SrvId), ToothId = x.ToothId }).ToList(),
        DocId = input.DocId, PatientId = input.PatientId, SrvId = MapPilot(input.SrvId), ToothId = input.ToothId
    };

    private static void Validate(PrescriptionCreateInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        // D-06: presence is known; no undocumented mobile regex is invented.
        if (string.IsNullOrWhiteSpace(input.Mobile)) throw new TaminRequestValidationException("mobile", "a nonblank value is required");
    }

    private static ProdModels.NoteDetailEprsc Map(PrescriptionDetailInput x) => new()
    {
        DateDo = x.DateDo, DiagnosisID = x.DiagnosisID is null ? null : new() { IcdId = x.DiagnosisID.IcdId },
        Dose = x.Dose, DrugInstruction = x.DrugInstruction is null ? null : new() { DrugInstId = x.DrugInstruction.DrugInstId },
        // D-07: the normalized flag maps to the generated wire enum without coercing other values.
        IllnessId = x.IllnessId, IsDentalService = x.IsDentalService == DentalServiceFlag.Zero ? ProdModels.NoteDetailEprsc_isDentalService.Zero : ProdModels.NoteDetailEprsc_isDentalService.One,
        NoteDetailDrug = JsonObject<ProdModels.JsonObject>(x.NoteDetailDrug), NoteDetailsEprscId = x.NoteDetailsEprscId,
        PlanId = x.PlanId, ReferenceStatus = x.ReferenceStatus, Repeat = x.Repeat, SrvId = Map(x.SrvId), SrvQty = x.SrvQty,
        TimesAday = x.TimesAday is null ? null : new() { DrugAmntId = x.TimesAday.DrugAmntId }, ToothId = x.ToothId
    };

    private static PilotModels.NoteDetailEprsc MapPilot(PrescriptionDetailInput x) => new()
    {
        DateDo = x.DateDo, DiagnosisID = x.DiagnosisID is null ? null : new() { IcdId = x.DiagnosisID.IcdId },
        Dose = x.Dose, DrugInstruction = x.DrugInstruction is null ? null : new() { DrugInstId = x.DrugInstruction.DrugInstId },
        // D-07: the normalized flag maps to the generated wire enum without coercing other values.
        IllnessId = x.IllnessId, IsDentalService = x.IsDentalService == DentalServiceFlag.Zero ? PilotModels.NoteDetailEprsc_isDentalService.Zero : PilotModels.NoteDetailEprsc_isDentalService.One,
        NoteDetailDrug = JsonObject<PilotModels.JsonObject>(x.NoteDetailDrug), NoteDetailsEprscId = x.NoteDetailsEprscId,
        PlanId = x.PlanId, ReferenceStatus = x.ReferenceStatus, Repeat = x.Repeat, SrvId = MapPilot(x.SrvId), SrvQty = x.SrvQty,
        TimesAday = x.TimesAday is null ? null : new() { DrugAmntId = x.TimesAday.DrugAmntId }, ToothId = x.ToothId
    };

    // D-09 through D-11: independent referral date/id, complaint members, and string ICD-10 codes are mapped without precedence or coercion.
    private static ProdModels.NoteDetailsReferral Map(PrescriptionReferralInput x) => new()
    {
        Complaints = x.Complaints?.Select(c => new ProdModels.ReferralComplaint { ComplaintIDs = c.ComplaintIDs, Id = c.Id }).ToList(),
        DocSpecReferred = x.DocSpecReferred is null ? null : new() { SpecCode = x.DocSpecReferred.SpecCode },
        Icd10s = x.Icd10s?.Select(i => new ProdModels.ReferralIcd10 { IcdId = i.IcdId }).ToList(), Id = x.Id, Message = x.Message,
        NoteDetailsEprscId = x.NoteDetailsEprscId, PatientNationalCode = x.PatientNationalCode, Quantity = x.Quantity,
        ReferenceStatus = x.ReferenceStatus, ReferralFeedbackList = x.ReferralFeedbackList?.Select(Map).ToList(), ReferralHijriDate = x.ReferralHijriDate, SiamId = x.SiamId
    };
    private static PilotModels.NoteDetailsReferral MapPilot(PrescriptionReferralInput x) => new()
    {
        Complaints = x.Complaints?.Select(c => new PilotModels.ReferralComplaint { ComplaintIDs = c.ComplaintIDs, Id = c.Id }).ToList(),
        DocSpecReferred = x.DocSpecReferred is null ? null : new() { SpecCode = x.DocSpecReferred.SpecCode },
        Icd10s = x.Icd10s?.Select(i => new PilotModels.ReferralIcd10 { IcdId = i.IcdId }).ToList(), Id = x.Id, Message = x.Message,
        NoteDetailsEprscId = x.NoteDetailsEprscId, PatientNationalCode = x.PatientNationalCode, Quantity = x.Quantity,
        ReferenceStatus = x.ReferenceStatus, ReferralFeedbackList = x.ReferralFeedbackList?.Select(MapPilot).ToList(), ReferralHijriDate = x.ReferralHijriDate, SiamId = x.SiamId
    };
    private static ProdModels.ReferralFeedbackItem Map(ReferralFeedbackItemInput x) => new() { AnswerId = x.AnswerId is null ? null : new() { DrugAnswerId = x.AnswerId.DrugAnswerId }, Comments = x.Comments, QuestionId = x.QuestionId };
    private static PilotModels.ReferralFeedbackItem MapPilot(ReferralFeedbackItemInput x) => new() { AnswerId = x.AnswerId is null ? null : new() { DrugAnswerId = x.AnswerId.DrugAnswerId }, Comments = x.Comments, QuestionId = x.QuestionId };
    private static ProdModels.Complaint? Map(ComplaintInput? x) => x is null ? null : new() { ComplaintIDs = x.ComplaintIDs };
    private static PilotModels.Complaint? MapPilot(ComplaintInput? x) => x is null ? null : new() { ComplaintIDs = x.ComplaintIDs };
    private static ProdModels.Service? Map(ServiceInput? x) => x is null ? null : new() { SrvCode = x.SrvCode, Terminology = x.Terminology };
    private static PilotModels.Service? MapPilot(ServiceInput? x) => x is null ? null : new() { SrvCode = x.SrvCode, Terminology = x.Terminology };
    private static T? JsonObject<T>(IReadOnlyDictionary<string, object?>? values) where T : Microsoft.Kiota.Abstractions.Serialization.IAdditionalDataHolder, new()
    {
        if (values is null) return default;
        var result = new T();
        foreach (var pair in values) result.AdditionalData[pair.Key] = pair.Value!;
        return result;
    }
}

// D-07: normalize the two documented string and numeric representations at the transport boundary.
public sealed class DentalServiceFlagJsonConverter : JsonConverter<DentalServiceFlag>
{
    public override bool HandleNull => true;
    public override DentalServiceFlag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return reader.GetString() switch { "0" => DentalServiceFlag.Zero, "1" => DentalServiceFlag.One, _ => throw Invalid() };
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value))
            return value switch { 0 => DentalServiceFlag.Zero, 1 => DentalServiceFlag.One, _ => throw Invalid() };
        throw Invalid();
    }
    public override void Write(Utf8JsonWriter writer, DentalServiceFlag value, JsonSerializerOptions options) => writer.WriteStringValue(value == DentalServiceFlag.Zero ? "0" : "1");
    private static TaminRequestValidationException Invalid() => new("isDentalService", "expected string or number 0 or 1");
}
