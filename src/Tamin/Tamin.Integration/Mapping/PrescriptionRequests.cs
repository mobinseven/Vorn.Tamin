using System.Text.Json;
using System.Text.Json.Serialization;
using PilotModels = Tamin.Client.Soa.Pilot.Models;
using ProdModels = Tamin.Client.Soa.Prod.Models;
using PilotApiModels = Tamin.Client.Api.Pilot.Models;
using ProdApiModels = Tamin.Client.Api.Prod.Models;

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
public sealed record SpecialInput(string? SpecCode, string? Terminology = null);
public sealed record MethodUsageInput(int? Id);
public sealed record ReferralFeedbackAnswerInput(int? DrugAnswerId);
public sealed record ReferralFeedbackItemInput(ReferralFeedbackAnswerInput? AnswerId, string? Comments, int? QuestionId);
/// <param name="SrvType">The service type code the guide requires on every item, such as "01" for a drug, "16" for a visit or "17" for a medical service.</param>
public sealed record ServiceInput(string? SrvCode, string? Terminology, string? SrvType = null);
public sealed record DrugInstructionInput(int? DrugInstId);
public sealed record DrugAmountInput(int? DrugAmntId);
public sealed record PrescriptionDetailInput(
    string? DateDo, string? Dose, string? DoseCode, DrugInstructionInput? DrugInstruction,
    string? IllnessId, DentalServiceFlag IsDentalService, MethodUsageInput? MethodUsage, DiagnosisIdInput? NoteDetailDrug,
    long? NoteDetailsEprscId, string? PlanId, int? ReferenceStatus, string? Repeat, ServiceInput? SrvId,
    int? SrvQty, DrugAmountInput? TimesAday, string? ToothId);
public sealed record PrescriptionReferralInput(
    IReadOnlyList<ReferralComplaintInput>? Complaints, SpecialInput? DocSpecReferred,
    IReadOnlyList<ReferralIcd10Input>? Icd10s, long? Id, string? Message, long? NoteDetailsEprscId,
    string? PatientNationalCode, int? Quantity, int? ReferenceStatus,
    IReadOnlyList<ReferralFeedbackItemInput>? ReferralFeedbackList, string? ReferralHijriDate, string? SiamId);
public sealed record PrescriptionCreateInput(
    string? Comments, string? CreatorType, string? DocId, string? DocMobileNo,
    string? DocNationalCode, string? ExpireDate, string? Mobile,
    IReadOnlyList<PrescriptionDetailInput>? NoteDetailEprscs,
    IReadOnlyList<PrescriptionReferralInput>? NoteDetailsReferralList,
    ComplaintInput? NoteHeadEprscSupplementary,
    string? Patient, string? PrescDate, PrescriptionTypeInput? PrescType, long? ReferralFeedbackId,
    string? SiamId, string? Terminology);

public sealed record DentistGridInput(ServiceInput? SrvId, string? ToothId);
public sealed record DentistRuleInput(IReadOnlyList<DentistGridInput>? AllGridData, string? DocId, string? PatientId, ServiceInput? SrvId, string? ToothId);
public sealed record NursingActionInput(string SiamId, IReadOnlyList<long> NoteDetailsEprscIds, string NurseNationalCode);
public enum PatientIllnessType { One = 1, Two = 2, Three = 3 }
public sealed record PatientIllnessInput(string PatientNationalCode, int IllId, string MobileNumber, string SiamId, PatientIllnessType? Type = null);

internal sealed class PrescriptionRequestMapper
{
    public ProdModels.PrescriptionCreateRequest MapProduction(PrescriptionCreateInput input)
    {
        Validate(input);
        return new()
        {
            Comments = input.Comments, CreatorType = input.CreatorType,
            DocId = input.DocId, DocMobileNo = input.DocMobileNo, DocNationalCode = input.DocNationalCode,
            ExpireDate = input.ExpireDate, Mobile = input.Mobile,
            // D-05: these are the only two canonical collection names emitted.
            NoteDetailEprscs = input.NoteDetailEprscs?.Select(Map).ToList(),
            NoteDetailsReferralList = input.NoteDetailsReferralList?.Select(Map).ToList(),
            NoteHeadEprscSupplementary = Map(input.NoteHeadEprscSupplementary),
            Patient = input.Patient, PrescDate = input.PrescDate,
            PrescType = input.PrescType is null ? null : new() { PrescTypeId = input.PrescType.PrescTypeId },
            ReferralFeedbackId = input.ReferralFeedbackId, SiamId = input.SiamId, Terminology = input.Terminology
        };
    }

