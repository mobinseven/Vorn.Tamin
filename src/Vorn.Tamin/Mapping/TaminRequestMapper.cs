using System.Text.Json;
using KiotaModels = Vorn.Tamin.Kiota.Models;

namespace Vorn.Tamin.Mapping;

/// <summary>Maps supported friendly prescription DTOs to generated Kiota request models.</summary>
internal static class TaminRequestMapper
{
    private static readonly TaminProviderSerializer Serializer = new();
    public static KiotaModels.SendEprescRequest ToSendEprescRequest(RegisterVisitPrescriptionRequest request, TaminEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CreateSendRequest(request, endpoint, request.VisitDate, request.PrescriptionType, request.MobileNumber, request.Description);
    }

    public static KiotaModels.SendEprescRequest ToSendEprescRequest(RegisterDrugPrescriptionRequest request, TaminEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = CreateSendRequest(request, endpoint, request.VisitDate, request.PrescriptionType, request.MobileNumber);
        result.NoteDetailEprscs = request.DrugItems.Select(ToNoteDetail).ToList();
        return result;
    }

    public static KiotaModels.SendEprescRequest ToSendEprescRequest(RegisterParaclinicPrescriptionRequest request, TaminEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = CreateSendRequest(request, endpoint, request.VisitDate, request.PrescriptionType);
        result.NoteDetailEprscs = request.ServiceItems.Select(ToNoteDetail).ToList();
        return result;
    }

    public static KiotaModels.SendEprescRequest ToSendEprescRequest(RegisterMedicalServicePrescriptionRequest request, TaminEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = CreateSendRequest(request, endpoint, request.VisitDate, request.PrescriptionType);
        result.NoteDetailEprscs = request.ServiceItems.Select(ToNoteDetail).ToList();
        return result;
    }

    public static KiotaModels.SendEprescRequest ToSendEprescRequest(RegisterReferralPrescriptionRequest request, TaminEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = CreateSendRequest(request, endpoint, request.VisitDate, request.PrescriptionType, comments: request.Description ?? request.Reason);
        result.AdditionalData["targetSpecialty"] = Serializer.SerializeStringCode(request.TargetSpecialty, nameof(request.TargetSpecialty));
        result.AdditionalData["targetProviderType"] = Serializer.SerializeStringCode(request.TargetProviderType, nameof(request.TargetProviderType));
        return result;
    }

    public static KiotaModels.SendEprescRequest ToSendEprescRequest(RegisterPhysiotherapyPrescriptionRequest request, TaminEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = CreateSendRequest(request, endpoint, request.EffectiveDate, request.PrescriptionType, comments: request.Description);
        result.NoteDetailEprscs = request.PhysiotherapyItems.Select(item => new KiotaModels.NoteDetailEprsc
        {
            SrvId = new KiotaModels.Service { SrvCode = Serializer.SerializeStringCode(item.ServiceCode, nameof(item.ServiceCode)) },
            SrvQty = request.SessionCount,
            AdditionalData = { ["description"] = item.Description ?? string.Empty }
        }).ToList();
        return result;
    }

    public static IReadOnlyList<KiotaModels.NoteDetailEprsc> ToNoteDetails(IEnumerable<object> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.Select(ToNoteDetail).ToList();
    }

    public static KiotaModels.DentistRuleRequest ToDentistRuleRequest(CheckWarningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new KiotaModels.DentistRuleRequest
        {
            DocId = Serializer.SerializeStringCode(request.DoctorId, nameof(request.DoctorId)),
            PatientId = Serializer.SerializeStringCode(request.PatientNationalId, nameof(request.PatientNationalId)),
            AllGridData = request.PrescriptionItems.Select(ToGridData).ToList()
        };
    }

    public static KiotaModels.SendEprescRequest ToSendEprescRequest(HospitalizationCreateRequest request, TaminEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = CreateSendRequest(request, endpoint, request.PrescriptionDate, request.PrescriptionType, comments: request.Message);
        result.SiamId = Serializer.SerializeStringCode(request.SiamId, nameof(request.SiamId));
        result.NoteDetailsReferralList = request.ReferralDetails.Select(ToNoteDetailsReferral).ToList();
        return result;
    }

