# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Moves the contract from the earlier `docs/EP-TAMIN-API.md` transcription (document 1.9.7) to the Tamin electronic prescription service guide v1.10.1 (1405/08/03). This release contains breaking changes; the next release must be a major version.

### Breaking changes
- Generated clients (`Tamin.Client.Account`, `Tamin.Client.Soa`, `Tamin.Client.Api`):
  - Production SOA routes that used `v7` now use `v2`: `SendEpresc/v7` (create), `noteDetailsReferral/count`, `findNoteReferral` (both), `noteDetailsReferral/referredList`, `fetchReferralCartable`, and `referral/noteDetail`. Pilot `/api/v7/...` routes (`ep/findNoteReferral`, `referral/referredList`, `cartable-nurse/get-all`, `ep/{nationalCode}/{docNatioanlCode}/{docId}/{trackingCode}/detail`) and `/ep/api/v7/cartable-nurse/save` are now `v2`. The request builders move with them (for example `...SendEpresc.V7...` becomes `...SendEpresc.V2...`).
  - Production nursing operations moved from the SOA client to the API client: `listUnclaimedNursingPrescriptions` is now `/ep/nurse-cartable/v1/{siamId}/{patientNationalCode}` (the path parameter `nationalCode` was renamed `patientNationalCode`) and `saveNursingActions` is now `/ep/nursing-prescription-action/v1`. `NursingActionRequest` is now in the API client models for production.
  - Production route `referentalPrescDetail` is now `referentialPrescDetail`.
  - Response field `complementary_Msg` is now `complemantary_Msg` on `PrescriptionMutationResponse` (the generated property `ComplementaryMsg` is now `ComplemantaryMsg`). The old spelling is no longer read.
  - `head_EPRSC_ID` on `PrescriptionMutationResponse` is declared `int64`: the generated `HeadEPRSCID` changes from `int?` to `long?`.
  - `PrescriptionCreateRequest`: `Complaint` is replaced by `NoteHeadEprscSupplementary`; `mobile` is no longer required.
  - `NoteDetailEprsc`: the stray `DiagnosisID` property is removed, and `noteDetailDrug` is now a `DiagnosisID` object (it was a free-form JSON object).
  - `ReferralCartableData` is reduced to `total` and `list`; the leaked `id`, `masterParent`, `masterChild` and `referralDate` members are removed.
  - `ParTarefGroup.parGrpCode` enum values change from the wire values `0.00` to `0.04` to `000` to `004`; the generated `ParTarefGroup_parGrpCode` member values on the wire change accordingly.
  - Pilot `AuthorizationCodeTokenRequest.ClientId` changes from `string?` to the enum `AuthorizationCodeTokenRequest_client_id`, and the authorize query parameter `ClientId` from `string?` to the enum `GetClient_idQueryParameterType`; the only value is `portal-js`.
  - `NoteDetailsReferralResponse`, `ReferralComplaintResponse` and `ReferralIcd10Response` replace the request-shaped models in `ReferralFeedbackPrescription.noteDetailsReferralList` and hospitalization-order responses.
- `Tamin.Integration`:
  - `PrescriptionCreateInput`: `Complaint` is replaced by `NoteHeadEprscSupplementary` (moved after `NoteDetailsReferralList`), `Mobile` is now `string?`, and `SiamId` and `Terminology` are new trailing parameters.
  - `PrescriptionDetailInput`: the `DiagnosisID` and dictionary-typed `NoteDetailDrug` parameters are replaced by a single `NoteDetailDrug` of type `DiagnosisIdInput`; `DoseCode` and `MethodUsage` are new. The parameter order changed.
  - `PrescriptionRequestMapper` no longer requires `Mobile`; it still rejects a blank (empty or whitespace) value.
  - `ReferralCartableDto` no longer has `Id`, `MasterParent`, `MasterChild` and `ReferralDate` (the guide does not document them at that level).
  - `HospitalizationOrderDto` has a new trailing `PatientNationalCode` parameter.
  - `INursingService.MapUnclaimedPrescriptions` and `MapSaveActions` now take `Tamin.Client.Api.Prod.Models.JsonObject` (they took the SOA model).
  - The number of opaque (`UnconstrainedResponse`) operations is now 30 (was 29).
- Contract tooling: `npm ci` now also installs `ajv` and `js-yaml`, which are declared as devDependencies.

### Added
- Seven operations in production and pilot (47 per environment, was 40):
  - `listDosageForms`, `getMedicationInstructionsByDosageForm`, `listReferralFeedbackEntries`, `listDoctorNursingPrescriptions`, `listPatientHospitalizationOrders`, `listDoctorActiveHospitals`, `listDoctorActiveClinics`.
  - `TaminMappingService`: `MapDosageForms`, `MapMedicationInstructionsByDosageForm`, `MapReferralFeedbackEntries`, `MapDoctorNursingPrescriptions`, `MapHospitalizationOrders(Api...)` and `MapDoctorActiveCenters`, with pilot overloads where the pilot models differ.
- Request fields `terminology`, `siamId`, `noteHeadEprscSupplementary`, `doseCode`, `methodUsage` and `Special.terminology`, mapped by `PrescriptionRequestMapper`.
- Response fields `parTarefGrp` on `ServiceDetails`, and the nursing-prescription item members the guide documents (`noteDetailDrug`, `timesAday`, `dose`, `doseCode`, `drugInstruction`, `illnessId`, `planId`, `drugAmntId`, `drugInstId`, `toothId`, `referenceStatus`).
- Conformance checks against the guide: `openapi/tamin-route-manifest.json`, `scripts/verify-tamin-conformance.ps1` and the sample payloads in `tests/tamin-contract-fixtures`; `verify-tamin-contracts.ps1` runs them.

### Changed
- Removed public placeholder client surfaces that were not backed by the Tamin service guide or generated Kiota paths.
- Removed `TaminSession.Identity`, `TaminSession.Pharmacy`, and `TaminSession.Paraclinic` because their standalone workflows are not implemented from the provider contract.
- Updated implementation-status documentation to describe only contract-backed public SDK surfaces.

### Fixed
- Corrected stale documentation and changelog claims for non-existent identity, pharmacy, paraclinic, pricing, allowed-count, and entitlement helper APIs.

## [0.1.0] - Initial Release

### Added
- `TaminSession` with bearer token and username/password login.
- `ServiceClient`: `GetAllServicesAsync`, `GetPrescriptionTypeAsync`, `GetParaclinicTarefAsync`, `GetDrugAmountAsync`, `GetDrugInstructionAsync`.
- `PrescriptionClient`: `CreatePrescriptionAsync<T>`, `GetPrescriptionDetailAsync`.
- HTTP exception hierarchy mirroring the Python tamin-sdk.
- `PrescriptionType` enum and `DocEprsc` type.