    public PilotModels.PrescriptionCreateRequest MapPilot(PrescriptionCreateInput input)
    {
        Validate(input);
        return new()
        {
            // Pilot contract requires the doctor's national code as clientId.
            ClientId = input.DocNationalCode,
            Comments = input.Comments, CreatorType = input.CreatorType,
            DocId = input.DocId, DocMobileNo = input.DocMobileNo, DocNationalCode = input.DocNationalCode,
            ExpireDate = input.ExpireDate, Mobile = input.Mobile,
            // D-05: these are the only two canonical collection names emitted.
            NoteDetailEprscs = input.NoteDetailEprscs?.Select(MapPilot).ToList(),
            NoteDetailsReferralList = input.NoteDetailsReferralList?.Select(MapPilot).ToList(),
            NoteHeadEprscSupplementary = MapPilot(input.NoteHeadEprscSupplementary),
            Patient = input.Patient, PrescDate = input.PrescDate,
            PrescType = input.PrescType is null ? null : new() { PrescTypeId = input.PrescType.PrescTypeId },
            ReferralFeedbackId = input.ReferralFeedbackId, SiamId = input.SiamId, Terminology = input.Terminology
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

    public List<ProdModels.NoteDetailEprsc> MapProductionDetails(IReadOnlyList<PrescriptionDetailInput> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return items.Select(Map).ToList();
    }

    public List<PilotModels.NoteDetailEprsc> MapPilotDetails(IReadOnlyList<PrescriptionDetailInput> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return items.Select(MapPilot).ToList();
    }

    public ProdApiModels.NursingActionRequest MapProduction(NursingActionInput input)
    {
        Validate(input);
        return new() { SiamId = input.SiamId, NoteDetailsEprscIds = input.NoteDetailsEprscIds.Select(id => (long?)id).ToList(), NurseNationalCode = input.NurseNationalCode };
    }

    public PilotModels.NursingActionRequest MapPilot(NursingActionInput input)
    {
        Validate(input);
        return new() { SiamId = input.SiamId, NoteDetailsEprscIds = input.NoteDetailsEprscIds.Select(id => (long?)id).ToList(), NurseNationalCode = input.NurseNationalCode };
    }

    public ProdApiModels.PatientIllnessRequest MapProduction(PatientIllnessInput input)
    {
        Validate(input);
        return new()
        {
            PatientNationalCode = input.PatientNationalCode, IllId = input.IllId, MobileNumber = input.MobileNumber, SiamId = input.SiamId, InputType = 1,
            Type = input.Type switch { PatientIllnessType.One => ProdApiModels.PatientIllnessRequest_type.One, PatientIllnessType.Two => ProdApiModels.PatientIllnessRequest_type.Two, PatientIllnessType.Three => ProdApiModels.PatientIllnessRequest_type.Three, _ => null }
        };
    }

    public PilotApiModels.PatientIllnessRequest MapPilot(PatientIllnessInput input)
    {
        Validate(input);
        return new()
        {
            PatientNationalCode = input.PatientNationalCode, IllId = input.IllId, MobileNumber = input.MobileNumber, SiamId = input.SiamId, InputType = 1,
            Type = input.Type switch { PatientIllnessType.One => PilotApiModels.PatientIllnessRequest_type.One, PatientIllnessType.Two => PilotApiModels.PatientIllnessRequest_type.Two, PatientIllnessType.Three => PilotApiModels.PatientIllnessRequest_type.Three, _ => null }
        };
    }

    private static void Validate(NursingActionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        Required("siamId", input.SiamId);
        Required("nurseNationalCode", input.NurseNationalCode);
        if (input.NoteDetailsEprscIds is null || input.NoteDetailsEprscIds.Count == 0) throw new TaminRequestValidationException("noteDetailsEprscIds", "at least one item is required");
    }

    private static void Validate(PatientIllnessInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        Required("patientNationalCode", input.PatientNationalCode);
        Required("mobileNumber", input.MobileNumber);
        Required("siamId", input.SiamId);
    }

    private static void Required(string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new TaminRequestValidationException(field, "a value is required");
    }

    private static void Validate(PrescriptionCreateInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        // D-06: guide v1.10.1 no longer requires mobile (p14-15), but p65 rejects an empty value when sent: omit it or send a nonblank value.
        // No undocumented mobile regex is invented.
        if (input.Mobile is not null && string.IsNullOrWhiteSpace(input.Mobile)) throw new TaminRequestValidationException("mobile", "omit the field or send a nonblank value");
    }

    private static ProdModels.NoteDetailEprsc Map(PrescriptionDetailInput x) => new()
    {
        DateDo = x.DateDo, Dose = x.Dose, DoseCode = x.DoseCode,
        DrugInstruction = x.DrugInstruction is null ? null : new() { DrugInstId = x.DrugInstruction.DrugInstId },
        // D-07: the normalized flag maps to the generated wire enum without coercing other values.
        IllnessId = x.IllnessId, IsDentalService = x.IsDentalService == DentalServiceFlag.Zero ? ProdModels.NoteDetailEprsc_isDentalService.Zero : ProdModels.NoteDetailEprsc_isDentalService.One,
        MethodUsage = x.MethodUsage is null ? null : new() { Id = x.MethodUsage.Id },
        NoteDetailDrug = x.NoteDetailDrug is null ? null : new() { IcdId = x.NoteDetailDrug.IcdId }, NoteDetailsEprscId = x.NoteDetailsEprscId,
        PlanId = x.PlanId, ReferenceStatus = x.ReferenceStatus, Repeat = x.Repeat, SrvId = Map(x.SrvId), SrvQty = x.SrvQty,
        TimesAday = x.TimesAday is null ? null : new() { DrugAmntId = x.TimesAday.DrugAmntId }, ToothId = x.ToothId
    };

    private static PilotModels.NoteDetailEprsc MapPilot(PrescriptionDetailInput x) => new()
    {
        DateDo = x.DateDo, Dose = x.Dose, DoseCode = x.DoseCode,
        DrugInstruction = x.DrugInstruction is null ? null : new() { DrugInstId = x.DrugInstruction.DrugInstId },
        // D-07: the normalized flag maps to the generated wire enum without coercing other values.
        IllnessId = x.IllnessId, IsDentalService = x.IsDentalService == DentalServiceFlag.Zero ? PilotModels.NoteDetailEprsc_isDentalService.Zero : PilotModels.NoteDetailEprsc_isDentalService.One,
        MethodUsage = x.MethodUsage is null ? null : new() { Id = x.MethodUsage.Id },
        NoteDetailDrug = x.NoteDetailDrug is null ? null : new() { IcdId = x.NoteDetailDrug.IcdId }, NoteDetailsEprscId = x.NoteDetailsEprscId,
        PlanId = x.PlanId, ReferenceStatus = x.ReferenceStatus, Repeat = x.Repeat, SrvId = MapPilot(x.SrvId), SrvQty = x.SrvQty,
        TimesAday = x.TimesAday is null ? null : new() { DrugAmntId = x.TimesAday.DrugAmntId }, ToothId = x.ToothId
    };

    // D-09 through D-11: independent referral date/id, complaint members, and string ICD-10 codes are mapped without precedence or coercion.
    private static ProdModels.NoteDetailsReferral Map(PrescriptionReferralInput x) => new()
    {
        Complaints = x.Complaints?.Select(c => new ProdModels.ReferralComplaint { ComplaintIDs = c.ComplaintIDs, Id = c.Id }).ToList(),
        DocSpecReferred = x.DocSpecReferred is null ? null : new() { SpecCode = x.DocSpecReferred.SpecCode, Terminology = x.DocSpecReferred.Terminology },
        Icd10s = x.Icd10s?.Select(i => new ProdModels.ReferralIcd10 { IcdId = i.IcdId }).ToList(), Id = x.Id, Message = x.Message,
        NoteDetailsEprscId = x.NoteDetailsEprscId, PatientNationalCode = x.PatientNationalCode, Quantity = x.Quantity,
        ReferenceStatus = x.ReferenceStatus, ReferralFeedbackList = x.ReferralFeedbackList?.Select(Map).ToList(), ReferralHijriDate = x.ReferralHijriDate, SiamId = x.SiamId
    };
    private static PilotModels.NoteDetailsReferral MapPilot(PrescriptionReferralInput x) => new()
    {
        Complaints = x.Complaints?.Select(c => new PilotModels.ReferralComplaint { ComplaintIDs = c.ComplaintIDs, Id = c.Id }).ToList(),
        DocSpecReferred = x.DocSpecReferred is null ? null : new() { SpecCode = x.DocSpecReferred.SpecCode, Terminology = x.DocSpecReferred.Terminology },
        Icd10s = x.Icd10s?.Select(i => new PilotModels.ReferralIcd10 { IcdId = i.IcdId }).ToList(), Id = x.Id, Message = x.Message,
        NoteDetailsEprscId = x.NoteDetailsEprscId, PatientNationalCode = x.PatientNationalCode, Quantity = x.Quantity,
        ReferenceStatus = x.ReferenceStatus, ReferralFeedbackList = x.ReferralFeedbackList?.Select(MapPilot).ToList(), ReferralHijriDate = x.ReferralHijriDate, SiamId = x.SiamId
    };
    private static ProdModels.ReferralFeedbackItem Map(ReferralFeedbackItemInput x) => new() { AnswerId = x.AnswerId is null ? null : new() { DrugAnswerId = x.AnswerId.DrugAnswerId }, Comments = x.Comments, QuestionId = x.QuestionId };
    private static PilotModels.ReferralFeedbackItem MapPilot(ReferralFeedbackItemInput x) => new() { AnswerId = x.AnswerId is null ? null : new() { DrugAnswerId = x.AnswerId.DrugAnswerId }, Comments = x.Comments, QuestionId = x.QuestionId };
    private static ProdModels.Complaint? Map(ComplaintInput? x) => x is null ? null : new() { ComplaintIDs = x.ComplaintIDs };
    private static PilotModels.Complaint? MapPilot(ComplaintInput? x) => x is null ? null : new() { ComplaintIDs = x.ComplaintIDs };
    private static ProdModels.Service? Map(ServiceInput? x) => x is null ? null : new() { SrvCode = x.SrvCode, Terminology = x.Terminology, SrvType = x.SrvType is null ? null : new() { SrvType = WireValue.Parse<ProdModels.ServiceType_srvType>("srvType", x.SrvType) } };
    private static PilotModels.Service? MapPilot(ServiceInput? x) => x is null ? null : new() { SrvCode = x.SrvCode, Terminology = x.Terminology, SrvType = x.SrvType is null ? null : new() { SrvType = WireValue.Parse<PilotModels.ServiceType_srvType>("srvType", x.SrvType) } };
}

/// <summary>Resolves a documented wire value, such as "01", to the generated enum member that serializes to it.</summary>
internal static class WireValue
{
    public static TEnum Parse<TEnum>(string field, string value) where TEnum : struct, Enum
    {
        foreach (var member in typeof(TEnum).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            var wire = (System.Runtime.Serialization.EnumMemberAttribute?)Attribute.GetCustomAttribute(member, typeof(System.Runtime.Serialization.EnumMemberAttribute));
            if (wire?.Value == value) return (TEnum)member.GetValue(null)!;
        }
        throw new TaminRequestValidationException(field, $"'{value}' is not a documented value");
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
