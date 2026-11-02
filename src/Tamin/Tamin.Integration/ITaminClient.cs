using Tamin.Integration.Mapping;

namespace Tamin.Integration;

/// <summary>
/// Environment-neutral Tamin operations, one member per contract operation (named after its operationId).
/// Every parameter is required in both environments, including those only the pilot route carries,
/// so calling code does not change between pilot and production.
/// Non-success responses throw <see cref="Http.TaminApiException"/>. Only GET operations are retried.
/// </summary>
public interface ITaminClient
{
    /// <summary>beginAuthorization: the URL to send the doctor's browser to. Makes no HTTP call.</summary>
    Uri CreateAuthorizationUri(string redirectUri, string codeChallenge, string? state = null);
    /// <summary>signOutUser: the URL to send the doctor's browser to. Makes no HTTP call.</summary>
    Uri CreateSignOutUri(string redirectUri);
    /// <summary>exchangeOrRefreshDoctorToken: stores the doctor token used by later calls. Refresh is automatic.</summary>
    Task CompleteAuthorizationAsync(string code, string redirectUri, string codeVerifier, CancellationToken cancellationToken = default);
    /// <summary>exchangeAuthorizationCode: the v1 token route. The result is not used by later calls.</summary>
    Task<AccessTokenResult> ExchangeAuthorizationCodeAsync(string code, string redirectUri, string codeVerifier, CancellationToken cancellationToken = default);

    Task<UnconstrainedResponse> ListPrescriptionTypesAsync(CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> ListPrescriptionServiceTypesAsync(CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> ListServicesAsync(string serviceType, CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> ListLaboratoryTariffGroupsAsync(CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> ListDrugAmountsAsync(CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> ListDrugUsagesAsync(CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> ListDrugInstructionsAsync(CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> ListDosageFormsAsync(CancellationToken cancellationToken = default);
    Task<TaminEnvelope<MedicationInstructionsDto>> GetMedicationInstructionsByDosageFormAsync(string formCode, CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> ListPhysiotherapyPlansAsync(CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> ListPhysiotherapyIllnessesAsync(CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> ListComplaintsAsync(CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> ListIcd10DiagnosesAsync(CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> ListDoctorSpecialtiesAsync(CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> ListDentistServicesWithoutToothAsync(int dentistType, CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> ListDentistServicesByToothAsync(string selectedTooth, string patientId, int toothType, CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> CheckDentistRulesAsync(DentistRuleInput input, CancellationToken cancellationToken = default);

    Task<PrescriptionMutationResult> CreatePrescriptionAsync(PrescriptionCreateInput input, CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> UpdatePrescriptionAsync(long headerId, string docNationalCode, string docId, IReadOnlyList<PrescriptionDetailInput> items, CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> DeletePrescriptionAsync(long headerId, string docNationalCode, string docId, CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> GetPrescriptionDetailsAsync(long headerId, string docNationalCode, string docId, CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> GetPatientEntitlementAsync(string siamId, string docId, string patientId, string doctorNationalCode, CancellationToken cancellationToken = default);

    Task<TaminEnvelope<ReferralCartableDto>> GetReferralCartableAsync(string docNationalCode, string patientNationalCode, int trackingCode, CancellationToken cancellationToken = default);
    Task<TaminEnvelope<ReferralFeedbackDetailsDto>> GetReferralFeedbackDetailsAsync(long id, long masterParent, CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> GetReferralPrescriptionAsync(string nationalCode, string docId, int trackingCode, CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> GetReferredServicePrescriptionAsync(string nationalCode, string docNationalCode, string docId, int trackingCode, CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> GetOpenReferralCountAsync(string nationalCode, string docId, CancellationToken cancellationToken = default);
    Task<TaminEnvelope<PatientOpenReferralsDto>> ListPatientOpenReferralsAsync(string nationalCode, CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> ListRecentDoctorReferralsAsync(string nationalCode, CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> ListReferralFeedbackPrescriptionsAsync(long masterId, string nationalCode, CancellationToken cancellationToken = default);
    Task<TaminEnvelope<ReferralFeedbackEntriesDto>> ListReferralFeedbackEntriesAsync(string referralId, CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> ListReferralFeedbackQuestionsAsync(CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> ListReferralFeedbackAnswersAsync(CancellationToken cancellationToken = default);

    Task<TaminEnvelope<IReadOnlyList<HospitalizationOrderDto>>> ListHospitalizationOrdersAsync(string siamId, string secretaryNationalCode, CancellationToken cancellationToken = default);
    Task<TaminEnvelope<IReadOnlyList<HospitalizationOrderDto>>> ListPatientHospitalizationOrdersAsync(string siamId, string secretaryNationalCode, string patientNationalCode, CancellationToken cancellationToken = default);
    Task<TaminEnvelope<IReadOnlyList<DoctorActiveCenterDto>>> ListDoctorActiveHospitalsAsync(CancellationToken cancellationToken = default);
    Task<TaminEnvelope<IReadOnlyList<DoctorActiveCenterDto>>> ListDoctorActiveClinicsAsync(CancellationToken cancellationToken = default);
    Task<TaminEnvelope<FamilyDoctorShareDto>> CalculateFamilyDoctorPatientShareAsync(string siamId, string nationalCode, string docId, CancellationToken cancellationToken = default);

    Task<UnconstrainedResponse> ListUnclaimedNursingPrescriptionsAsync(string siamId, string patientNationalCode, CancellationToken cancellationToken = default);
    Task<NursingPrescriptionListDto> ListDoctorNursingPrescriptionsAsync(string siamId, string doctorNationalCode, string patientNationalCode, CancellationToken cancellationToken = default);
    Task<UnconstrainedResponse> SaveNursingActionsAsync(NursingActionInput input, CancellationToken cancellationToken = default);

    Task<TaminEnvelope<PatientDiseaseListDto>> ListPatientDiseasesAsync(string siamId, string nationalCode, CancellationToken cancellationToken = default);
    Task<PrescriptionMutationResult> MarkPatientDiseaseAsync(PatientIllnessInput input, CancellationToken cancellationToken = default);
}
