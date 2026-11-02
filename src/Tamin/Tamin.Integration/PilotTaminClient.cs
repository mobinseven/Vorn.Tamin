using System.Globalization;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Tamin.Client.Account.Pilot.Auth.Server.Authorize;
using Tamin.Integration.Auth;
using Tamin.Integration.Mapping;
using static Tamin.Integration.Http.TaminOperationSender;
using AccountModels = Tamin.Client.Account.Pilot.Models;
using ApiModels = Tamin.Client.Api.Pilot.Models;
using PilotAccountClient = Tamin.Client.Account.Pilot.PilotAccountClient;
using PilotApiClient = Tamin.Client.Api.Pilot.PilotApiClient;
using PilotSoaClient = Tamin.Client.Soa.Pilot.PilotSoaClient;
using SoaModels = Tamin.Client.Soa.Pilot.Models;
using SoaEp = Tamin.Client.Soa.Pilot.Api.V2.Ep;

namespace Tamin.Integration;

internal sealed class PilotTaminClient(PilotAccountClient account, PilotSoaClient soa, PilotApiClient api, TaminAdapters adapters, TaminTokenProvider tokens) : ITaminClient
{
    private static readonly TaminMappingService Responses = new();
    private static readonly PrescriptionRequestMapper Requests = new();

    private Task<UnconstrainedResponse> Soa(string operationId, RequestInformation request, CancellationToken ct) => SendOpaqueAsync(adapters.Soa, operationId, request, ct);
    private Task<UnconstrainedResponse> Api(string operationId, RequestInformation request, CancellationToken ct) => SendOpaqueAsync(adapters.Api, operationId, request, ct);
    private Task<TResult> Soa<TModel, TResult>(string operationId, RequestInformation request, ParsableFactory<TModel> factory, Func<TModel, TResult> map, CancellationToken ct) where TModel : IParsable => SendAsync(adapters.Soa, operationId, request, factory, map, ct);
    private Task<TResult> Api<TModel, TResult>(string operationId, RequestInformation request, ParsableFactory<TModel> factory, Func<TModel, TResult> map, CancellationToken ct) where TModel : IParsable => SendAsync(adapters.Api, operationId, request, factory, map, ct);

    public Uri CreateAuthorizationUri(string redirectUri, string codeChallenge, string? state = null) => account.Auth.Server.Authorize.ToGetRequestInformation(c =>
    {
        c.QueryParameters.RedirectUri = Route(redirectUri);
        c.QueryParameters.CodeChallenge = Route(codeChallenge);
        c.QueryParameters.ClientId = GetClient_idQueryParameterType.PortalJs;
        c.QueryParameters.ResponseType = GetResponse_typeQueryParameterType.Code;
        c.QueryParameters.CodeChallengeMethod = GetCode_challenge_methodQueryParameterType.S256;
        c.QueryParameters.State = state;
    }).URI;

    public Uri CreateSignOutUri(string redirectUri) => account.Auth.Signout.ToGetRequestInformation(c => c.QueryParameters.RedirectUri = Route(redirectUri)).URI;

    public Task CompleteAuthorizationAsync(string code, string redirectUri, string codeVerifier, CancellationToken cancellationToken = default) => tokens.CompleteAuthorizationAsync(code, redirectUri, codeVerifier, cancellationToken);

    public Task<AccessTokenResult> ExchangeAuthorizationCodeAsync(string code, string redirectUri, string codeVerifier, CancellationToken cancellationToken = default) =>
        SendAsync(adapters.Account, "exchangeAuthorizationCode", account.Auth.Server.Token.ToPostRequestInformation(new AccountModels.AuthorizationCodeTokenRequest
        {
            Code = Route(code), RedirectUri = Route(redirectUri), CodeVerifier = Route(codeVerifier),
            ClientId = AccountModels.AuthorizationCodeTokenRequest_client_id.PortalJs,
            GrantType = AccountModels.AuthorizationCodeTokenRequest_grant_type.Authorization_code
        }), AccountModels.AccessTokenResponse.CreateFromDiscriminatorValue, r => new AccessTokenResult(r.AccessToken, r.ExpiresIn), cancellationToken);