    private static KiotaModels.SendEprescRequest CreateSendRequest(
        ProviderPrescriptionIdentity provider,
        TaminEndpoint endpoint,
        string? prescriptionDate,
        int prescriptionType,
        string? mobile = null,
        string? comments = null)
        => new()
        {
            DocId = Serializer.SerializeStringCode(provider.DoctorId, nameof(provider.DoctorId)),
            DocNationalCode = Serializer.SerializeOptionalStringCode(provider.DoctorNationalCode, nameof(provider.DoctorNationalCode)),
            DocMobileNo = Serializer.SerializeOptionalStringCode(provider.DoctorMobileNumber, nameof(provider.DoctorMobileNumber)),
            ClientId = endpoint == TaminEndpoint.Sandbox ? Serializer.SerializeOptionalStringCode(provider.DoctorNationalCode, nameof(provider.DoctorNationalCode)) : null,
            Patient = Serializer.SerializeStringCode(provider.PatientNationalId, nameof(provider.PatientNationalId)),
            PrescDate = Serializer.SerializeJalaliDate(prescriptionDate, nameof(prescriptionDate)),
            Mobile = Serializer.SerializeOptionalStringCode(mobile, nameof(mobile)),
            Comments = comments,
            PrescType = new KiotaModels.PrescType { PrescTypeId = prescriptionType }
        };

    private static KiotaModels.NoteDetailsReferral ToNoteDetailsReferral(HospitalizationReferralDetail item)
        => new()
        {
            PatientNationalCode = Serializer.SerializeStringCode(item.PatientNationalCode, nameof(item.PatientNationalCode)),
            ReferralHijriDate = Serializer.SerializeJalaliDate(item.ReferralHijriDate, nameof(item.ReferralHijriDate)),
            SiamId = Serializer.SerializeStringCode(item.SiamId, nameof(item.SiamId)),
            Icd10s = item.Icd10Items.Select(ToIcd10).ToList(),
            Message = item.Message
        };

    private static KiotaModels.Icd10 ToIcd10(HospitalizationIcd10Item item)
        => new()
        {
            IcdCode = Serializer.SerializeStringCode(item.IcdCode, nameof(item.IcdCode)),
            IcdName = item.IcdName,
            IcdId = string.IsNullOrWhiteSpace(item.IcdId) ? null : new KiotaModels.Icd10.Icd10_icdId { String = item.IcdId }
        };

    private static KiotaModels.NoteDetailEprsc ToNoteDetail(DrugItem item)
        => new()
        {
            NoteDetailDrug = new KiotaModels.DiagnosisID
            {
                AdditionalData = { ["drugCode"] = Serializer.SerializeStringCode(item.DrugCode, nameof(item.DrugCode)) }
            },
            SrvQty = item.Quantity,
            Dose = item.DosageInstruction,
            Repeat = item.RepeatCount?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            AdditionalData = { ["drugCode"] = Serializer.SerializeStringCode(item.DrugCode, nameof(item.DrugCode)), ["frequency"] = item.Frequency ?? string.Empty, ["duration"] = item.Duration ?? string.Empty, ["route"] = item.Route ?? string.Empty, ["usageNote"] = item.UsageNote ?? string.Empty }
        };

    private static KiotaModels.NoteDetailEprsc ToNoteDetail(ServiceItem item)
        => new()
        {
            SrvId = new KiotaModels.Service { SrvCode = Serializer.SerializeStringCode(item.ServiceCode, nameof(item.ServiceCode)) },
            SrvQty = item.Quantity,
            AdditionalData = { ["serviceGroup"] = item.ServiceGroup ?? string.Empty, ["dateDo"] = Serializer.SerializeOptionalJalaliDate(item.EffectiveDate, nameof(item.EffectiveDate)) ?? string.Empty, ["description"] = item.Description ?? string.Empty }
        };

    private static KiotaModels.NoteDetailEprsc ToNoteDetail(object value)
    {
        if (value is KiotaModels.NoteDetailEprsc noteDetail)
            return noteDetail;

        return JsonSerializer.Deserialize<KiotaModels.NoteDetailEprsc>(JsonSerializer.Serialize(value))
            ?? throw new ArgumentException("Edited item could not be mapped to a generated prescription detail model.", nameof(value));
    }

    private static KiotaModels.GridData ToGridData(object value)
    {
        if (value is KiotaModels.GridData gridData)
            return gridData;

        return JsonSerializer.Deserialize<KiotaModels.GridData>(JsonSerializer.Serialize(value))
            ?? throw new ArgumentException("Warning item could not be mapped to a generated rules-detail grid model.", nameof(value));
    }
}
