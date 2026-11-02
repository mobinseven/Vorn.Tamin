using System.Globalization;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Tamin.Client.Account.Prod.Auth.Server.Authorize;
using Tamin.Integration.Auth;
using Tamin.Integration.Mapping;
using static Tamin.Integration.Http.TaminOperationSender;
using AccountModels = Tamin.Client.Account.Prod.Models;
using ApiModels = Tamin.Client.Api.Prod.Models;
using ProdAccountClient = Tamin.Client.Account.Prod.ProdAccountClient;
using ProdApiClient = Tamin.Client.Api.Prod.ProdApiClient;
using ProdSoaClient = Tamin.Client.Soa.Prod.ProdSoaClient;
using SoaModels = Tamin.Client.Soa.Prod.Models;
using SoaV2 = Tamin.Client.Soa.Prod.Interface.Epresc.SendEpresc.V2;

namespace Tamin.Integration;

internal sealed class ProductionTaminClient(ProdAccountClient account, ProdSoaClient soa, ProdApiClient api, TaminAdapters adapters, TaminTokenProvider tokens, string clientId) : ITaminClient
{
    private static readonly TaminMappingService Responses = new();
    private static readonly PrescriptionRequestMapper Requests = new();
    private SoaV2.V2RequestBuilder V2 => soa.Interface.Epresc.SendEpresc.V2;

    private Task<UnconstrainedResponse> Soa(string operationId, RequestInformation request, CancellationToken ct) => SendOpaqueAsync(adapters.Soa, operationId, request, ct);
    private Task<UnconstrainedResponse> Api(string operationId, RequestInformation request, CancellationToken ct) => SendOpaqueAsync(adapters.Api, operationId, request, ct);
    private Task<TResult> Soa<TModel, TResult>(string operationId, RequestInformation request, ParsableFactory<TModel> factory, Func<TModel, TResult> map, CancellationToken ct) where TModel : IParsable => SendAsync(adapters.Soa, operationId, request, factory, map, ct);
    private Task<TResult> Api<TModel, TResult>(string operationId, RequestInformation request, ParsableFactory<TModel> factory, Func<TModel, TResult> map, CancellationToken ct) where TModel : IParsable => SendAsync(adapters.Api, operationId, request, factory, map, ct);

    public Uri CreateAuthorizationUri(string redirectUri, string codeChallenge, string? state = null) => account.Auth.Server.Authorize.ToGetRequestInformation(c =>
    {
        c.QueryParameters.RedirectUri = Route(redirectUri);
        c.QueryParameters.CodeChallenge = Route(codeChallenge);
        c.QueryParameters.ClientId = clientId;
        c.QueryParameters.ResponseType = GetResponse_typeQueryParameterType.Code;
        c.QueryParameters.CodeChallengeMethod = GetCode_challenge_methodQueryParameterType.S256;
        c.QueryParameters.State = state;
    }).URI;

    public Uri CreateSignOutUri(string redirectUri) => account.Auth.Signout.ToGetRequestInformation(c => c.QueryParameters.RedirectUri = Route(redirectUri)).URI;

    public Task CompleteAuthorizationAsync(string code, string redirectUri, string codeVerifier, CancellationToken cancellationToken = default) => tokens.CompleteAuthorizationAsync(code, redirectUri, codeVerifier, cancellationToken);

    public Task<AccessTokenResult> ExchangeAuthorizationCodeAsync(string code, string redirectUri, string codeVerifier, CancellationToken cancellationToken = default) =>
        SendAsync(adapters.Account, "exchangeAuthorizationCode", account.Auth.Server.Token.ToPostRequestInformation(new AccountModels.AuthorizationCodeTokenRequest
        {
            Code = Route(code), RedirectUri = Route(redirectUri), CodeVerifier = Route(codeVerifier), ClientId = clientId,
            GrantType = AccountModels.AuthorizationCodeTokenRequest_grant_type.Authorization_code
        }), AccountModels.AccessTokenResponse.CreateFromDiscriminatorValue, r => new AccessTokenResult(r.AccessToken, r.ExpiresIn), cancellationToken);