    public Task<UnconstrainedResponse> ListPrescriptionTypesAsync(CancellationToken cancellationToken = default) => Soa("listPrescriptionTypes", soa.Api.V2.WsPrescriptionType.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListPrescriptionServiceTypesAsync(CancellationToken cancellationToken = default) => Soa("listPrescriptionServiceTypes", soa.Api.V2.WsServiceType.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListServicesAsync(string serviceType, CancellationToken cancellationToken = default) => Soa("listServices", soa.Api.V2.WsServices.ToGetRequestInformation(c => c.QueryParameters.ServiceType = Route(serviceType)), cancellationToken);
    public Task<UnconstrainedResponse> ListLaboratoryTariffGroupsAsync(CancellationToken cancellationToken = default) => Soa("listLaboratoryTariffGroups", soa.Api.V2.WsParTaref.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListDrugAmountsAsync(CancellationToken cancellationToken = default) => Soa("listDrugAmounts", soa.Api.V2.WsDrugAmount.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListDrugUsagesAsync(CancellationToken cancellationToken = default) => Soa("listDrugUsages", soa.Api.V2.WsDrugUsage.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListDrugInstructionsAsync(CancellationToken cancellationToken = default) => Soa("listDrugInstructions", soa.Api.V2.WsDrugInstruction.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListDosageFormsAsync(CancellationToken cancellationToken = default) => Soa("listDosageForms", soa.Api.V2.WsDrugForm.ToGetRequestInformation(), cancellationToken);
    public Task<TaminEnvelope<MedicationInstructionsDto>> GetMedicationInstructionsByDosageFormAsync(string formCode, CancellationToken cancellationToken = default) =>
        Soa("getMedicationInstructionsByDosageForm", soa.Api.V2.WsDrugFormsOptions[Route(formCode)].ToGetRequestInformation(), SoaModels.MedicationInstructionsResponse.CreateFromDiscriminatorValue, Responses.MapMedicationInstructionsByDosageForm, cancellationToken);
    public Task<UnconstrainedResponse> ListPhysiotherapyPlansAsync(CancellationToken cancellationToken = default) => Soa("listPhysiotherapyPlans", soa.Api.V2.WsPhPlan.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListPhysiotherapyIllnessesAsync(CancellationToken cancellationToken = default) => Soa("listPhysiotherapyIllnesses", soa.Api.V2.WsPhIllness.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListComplaintsAsync(CancellationToken cancellationToken = default) => Soa("listComplaints", soa.Api.Complaint.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListIcd10DiagnosesAsync(CancellationToken cancellationToken = default) => Soa("listIcd10Diagnoses", soa.Api.Icd10.GetAll.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListDoctorSpecialtiesAsync(CancellationToken cancellationToken = default) => Soa("listDoctorSpecialties", soa.Api.Specials.GetAll.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListDentistServicesWithoutToothAsync(int dentistType, CancellationToken cancellationToken = default) => Soa("listDentistServicesWithoutTooth", soa.Api.V2.WsDentistBase.FindDentistServiceNameWithoutTooth[dentistType].ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListDentistServicesByToothAsync(string selectedTooth, string patientId, int toothType, CancellationToken cancellationToken = default) => Soa("listDentistServicesByTooth", soa.Api.V2.WsDentistBase.FindDentistServiceNameByTooth[Route(selectedTooth)][Route(patientId)][toothType].ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> CheckDentistRulesAsync(DentistRuleInput input, CancellationToken cancellationToken = default) => Soa("checkDentistRules", soa.Api.V2.CheckRulesInDetail.ToPostRequestInformation(Requests.MapPilot(input)), cancellationToken);

    public Task<PrescriptionMutationResult> CreatePrescriptionAsync(PrescriptionCreateInput input, CancellationToken cancellationToken = default) =>
        Soa("createPrescription", soa.Api.V2.SendEpresc.ToPostRequestInformation(Requests.MapPilot(input)), SoaModels.PrescriptionMutationResponse.CreateFromDiscriminatorValue, Responses.MapCreatePrescription, cancellationToken);
    public Task<UnconstrainedResponse> UpdatePrescriptionAsync(long headerId, string docNationalCode, string docId, IReadOnlyList<PrescriptionDetailInput> items, CancellationToken cancellationToken = default) =>
        Soa("updatePrescription", soa.Api.V2.Ep.Update[headerId.ToString(CultureInfo.InvariantCulture)][Route(docNationalCode)][Route(docId)].ToPostRequestInformation(Requests.MapPilotDetails(items)), cancellationToken);
    public Task<UnconstrainedResponse> DeletePrescriptionAsync(long headerId, string docNationalCode, string docId, CancellationToken cancellationToken = default) => Soa("deletePrescription", soa.Api.V2.Ep[headerId][Route(docNationalCode)][Route(docId)].ToPostRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> GetPrescriptionDetailsAsync(long headerId, string docNationalCode, string docId, CancellationToken cancellationToken = default) => Soa("getPrescriptionDetails", soa.Api.V2.Ep[headerId][Route(docNationalCode)][Route(docId)].Detail.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> GetPatientEntitlementAsync(string siamId, string docId, string patientId, string doctorNationalCode, CancellationToken cancellationToken = default) => Soa("getPatientEntitlement", soa.Api.V2.Patients.DeserveInfo[Route(doctorNationalCode)][Route(siamId)][Route(docId)][Route(patientId)].ToGetRequestInformation(), cancellationToken);

    public Task<TaminEnvelope<ReferralCartableDto>> GetReferralCartableAsync(string docNationalCode, string patientNationalCode, int trackingCode, CancellationToken cancellationToken = default) =>
        Soa("getReferralCartable", soa.Api.CartablenoteDetailsReferral[Route(docNationalCode)][Route(patientNationalCode)][trackingCode].ToGetRequestInformation(), SoaModels.ReferralCartableResponse.CreateFromDiscriminatorValue, Responses.MapReferralCartable, cancellationToken);
    public Task<TaminEnvelope<ReferralFeedbackDetailsDto>> GetReferralFeedbackDetailsAsync(long id, long masterParent, CancellationToken cancellationToken = default) =>
        Soa("getReferralFeedbackDetails", soa.Api.NoteDetailsReferral.Noteheads[id][masterParent].ToGetRequestInformation(), SoaModels.ReferralFeedbackDetailResponse.CreateFromDiscriminatorValue, Responses.MapReferralFeedbackDetails, cancellationToken);

    // Kiota merged {nationalCode} into a sibling integer parameter, so its indexer takes a long and would drop leading zeros. The builder is created directly with the string value.
    public Task<UnconstrainedResponse> GetReferralPrescriptionAsync(string nationalCode, string docId, int trackingCode, CancellationToken cancellationToken = default) =>
        Soa("getReferralPrescription", new SoaEp.FindNoteReferral.Item.Item.Item.WithTrackingCodeItemRequestBuilder(new Dictionary<string, object>
        {
            ["baseurl"] = adapters.Soa.BaseUrl!, ["master%2Did"] = Route(nationalCode), ["doc%2Did"] = Route(docId), ["trackingCode"] = trackingCode
        }, adapters.Soa).ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> GetReferredServicePrescriptionAsync(string nationalCode, string docNationalCode, string docId, int trackingCode, CancellationToken cancellationToken = default) =>
        Soa("getReferredServicePrescription", new SoaEp.Item.Item.Item.Item.Detail.DetailRequestBuilder(new Dictionary<string, object>
        {
            ["baseurl"] = adapters.Soa.BaseUrl!, ["header%2Did"] = Route(nationalCode), ["docNatioanlCode%2Did"] = Route(docNationalCode), ["docId"] = Route(docId), ["trackingCode"] = trackingCode
        }, adapters.Soa).ToGetRequestInformation(), cancellationToken);

    public Task<UnconstrainedResponse> GetOpenReferralCountAsync(string nationalCode, string docId, CancellationToken cancellationToken = default) => Soa("getOpenReferralCount", soa.Api.V2.Referral.Count[Route(nationalCode)][Route(docId)].ToGetRequestInformation(), cancellationToken);
    public Task<TaminEnvelope<PatientOpenReferralsDto>> ListPatientOpenReferralsAsync(string nationalCode, CancellationToken cancellationToken = default) =>
        Soa("listPatientOpenReferrals", soa.Api.V2.Referral.Count[Route(nationalCode)].ToGetRequestInformation(), SoaModels.PatientOpenReferralsResponse.CreateFromDiscriminatorValue, Responses.MapPatientOpenReferrals, cancellationToken);
    public Task<UnconstrainedResponse> ListRecentDoctorReferralsAsync(string nationalCode, CancellationToken cancellationToken = default) => Soa("listRecentDoctorReferrals", soa.Api.V2.Referral.ReferredList[Route(nationalCode)].ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListReferralFeedbackPrescriptionsAsync(long masterId, string nationalCode, CancellationToken cancellationToken = default) => Soa("listReferralFeedbackPrescriptions", soa.Api.V2.Ep.FindNoteReferral[masterId][Route(nationalCode)].ToGetRequestInformation(), cancellationToken);
    public Task<TaminEnvelope<ReferralFeedbackEntriesDto>> ListReferralFeedbackEntriesAsync(string referralId, CancellationToken cancellationToken = default) =>
        Soa("listReferralFeedbackEntries", soa.Api.NoteDetailsReferral.Forward[Route(referralId)].ToGetRequestInformation(), SoaModels.ReferralListResponse.CreateFromDiscriminatorValue, Responses.MapReferralFeedbackEntries, cancellationToken);
    public Task<UnconstrainedResponse> ListReferralFeedbackQuestionsAsync(CancellationToken cancellationToken = default) => Api("listReferralFeedbackQuestions", api.Api.ReferralFeedback.QuestionList.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListReferralFeedbackAnswersAsync(CancellationToken cancellationToken = default) => Api("listReferralFeedbackAnswers", api.Api.ReferralFeedback.AnswerList.ToGetRequestInformation(), cancellationToken);

    public Task<TaminEnvelope<IReadOnlyList<HospitalizationOrderDto>>> ListHospitalizationOrdersAsync(string siamId, string secretaryNationalCode, CancellationToken cancellationToken = default) =>
        Soa("listHospitalizationOrders", soa.Api.NoteDetailsReferral.HospitalizationOrder[Route(siamId)][Route(secretaryNationalCode)].ToGetRequestInformation(), SoaModels.HospitalizationOrderResponse.CreateFromDiscriminatorValue, Responses.MapHospitalizationOrders, cancellationToken);
    public Task<TaminEnvelope<IReadOnlyList<HospitalizationOrderDto>>> ListPatientHospitalizationOrdersAsync(string siamId, string secretaryNationalCode, string patientNationalCode, CancellationToken cancellationToken = default) =>
        Soa("listPatientHospitalizationOrders", soa.Api.NoteDetailsReferral.HospitalizationOrder[Route(siamId)][Route(secretaryNationalCode)][Route(patientNationalCode)].ToGetRequestInformation(), SoaModels.HospitalizationOrderResponse.CreateFromDiscriminatorValue, Responses.MapHospitalizationOrders, cancellationToken);
    public Task<TaminEnvelope<IReadOnlyList<DoctorActiveCenterDto>>> ListDoctorActiveHospitalsAsync(CancellationToken cancellationToken = default) =>
        Soa("listDoctorActiveHospitals", soa.Api.NoteDetailsReferral.HospitalizationOrder.ActiveHospitals.ToGetRequestInformation(), SoaModels.DoctorActiveCenterListResponse.CreateFromDiscriminatorValue, Responses.MapDoctorActiveCenters, cancellationToken);
    public Task<TaminEnvelope<IReadOnlyList<DoctorActiveCenterDto>>> ListDoctorActiveClinicsAsync(CancellationToken cancellationToken = default) =>
        Soa("listDoctorActiveClinics", soa.Api.V2.DoctorActiveClinics.ToGetRequestInformation(), SoaModels.DoctorActiveCenterListResponse.CreateFromDiscriminatorValue, Responses.MapDoctorActiveCenters, cancellationToken);
    public Task<TaminEnvelope<FamilyDoctorShareDto>> CalculateFamilyDoctorPatientShareAsync(string siamId, string nationalCode, string docId, CancellationToken cancellationToken = default) =>
        Api("calculateFamilyDoctorPatientShare", api.Api.FamilyDoctor.ClinicReceptionAPI[Route(siamId)][Route(nationalCode)][Route(docId)].ToGetRequestInformation(), ApiModels.FamilyDoctorShareResponse.CreateFromDiscriminatorValue, Responses.MapFamilyDoctorShare, cancellationToken);

    public Task<UnconstrainedResponse> ListUnclaimedNursingPrescriptionsAsync(string siamId, string patientNationalCode, CancellationToken cancellationToken = default) => Soa("listUnclaimedNursingPrescriptions", soa.Api.V2.CartableNurse.GetAll[Route(siamId)][Route(patientNationalCode)].ToGetRequestInformation(), cancellationToken);
    public Task<NursingPrescriptionListDto> ListDoctorNursingPrescriptionsAsync(string siamId, string doctorNationalCode, string patientNationalCode, CancellationToken cancellationToken = default) =>
        Soa("listDoctorNursingPrescriptions", soa.Api.V2.CartableNurse.GetAll[Route(siamId)][Route(doctorNationalCode)][Route(patientNationalCode)].ToGetRequestInformation(), SoaModels.NursingPrescriptionListResponse.CreateFromDiscriminatorValue, Responses.MapDoctorNursingPrescriptions, cancellationToken);
    public Task<UnconstrainedResponse> SaveNursingActionsAsync(NursingActionInput input, CancellationToken cancellationToken = default) => Soa("saveNursingActions", soa.Ep.Api.V2.CartableNurse.Save.ToPostRequestInformation(Requests.MapPilot(input)), cancellationToken);

    public Task<TaminEnvelope<PatientDiseaseListDto>> ListPatientDiseasesAsync(string siamId, string nationalCode, CancellationToken cancellationToken = default) =>
        Api("listPatientDiseases", api.Api.SpecialDisease.Patient[Route(siamId)][Route(nationalCode)].ToGetRequestInformation(), ApiModels.PatientDiseaseListResponse.CreateFromDiscriminatorValue, Responses.MapPatientDiseases, cancellationToken);
    public Task<PrescriptionMutationResult> MarkPatientDiseaseAsync(PatientIllnessInput input, CancellationToken cancellationToken = default) =>
        Api("markPatientDisease", api.Api.Patient_illness.SavePatientInfoIllness.ToPostRequestInformation(Requests.MapPilot(input)), ApiModels.PrescriptionMutationResponse.CreateFromDiscriminatorValue, Responses.MapMarkPatientDisease, cancellationToken);
}
