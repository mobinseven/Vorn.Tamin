using System.Text.Json;
using ApiModels = Tamin.Client.Api.Prod.Models;
using SoaModels = Tamin.Client.Soa.Prod.Models;
using PilotApiModels = Tamin.Client.Api.Pilot.Models;
using PilotSoaModels = Tamin.Client.Soa.Pilot.Models;

namespace Tamin.Integration.Mapping;

/// <summary>The body of an operation for which the contract defines no response schema. No names, types or envelope are inferred.</summary>
public sealed record UnconstrainedResponse(JsonElement Payload);
public sealed record AccessTokenResult(string? AccessToken, int? ExpiresIn);
public sealed record TaminEnvelope<T>(int? Status, string? Family, string? Reason, string? TraceId, T Data);
public sealed record ReferralCartableItemDto(string? NoteHeadId, string? ReferralId, string? PatientId, string? DoctorId, string? PrescriptionDate, string? ReferralDate);
public sealed record ReferralCartableDto(int? Total, IReadOnlyList<ReferralCartableItemDto> Items);
public sealed record PrescriptionTypeDto(int? Id, string? Code, string? Description);
public sealed record ReferralFeedbackPrescriptionDto(long? NoteHeadEprscId, string? Patient, string? PrescriptionDate, JsonElement Items, JsonElement? Referrals, PrescriptionTypeDto? PrescriptionType);
public sealed record ReferralFeedbackDetailsDto(int? Total, IReadOnlyList<ReferralFeedbackPrescriptionDto> Items);
public sealed record OpenReferralSpecialtyDto(string? SpecCode, string? SpecDesc, string? SpecGroup, string? Status, string? StatusDate, string? LStatus);
public sealed record PatientOpenReferralsDto(int? Count, IReadOnlyList<OpenReferralSpecialtyDto> Referrals);
public sealed record HospitalizationOrderDto(long? Id, long? MasterParent, long? ReferralDate, string? DoctorId, int? Quantity, bool? DoctorFamily, string? PatientNationalCode = null);
public sealed record FamilyDoctorShareDto(string? Price, bool? FamilyDoctor);
public sealed record PatientDiseaseDto(int? Id, string? IllText, string? Status, string? Type, string? CodeMapping, string? IllName, int? PriorityOrder, string? StatusDate);
public sealed record PatientDiseaseListDto(int? Total, IReadOnlyList<PatientDiseaseDto> Diseases);
public sealed record MedicationInstructionOptionDto(string? Id, string? Code, string? Name, string? NameLatin);
public sealed record FormAmountDto(string? Id, string? Code, string? Name, string? NameLatin, IReadOnlyList<MedicationInstructionOptionDto> DrugAmounts);
public sealed record MedicationInstructionsDto(IReadOnlyList<MedicationInstructionOptionDto> DrugInstructions, IReadOnlyList<MedicationInstructionOptionDto> DrugUsages, IReadOnlyList<MedicationInstructionOptionDto> MethodUsages, IReadOnlyList<FormAmountDto> FormAmounts);
public sealed record ReferralComplaintDto(int? Id, long? Code, string? DisplayName, string? EnglishName, string? Terminology, string? Status);
public sealed record ReferralIcd10Dto(string? IcdId, string? IcdCode, string? IcdName);
public sealed record ReferralFeedbackEntryDto(long? Id, long? MasterParent, long? MasterChild, long? ReferralDate, string? DoctorId, int? Quantity, string? Message, string? DiagnosisIds, string? SpecCodeReferral, string? SpecDescReferral, string? SpecCodeReferred, string? SpecDescReferred, IReadOnlyList<ReferralComplaintDto> Complaints, IReadOnlyList<ReferralIcd10Dto> Icd10Referrals);
public sealed record ReferralFeedbackEntriesDto(int? Total, IReadOnlyList<ReferralFeedbackEntryDto> Entries);
public sealed record NursingPrescriptionItemDto(long? NoteDetailsEprscId, long? ServiceId, string? ServiceCode, string? ServiceName, int? Quantity, int? RemainingQuantity, long? Price, string? IsOk, bool? ReadOnly);
public sealed record NursingPrescriptionDto(long? NoteHeadId, long? DocEprscId, long? ClinicDocId, int? TrackingCode, string? NationalCode, string? PatientFirstName, string? PatientLastName, string? DoctorId, string? DoctorFirstName, string? DoctorLastName, int? PrescriptionType, int? IsSpecialPatient, string? IsOk, long? PrescriptionCreationTime, IReadOnlyList<NursingPrescriptionItemDto> Items);
public sealed record NursingPrescriptionListDto(int? Total, IReadOnlyList<NursingPrescriptionDto> Prescriptions);
public sealed record DoctorActiveCenterDto(string? DbLinkId, string? Name, string? SiamId, string? ProvinceName);
public sealed record PrescriptionMutationResult(long? HeaderId, string? ComplementaryMessage, string? ErrorCode, string? ErrorMessage, long? TrackingCode);