    public Task<UnconstrainedResponse> ListPrescriptionTypesAsync(CancellationToken cancellationToken = default) => Soa("listPrescriptionTypes", V2.PrescriptionType.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListPrescriptionServiceTypesAsync(CancellationToken cancellationToken = default) => Soa("listPrescriptionServiceTypes", V2.ServiceType.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListServicesAsync(string serviceType, CancellationToken cancellationToken = default) => Soa("listServices", V2.Services.ToGetRequestInformation(c => c.QueryParameters.ServiceType = Route(serviceType)), cancellationToken);
    public Task<UnconstrainedResponse> ListLaboratoryTariffGroupsAsync(CancellationToken cancellationToken = default) => Soa("listLaboratoryTariffGroups", V2.ParTaref.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListDrugAmountsAsync(CancellationToken cancellationToken = default) => Soa("listDrugAmounts", V2.DrugAmount.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListDrugUsagesAsync(CancellationToken cancellationToken = default) => Soa("listDrugUsages", V2.DrugUsage.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListDrugInstructionsAsync(CancellationToken cancellationToken = default) => Soa("listDrugInstructions", V2.DrugInstruction.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListDosageFormsAsync(CancellationToken cancellationToken = default) => Api("listDosageForms", api.Ep.DosageForms.V1.ToGetRequestInformation(), cancellationToken);
    public Task<TaminEnvelope<MedicationInstructionsDto>> GetMedicationInstructionsByDosageFormAsync(string formCode, CancellationToken cancellationToken = default) =>
        Api("getMedicationInstructionsByDosageForm", api.Ep.MedicationInstructionsByDosageForm.V1[Route(formCode)].ToGetRequestInformation(), ApiModels.MedicationInstructionsResponse.CreateFromDiscriminatorValue, Responses.MapMedicationInstructionsByDosageForm, cancellationToken);
    public Task<UnconstrainedResponse> ListPhysiotherapyPlansAsync(CancellationToken cancellationToken = default) => Soa("listPhysiotherapyPlans", V2.PhPlan.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListPhysiotherapyIllnessesAsync(CancellationToken cancellationToken = default) => Soa("listPhysiotherapyIllnesses", V2.PhIllness.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListComplaintsAsync(CancellationToken cancellationToken = default) => Soa("listComplaints", V2.Complaint.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListIcd10DiagnosesAsync(CancellationToken cancellationToken = default) => Soa("listIcd10Diagnoses", V2.Icd10.GetAll.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListDoctorSpecialtiesAsync(CancellationToken cancellationToken = default) => Soa("listDoctorSpecialties", V2.Special.GetAll.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListDentistServicesWithoutToothAsync(int dentistType, CancellationToken cancellationToken = default) => Soa("listDentistServicesWithoutTooth", V2.FindDentistServiceNameWithoutTooth[dentistType].ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListDentistServicesByToothAsync(string selectedTooth, string patientId, int toothType, CancellationToken cancellationToken = default) => Soa("listDentistServicesByTooth", V2.FindDentistServiceNameByTooth[Route(selectedTooth)][Route(patientId)][toothType].ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> CheckDentistRulesAsync(DentistRuleInput input, CancellationToken cancellationToken = default) => Soa("checkDentistRules", V2.CheckRulesInDetail.ToPostRequestInformation(Requests.MapProduction(input)), cancellationToken);

    public Task<PrescriptionMutationResult> CreatePrescriptionAsync(PrescriptionCreateInput input, CancellationToken cancellationToken = default) =>
        Soa("createPrescription", V2.ToPostRequestInformation(Requests.MapProduction(input)), SoaModels.PrescriptionMutationResponse.CreateFromDiscriminatorValue, Responses.MapCreatePrescription, cancellationToken);
    public Task<UnconstrainedResponse> UpdatePrescriptionAsync(long headerId, string docNationalCode, string docId, IReadOnlyList<PrescriptionDetailInput> items, CancellationToken cancellationToken = default)
    {
        Route(docNationalCode); // Only the pilot route carries it.
        return Soa("updatePrescription", V2.Edit[headerId.ToString(CultureInfo.InvariantCulture)][Route(docId)].ToPostRequestInformation(Requests.MapProductionDetails(items)), cancellationToken);
    }
    public Task<UnconstrainedResponse> DeletePrescriptionAsync(long headerId, string docNationalCode, string docId, CancellationToken cancellationToken = default)
    {
        Route(docNationalCode); // Only the pilot route carries it.
        return Soa("deletePrescription", V2.Remove[headerId.ToString(CultureInfo.InvariantCulture)][Route(docId)].ToPostRequestInformation(), cancellationToken);
    }
    public Task<UnconstrainedResponse> GetPrescriptionDetailsAsync(long headerId, string docNationalCode, string docId, CancellationToken cancellationToken = default)
    {
        Route(docNationalCode); // Only the pilot route carries it.
        return Soa("getPrescriptionDetails", V2[headerId][Route(docId)].ToGetRequestInformation(), cancellationToken);
    }
    public Task<UnconstrainedResponse> GetPatientEntitlementAsync(string siamId, string docId, string patientId, string doctorNationalCode, CancellationToken cancellationToken = default)
    {
        Route(doctorNationalCode); // Only the pilot route carries it.
        return Soa("getPatientEntitlement", soa.Interface.Epresc.Patient.V2.DeserveInfo[Route(siamId)][Route(docId)][Route(patientId)].ToGetRequestInformation(), cancellationToken);
    }

    public Task<TaminEnvelope<ReferralCartableDto>> GetReferralCartableAsync(string docNationalCode, string patientNationalCode, int trackingCode, CancellationToken cancellationToken = default) =>
        Soa("getReferralCartable", V2.FetchReferralCartable[Route(docNationalCode)][Route(patientNationalCode)][trackingCode].ToGetRequestInformation(), SoaModels.ReferralCartableResponse.CreateFromDiscriminatorValue, Responses.MapReferralCartable, cancellationToken);
    public Task<TaminEnvelope<ReferralFeedbackDetailsDto>> GetReferralFeedbackDetailsAsync(long id, long masterParent, CancellationToken cancellationToken = default) =>
        Soa("getReferralFeedbackDetails", V2.Referral.NoteDetail[id][masterParent].ToGetRequestInformation(), SoaModels.ReferralFeedbackDetailResponse.CreateFromDiscriminatorValue, Responses.MapReferralFeedbackDetails, cancellationToken);

    // Kiota merged {nationalCode} into a sibling integer parameter, so its indexer takes a long and would drop leading zeros. The builder is created directly with the string value.
    public Task<UnconstrainedResponse> GetReferralPrescriptionAsync(string nationalCode, string docId, int trackingCode, CancellationToken cancellationToken = default) =>
        Soa("getReferralPrescription", new SoaV2.FindNoteReferral.Item.Item.Item.WithTrackingCodeItemRequestBuilder(new Dictionary<string, object>
        {
            ["baseurl"] = adapters.Soa.BaseUrl!, ["master%2Did"] = Route(nationalCode), ["doc%2Did"] = Route(docId), ["trackingCode"] = trackingCode
        }, adapters.Soa).ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> GetReferredServicePrescriptionAsync(string nationalCode, string docNationalCode, string docId, int trackingCode, CancellationToken cancellationToken = default)
    {
        Route(docNationalCode); // Only the pilot route carries it.
        return Soa("getReferredServicePrescription", new SoaV2.Item.Item.Item.ReferentialPrescDetail.ReferentialPrescDetailRequestBuilder(new Dictionary<string, object>
        {
            ["baseurl"] = adapters.Soa.BaseUrl!, ["header%2Did"] = Route(nationalCode), ["docId"] = Route(docId), ["trackingCode"] = trackingCode
        }, adapters.Soa).ToGetRequestInformation(), cancellationToken);
    }

    public Task<UnconstrainedResponse> GetOpenReferralCountAsync(string nationalCode, string docId, CancellationToken cancellationToken = default) => Soa("getOpenReferralCount", V2.NoteDetailsReferral.Count[Route(nationalCode)][Route(docId)].ToGetRequestInformation(), cancellationToken);
    public Task<TaminEnvelope<PatientOpenReferralsDto>> ListPatientOpenReferralsAsync(string nationalCode, CancellationToken cancellationToken = default) =>
        Soa("listPatientOpenReferrals", V2.PatientNoteDetailsReferral.Count[Route(nationalCode)].ToGetRequestInformation(), SoaModels.PatientOpenReferralsResponse.CreateFromDiscriminatorValue, Responses.MapPatientOpenReferrals, cancellationToken);
    public Task<UnconstrainedResponse> ListRecentDoctorReferralsAsync(string nationalCode, CancellationToken cancellationToken = default) => Soa("listRecentDoctorReferrals", V2.NoteDetailsReferral.ReferredList[Route(nationalCode)].ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListReferralFeedbackPrescriptionsAsync(long masterId, string nationalCode, CancellationToken cancellationToken = default) => Soa("listReferralFeedbackPrescriptions", V2.FindNoteReferral[masterId][Route(nationalCode)].ToGetRequestInformation(), cancellationToken);
    public Task<TaminEnvelope<ReferralFeedbackEntriesDto>> ListReferralFeedbackEntriesAsync(string referralId, CancellationToken cancellationToken = default) =>
        Soa("listReferralFeedbackEntries", V2.ReferralList[Route(referralId)].ToGetRequestInformation(), SoaModels.ReferralListResponse.CreateFromDiscriminatorValue, Responses.MapReferralFeedbackEntries, cancellationToken);
    public Task<UnconstrainedResponse> ListReferralFeedbackQuestionsAsync(CancellationToken cancellationToken = default) => Api("listReferralFeedbackQuestions", api.Ep.ReferralFeedbackQuestions.V1.ToGetRequestInformation(), cancellationToken);
    public Task<UnconstrainedResponse> ListReferralFeedbackAnswersAsync(CancellationToken cancellationToken = default) => Api("listReferralFeedbackAnswers", api.Ep.ReferralFeedbackResponses.V1.ToGetRequestInformation(), cancellationToken);

    public Task<TaminEnvelope<IReadOnlyList<HospitalizationOrderDto>>> ListHospitalizationOrdersAsync(string siamId, string secretaryNationalCode, CancellationToken cancellationToken = default) =>
        Soa("listHospitalizationOrders", V2.HospitalizationOrder.Fetch[Route(siamId)][Route(secretaryNationalCode)].ToGetRequestInformation(), SoaModels.HospitalizationOrderResponse.CreateFromDiscriminatorValue, Responses.MapHospitalizationOrders, cancellationToken);
    public Task<TaminEnvelope<IReadOnlyList<HospitalizationOrderDto>>> ListPatientHospitalizationOrdersAsync(string siamId, string secretaryNationalCode, string patientNationalCode, CancellationToken cancellationToken = default) =>
        Api("listPatientHospitalizationOrders", api.Ep.InpatientPrescriptions.V1[Route(siamId)][Route(secretaryNationalCode)][Route(patientNationalCode)].ToGetRequestInformation(), ApiModels.HospitalizationOrderResponse.CreateFromDiscriminatorValue, Responses.MapHospitalizationOrders, cancellationToken);
    public Task<TaminEnvelope<IReadOnlyList<DoctorActiveCenterDto>>> ListDoctorActiveHospitalsAsync(CancellationToken cancellationToken = default) =>
        Api("listDoctorActiveHospitals", api.Ep.DoctorActiveHospitals.V1.ToGetRequestInformation(), ApiModels.DoctorActiveCenterListResponse.CreateFromDiscriminatorValue, Responses.MapDoctorActiveCenters, cancellationToken);
    public Task<TaminEnvelope<IReadOnlyList<DoctorActiveCenterDto>>> ListDoctorActiveClinicsAsync(CancellationToken cancellationToken = default) =>
        Api("listDoctorActiveClinics", api.Ep.DoctorActiveClinics.V1.ToGetRequestInformation(), ApiModels.DoctorActiveCenterListResponse.CreateFromDiscriminatorValue, Responses.MapDoctorActiveCenters, cancellationToken);
    public Task<TaminEnvelope<FamilyDoctorShareDto>> CalculateFamilyDoctorPatientShareAsync(string siamId, string nationalCode, string docId, CancellationToken cancellationToken = default) =>
        Api("calculateFamilyDoctorPatientShare", api.Ep.PatientAdmission.V1[Route(siamId)][Route(nationalCode)][Route(docId)].ToGetRequestInformation(), ApiModels.FamilyDoctorShareResponse.CreateFromDiscriminatorValue, Responses.MapFamilyDoctorShare, cancellationToken);

    public Task<UnconstrainedResponse> ListUnclaimedNursingPrescriptionsAsync(string siamId, string patientNationalCode, CancellationToken cancellationToken = default) => Api("listUnclaimedNursingPrescriptions", api.Ep.NurseCartable.V1[Route(siamId)][Route(patientNationalCode)].ToGetRequestInformation(), cancellationToken);
    public Task<NursingPrescriptionListDto> ListDoctorNursingPrescriptionsAsync(string siamId, string doctorNationalCode, string patientNationalCode, CancellationToken cancellationToken = default) =>
        Api("listDoctorNursingPrescriptions", api.Ep.PatientNursingServices.V1[Route(siamId)][Route(doctorNationalCode)][Route(patientNationalCode)].ToGetRequestInformation(), ApiModels.NursingPrescriptionListResponse.CreateFromDiscriminatorValue, Responses.MapDoctorNursingPrescriptions, cancellationToken);
    public Task<UnconstrainedResponse> SaveNursingActionsAsync(NursingActionInput input, CancellationToken cancellationToken = default) => Api("saveNursingActions", api.Ep.NursingPrescriptionAction.V1.ToPostRequestInformation(Requests.MapProduction(input)), cancellationToken);

    public Task<TaminEnvelope<PatientDiseaseListDto>> ListPatientDiseasesAsync(string siamId, string nationalCode, CancellationToken cancellationToken = default) =>
        Api("listPatientDiseases", api.Ep.PatientDiseasesList.V1[Route(siamId)][Route(nationalCode)].ToGetRequestInformation(), ApiModels.PatientDiseaseListResponse.CreateFromDiscriminatorValue, Responses.MapPatientDiseases, cancellationToken);
    public Task<PrescriptionMutationResult> MarkPatientDiseaseAsync(PatientIllnessInput input, CancellationToken cancellationToken = default) =>
        Api("markPatientDisease", api.Ep.SavePatientInfoIllness.V1.ToPostRequestInformation(Requests.MapProduction(input)), ApiModels.PrescriptionMutationResponse.CreateFromDiscriminatorValue, Responses.MapMarkPatientDisease, cancellationToken);
}