/// <summary>Maps generated response models of both environments to the public DTOs.</summary>
internal sealed class TaminMappingService
{
    public PrescriptionMutationResult MapCreatePrescription(SoaModels.PrescriptionMutationResponse response) => new(response.HeadEPRSCID, response.ComplemantaryMsg, response.ErrorCode, response.ErrorMsg, response.TrackingCode);
    public PrescriptionMutationResult MapMarkPatientDisease(ApiModels.PrescriptionMutationResponse response) => new(response.HeadEPRSCID, response.ComplemantaryMsg, response.ErrorCode, response.ErrorMsg, response.TrackingCode); // D-20: reuse only the documented optional mutation fields.
    public PrescriptionMutationResult MapCreatePrescription(PilotSoaModels.PrescriptionMutationResponse response) => new(response.HeadEPRSCID, response.ComplemantaryMsg, response.ErrorCode, response.ErrorMsg, response.TrackingCode);
    public PrescriptionMutationResult MapMarkPatientDisease(PilotApiModels.PrescriptionMutationResponse response) => new(response.HeadEPRSCID, response.ComplemantaryMsg, response.ErrorCode, response.ErrorMsg, response.TrackingCode); // D-20: pilot uses the same optional mutation fields.

    public TaminEnvelope<ReferralCartableDto> MapReferralCartable(SoaModels.ReferralCartableResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var data = response.Data;
        // D-12: patientID has an untrusted description, so it remains a string with no semantic reinterpretation.
        var items = data?.List?.Select(item => new ReferralCartableItemDto(item.NoteHeadId, item.ReferralId, item.PatientID, item.DocID, item.PrescDate, item.ReferralDate)).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, null, new(data?.Total, items));
    }
    public TaminEnvelope<ReferralCartableDto> MapReferralCartable(PilotSoaModels.ReferralCartableResponse response)
    {
        var data = response.Data; // D-12: patientID semantics remain untrusted in pilot too.
        var items = data?.List?.Select(item => new ReferralCartableItemDto(item.NoteHeadId, item.ReferralId, item.PatientID, item.DocID, item.PrescDate, item.ReferralDate)).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, null, new(data?.Total, items));
    }
    public TaminEnvelope<ReferralFeedbackDetailsDto> MapReferralFeedbackDetails(SoaModels.ReferralFeedbackDetailResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        // D-13: mapping follows the declared operation (v2 in guide v1.10.1), not the older conflicting invocation examples.
        var items = response.Data?.List?.Select(item => new ReferralFeedbackPrescriptionDto(item.NoteHeadEprscId, item.Patient, item.PrescDate, JsonSerializer.SerializeToElement(item.NoteDetailEprscs), item.NoteDetailsReferralList is null ? null : JsonSerializer.SerializeToElement(item.NoteDetailsReferralList), item.PrescType is null ? null : new(item.PrescType.PrescTypeId, item.PrescType.PrescTypeCode, item.PrescType.PrescTypeDesc))).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, null, new(response.Data?.Total, items));
    }
    public TaminEnvelope<ReferralFeedbackDetailsDto> MapReferralFeedbackDetails(PilotSoaModels.ReferralFeedbackDetailResponse response)
    {
        // D-13: the pilot mapper follows its generated declared route rather than the conflicting production invocation examples.
        var items = response.Data?.List?.Select(item => new ReferralFeedbackPrescriptionDto(item.NoteHeadEprscId, item.Patient, item.PrescDate, JsonSerializer.SerializeToElement(item.NoteDetailEprscs), item.NoteDetailsReferralList is null ? null : JsonSerializer.SerializeToElement(item.NoteDetailsReferralList), item.PrescType is null ? null : new(item.PrescType.PrescTypeId, item.PrescType.PrescTypeCode, item.PrescType.PrescTypeDesc))).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, null, new(response.Data?.Total, items));
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
    public TaminEnvelope<IReadOnlyList<HospitalizationOrderDto>> MapHospitalizationOrders(SoaModels.HospitalizationOrderResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var data = response.Data?.Select(item => new HospitalizationOrderDto(item.Id, item.MasterParent, item.ReferralDate, item.DocId, item.Quantity, item.DocFamily, item.PatientNationalCode)).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, response.TraceId, data);
    }
    public TaminEnvelope<IReadOnlyList<HospitalizationOrderDto>> MapHospitalizationOrders(ApiModels.HospitalizationOrderResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var data = response.Data?.Select(item => new HospitalizationOrderDto(item.Id, item.MasterParent, item.ReferralDate, item.DocId, item.Quantity, item.DocFamily, item.PatientNationalCode)).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, response.TraceId, data);
    }
    public TaminEnvelope<IReadOnlyList<HospitalizationOrderDto>> MapHospitalizationOrders(PilotSoaModels.HospitalizationOrderResponse response)
    {
        var data = response.Data?.Select(item => new HospitalizationOrderDto(item.Id, item.MasterParent, item.ReferralDate, item.DocId, item.Quantity, item.DocFamily, item.PatientNationalCode)).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, response.TraceId, data);
    }
    public TaminEnvelope<FamilyDoctorShareDto> MapFamilyDoctorShare(ApiModels.FamilyDoctorShareResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new(response.Status, response.Family, response.Reason, response.TraceId, new(response.Data?.Price, response.Data?.FamilyDoctor));
    }
    public TaminEnvelope<FamilyDoctorShareDto> MapFamilyDoctorShare(PilotApiModels.FamilyDoctorShareResponse response) => new(response.Status, response.Family, response.Reason, response.TraceId, new(response.Data?.Price, response.Data?.FamilyDoctor));
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

    public TaminEnvelope<MedicationInstructionsDto> MapMedicationInstructionsByDosageForm(ApiModels.MedicationInstructionsResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var data = response.Data;
        return new(response.Status, response.Family, response.Reason, response.TraceId, new(
            data?.DrugInstructions?.Select(o => new MedicationInstructionOptionDto(o.Id, o.Code, o.Name, o.NameLatin)).ToArray() ?? [],
            data?.DrugUsages?.Select(o => new MedicationInstructionOptionDto(o.Id, o.Code, o.Name, o.NameLatin)).ToArray() ?? [],
            data?.MethodUsages?.Select(o => new MedicationInstructionOptionDto(o.Id, o.Code, o.Name, o.NameLatin)).ToArray() ?? [],
            data?.FormAmounts?.Select(f => new FormAmountDto(f.Id, f.Code, f.Name, f.NameLatin, f.DrugAmounts?.Select(o => new MedicationInstructionOptionDto(o.Id, o.Code, o.Name, o.NameLatin)).ToArray() ?? [])).ToArray() ?? []));
    }
    public TaminEnvelope<MedicationInstructionsDto> MapMedicationInstructionsByDosageForm(PilotSoaModels.MedicationInstructionsResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var data = response.Data;
        return new(response.Status, response.Family, response.Reason, response.TraceId, new(
            data?.DrugInstructions?.Select(o => new MedicationInstructionOptionDto(o.Id, o.Code, o.Name, o.NameLatin)).ToArray() ?? [],
            data?.DrugUsages?.Select(o => new MedicationInstructionOptionDto(o.Id, o.Code, o.Name, o.NameLatin)).ToArray() ?? [],
            data?.MethodUsages?.Select(o => new MedicationInstructionOptionDto(o.Id, o.Code, o.Name, o.NameLatin)).ToArray() ?? [],
            data?.FormAmounts?.Select(f => new FormAmountDto(f.Id, f.Code, f.Name, f.NameLatin, f.DrugAmounts?.Select(o => new MedicationInstructionOptionDto(o.Id, o.Code, o.Name, o.NameLatin)).ToArray() ?? [])).ToArray() ?? []));
    }
    public TaminEnvelope<ReferralFeedbackEntriesDto> MapReferralFeedbackEntries(SoaModels.ReferralListResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var entries = response.Data?.List?.Select(e => new ReferralFeedbackEntryDto(e.Id, e.MasterParent, e.MasterChild, e.ReferralDate, e.DocId, e.Quantity, e.Message, e.DiagnosisIDs, e.SpecCodeReferral, e.SpecDescReferral, e.SpecCodeReferred, e.SpecDescReferred,
            e.Complaints?.Select(c => new ReferralComplaintDto(c.Id, c.Code, c.DisplayName, c.EnglishName, c.Terminology, c.Status)).ToArray() ?? [],
            e.Icd10Referrals?.Select(i => new ReferralIcd10Dto(i.IcdId, i.IcdCode, i.IcdName)).ToArray() ?? [])).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, null, new(response.Data?.Total, entries));
    }
    public TaminEnvelope<ReferralFeedbackEntriesDto> MapReferralFeedbackEntries(PilotSoaModels.ReferralListResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var entries = response.Data?.List?.Select(e => new ReferralFeedbackEntryDto(e.Id, e.MasterParent, e.MasterChild, e.ReferralDate, e.DocId, e.Quantity, e.Message, e.DiagnosisIDs, e.SpecCodeReferral, e.SpecDescReferral, e.SpecCodeReferred, e.SpecDescReferred,
            e.Complaints?.Select(c => new ReferralComplaintDto(c.Id, c.Code, c.DisplayName, c.EnglishName, c.Terminology, c.Status)).ToArray() ?? [],
            e.Icd10Referrals?.Select(i => new ReferralIcd10Dto(i.IcdId, i.IcdCode, i.IcdName)).ToArray() ?? [])).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, null, new(response.Data?.Total, entries));
    }
    public NursingPrescriptionListDto MapDoctorNursingPrescriptions(ApiModels.NursingPrescriptionListResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        // p53: the payload has top-level total and list, with no status/family/reason envelope.
        var prescriptions = response.List?.Select(p => new NursingPrescriptionDto(p.NoteHeadId, p.DocEprscId, p.ClinicDocId, p.TrackingCode, p.NationalCode, p.PatientFirstName, p.PatientLastName, p.DocId, p.DocFirstName, p.DocLastName, p.PrescType, p.IsSpecialPatient, p.IsOk, p.PrescCreationTime,
            p.NoteDetailEprscs?.Select(i => new NursingPrescriptionItemDto(i.NoteDetailsEprscId, i.SrvId?.SrvId, i.SrvId?.SrvCode, i.SrvId?.SrvName, i.SrvQty, i.SrvRem, i.SrvPrice, i.IsOk, i.ReadOnly)).ToArray() ?? [])).ToArray() ?? [];
        return new(response.Total, prescriptions);
    }
    public NursingPrescriptionListDto MapDoctorNursingPrescriptions(PilotSoaModels.NursingPrescriptionListResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var prescriptions = response.List?.Select(p => new NursingPrescriptionDto(p.NoteHeadId, p.DocEprscId, p.ClinicDocId, p.TrackingCode, p.NationalCode, p.PatientFirstName, p.PatientLastName, p.DocId, p.DocFirstName, p.DocLastName, p.PrescType, p.IsSpecialPatient, p.IsOk, p.PrescCreationTime,
            p.NoteDetailEprscs?.Select(i => new NursingPrescriptionItemDto(i.NoteDetailsEprscId, i.SrvId?.SrvId, i.SrvId?.SrvCode, i.SrvId?.SrvName, i.SrvQty, i.SrvRem, i.SrvPrice, i.IsOk, i.ReadOnly)).ToArray() ?? [])).ToArray() ?? [];
        return new(response.Total, prescriptions);
    }
    public TaminEnvelope<IReadOnlyList<DoctorActiveCenterDto>> MapDoctorActiveCenters(ApiModels.DoctorActiveCenterListResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var data = response.Data?.Select(c => new DoctorActiveCenterDto(c.DbLinkId, c.HosName, c.SiamId, c.ProvinceName)).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, response.TraceId, data);
    }
    public TaminEnvelope<IReadOnlyList<DoctorActiveCenterDto>> MapDoctorActiveCenters(PilotSoaModels.DoctorActiveCenterListResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        var data = response.Data?.Select(c => new DoctorActiveCenterDto(c.DbLinkId, c.HosName, c.SiamId, c.ProvinceName)).ToArray() ?? [];
        return new(response.Status, response.Family, response.Reason, response.TraceId, data);
    }
}
