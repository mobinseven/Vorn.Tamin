# Tamin OpenAPI audit

## Scope and interpretation

The sole contract source for this audit and both specifications is the attached `EP-TAMIN-API(1).md`, titled *راهنمای سرویس نسخه نویسی الکترونیک سازمان تامین اجتماعی*, version 1.9.7 dated 1405/04/20. No live endpoint, external documentation, generated client, or prior contract was used to fill gaps.

The source documents 41 logical operations for each environment. Two of those logical operations—the doctor authorization-code exchange and doctor refresh-token exchange—use the same method and route, `POST /auth/server/v2/token`. OpenAPI permits only one operation per method/path pair, so each specification represents that route once as `exchangeOrRefreshDoctorToken`, with discriminated `authorization_code` and `refresh_token` form schemas. Both logical source operations retain separate coverage rows below.

Server aliases used in the coverage table:

| Alias | URL |
| --- | --- |
| `PROD-ACCOUNT` | `https://account.tamin.ir` |
| `PROD-SOA` | `https://soa.tamin.ir` |
| `PROD-API` | `https://api.tamin.ir` |
| `PILOT-ACCOUNT` | `https://account-pilot.tamin.ir` |
| `PILOT-API` | `https://ep-test.tamin.ir` |

Status meanings:

- **Complete success**: the documented success payload is modeled from a field table or concrete example; undocumented error responses remain noted below.
- **Partial**: the route and documented request contract are covered, but the source omits some response or request details.
- **Ambiguous**: the source contains a contradiction, typo, inconsistent example, or a material type/name uncertainty. The chosen representation is disclosed below.

## Operation coverage

| # | Source section and operation | Method | Production path | Production `operationId` | Pilot path | Pilot `operationId` | Status |
| ---: | --- | :---: | --- | --- | --- | --- | --- |
| 1 | 1-2 authorization code request | GET | `PROD-ACCOUNT /auth/server/authorize` | `beginAuthorization` | `PILOT-ACCOUNT /auth/server/authorize` | `beginAuthorization` | **Ambiguous** — browser HTML/redirect status and failure contract absent. |
| 2 | 2-2 access-token exchange | POST | `PROD-ACCOUNT /auth/server/token` | `exchangeAuthorizationCode` | `PILOT-ACCOUNT /auth/server/token` | `exchangeAuthorizationCode` | **Partial** — success example only; token/error metadata incomplete. |
| 3 | 3-2 sign-out | GET | `PROD-ACCOUNT /auth/signout` | `signOutUser` | `PILOT-ACCOUNT /auth/signout` | `signOutUser` | **Partial** — redirect status and payload absent. |
| 4 | 1-4-2 doctor token exchange | POST | `PROD-ACCOUNT /auth/server/v2/token` | `exchangeOrRefreshDoctorToken` | `PILOT-ACCOUNT /auth/server/v2/token` | `exchangeOrRefreshDoctorToken` | **Ambiguous** — consolidated route variant; curl incorrectly shows `/v1/token`. |
| 5 | 2-4-2 doctor refresh-token exchange | POST | `PROD-ACCOUNT /auth/server/v2/token` | `exchangeOrRefreshDoctorToken` | `PILOT-ACCOUNT /auth/server/v2/token` | `exchangeOrRefreshDoctorToken` | **Partial** — consolidated route variant; refreshed-token response fields absent. |
| 6 | 3 create electronic prescription | POST | `PROD-SOA /interface/epresc/SendEpresc/v7` | `createPrescription` | `PILOT-API /api/v2/SendEpresc` | `createPrescription` | **Ambiguous** — request wire-name/type conflicts and only indirect response definition. |
| 7 | 4 patient entitlement | GET | `PROD-SOA /interface/epresc/patient/v2/deserve-info/{siam-id}/{doc-id}/{patient-id}` | `getPatientEntitlement` | `PILOT-API /api/v2/patients/deserve-info/{requestBy}/{siam-id}/{doc-id}/{patient-id}` | `getPatientEntitlement` | **Partial** — response omitted. |
| 8 | 5 prescription reaction/details | GET | `PROD-SOA /interface/epresc/SendEpresc/v2/{headerID}/{docId}` | `getPrescriptionDetails` | `PILOT-API /api/v2/ep/{headerID}/{docNationalCode}/{docId}/detail` | `getPrescriptionDetails` | **Partial** — response described only as prescription details. |
| 9 | 6 delete prescription | POST | `PROD-SOA /interface/epresc/SendEpresc/v2/remove/{headerID}/{docId}` | `deletePrescription` | `PILOT-API /api/v2/ep/{headerID}/{docNationalCode}/{docId}` | `deletePrescription` | **Partial** — response omitted. |
| 10 | 7 edit prescription | POST | `PROD-SOA /interface/epresc/SendEpresc/v2/edit/{headerID}/{docId}` | `updatePrescription` | `PILOT-API /api/v2/ep/update/{headerID}/{docNationalCode}/{docId}` | `updatePrescription` | **Ambiguous** — source says “list of items” without showing whether the JSON body is a raw array or wrapper. |
| 11 | 8 prescription types | GET | `PROD-SOA /interface/epresc/SendEpresc/v2/prescription-type` | `listPrescriptionTypes` | `PILOT-API /api/v2/ws-prescription-type` | `listPrescriptionTypes` | **Partial** — response omitted. |
| 12 | 8 prescription item/service types | GET | `PROD-SOA /interface/epresc/SendEpresc/v2/service-type` | `listPrescriptionServiceTypes` | `PILOT-API /api/v2/ws-service-type` | `listPrescriptionServiceTypes` | **Partial** — response omitted. |
| 13 | 8 drug/paraclinical/service coding | GET | `PROD-SOA /interface/epresc/SendEpresc/v2/services` | `listServices` | `PILOT-API /api/v2/ws-services` | `listServices` | **Partial** — response omitted; query casing differs by environment. |
| 14 | 8 laboratory subgroups | GET | `PROD-SOA /interface/epresc/SendEpresc/v2/par-taref` | `listLaboratoryTariffGroups` | `PILOT-API /api/v2/ws-par-taref` | `listLaboratoryTariffGroups` | **Partial** — response omitted. |
| 15 | 8 drug amounts | GET | `PROD-SOA /interface/epresc/SendEpresc/v2/drug-amount` | `listDrugAmounts` | `PILOT-API /api/v2/ws-drug-amount` | `listDrugAmounts` | **Partial** — response omitted. |
| 16 | 8 drug usage methods | GET | `PROD-SOA /interface/epresc/SendEpresc/v2/drug-usage` | `listDrugUsages` | `PILOT-API /api/v2/ws-drug-usage` | `listDrugUsages` | **Partial** — response omitted. |
| 17 | 8 drug administration times | GET | `PROD-SOA /interface/epresc/SendEpresc/v2/drug-instruction` | `listDrugInstructions` | `PILOT-API /api/v2/ws-drug-instruction` | `listDrugInstructions` | **Partial** — response omitted. |
| 18 | 8 physiotherapy plans | GET | `PROD-SOA /interface/epresc/SendEpresc/v2/ph-plan` | `listPhysiotherapyPlans` | `PILOT-API /api/v2/ws-ph-plan` | `listPhysiotherapyPlans` | **Partial** — response omitted. |
| 19 | 8 physiotherapy illnesses | GET | `PROD-SOA /interface/epresc/SendEpresc/v2/ph-illness` | `listPhysiotherapyIllnesses` | `PILOT-API /api/v2/ws-ph-illness` | `listPhysiotherapyIllnesses` | **Partial** — response omitted. |
| 20 | 8 complaints | GET | `PROD-SOA /interface/epresc/SendEpresc/v2/complaint` | `listComplaints` | `PILOT-API /api/complaint` | `listComplaints` | **Partial** — response omitted. |
| 21 | 8 ICD-10 initial diagnoses | GET | `PROD-SOA /interface/epresc/SendEpresc/v2/icd10/getAll` | `listIcd10Diagnoses` | `PILOT-API /api/icd10/getAll` | `listIcd10Diagnoses` | **Partial** — response omitted. |
| 22 | 8 doctor specialties | GET | `PROD-SOA /interface/epresc/SendEpresc/v2/special/getAll` | `listDoctorSpecialties` | `PILOT-API /api/specials/getAll` | `listDoctorSpecialties` | **Partial** — response omitted. |
| 23 | 1-9 check dentistry rules | POST | `PROD-SOA /interface/epresc/SendEpresc/v2/check-rules-in-detail` | `checkDentistRules` | `PILOT-API /api/v2/check-rules-in-detail` | `checkDentistRules` | **Ambiguous** — `allGridData` is typed as one object in the table but described as a list in prose. |
| 24 | 1-2-9 dentistry services without tooth | GET | `PROD-SOA /interface/epresc/SendEpresc/v2/find-dentist-service-name-without-tooth/{dentistType}` | `listDentistServicesWithoutTooth` | `PILOT-API /api/v2/ws-dentist-base/find-dentist-service-name-without-tooth/{dentistType}` | `listDentistServicesWithoutTooth` | **Partial** — response omitted. |
| 25 | 2-2-9 dentistry services by tooth | GET | `PROD-SOA /interface/epresc/SendEpresc/v2/find-dentist-service-name-by-tooth/{selectedTooth}/{patientId}/{toothType}` | `listDentistServicesByTooth` | `PILOT-API /api/v2/ws-dentist-base/find-dentist-service-name-by-tooth/{selectedTooth}/{patientId}/{toothType}` | `listDentistServicesByTooth` | **Partial** — response omitted. |
| 26 | 1-10 open-referral count | GET | `PROD-SOA /interface/epresc/SendEpresc/v7/noteDetailsReferral/count/{nationalCode}/{docId}` | `getOpenReferralCount` | `PILOT-API /api/v2/referral/count/{nationalCode}/{docId}` | `getOpenReferralCount` | **Partial** — output concepts given, but wire field names/types omitted. |
| 27 | 2-10 referral prescription reaction | GET | `PROD-SOA /interface/epresc/SendEpresc/v7/findNoteReferral/{nationalCode}/{docId}/{trackingCode}` | `getReferralPrescription` | `PILOT-API /api/v7/ep/findNoteReferral/{nationalCode}/{docId}/{trackingCode}` | `getReferralPrescription` | **Partial** — response omitted. |
| 28 | 3-10 prescriptions with referral feedback | GET | `PROD-SOA /interface/epresc/SendEpresc/v7/findNoteReferral/{masterId}/{nationalCode}` | `listReferralFeedbackPrescriptions` | `PILOT-API /api/v7/ep/findNoteReferral/{masterId}/{nationalCode}` | `listReferralFeedbackPrescriptions` | **Partial** — response omitted. |
| 29 | 4-10 recent doctor referrals | GET | `PROD-SOA /interface/epresc/SendEpresc/v7/noteDetailsReferral/referredList/{nationalCode}` | `listRecentDoctorReferrals` | `PILOT-API /api/v7/referral/referredList/{nationalCode}` | `listRecentDoctorReferrals` | **Partial** — method is inferred from neighboring retrieval services; response omitted. |
| 30 | 1-5-10 referral cartable | GET | `PROD-SOA /interface/epresc/SendEpresc/v7/fetchReferralCartable/{docNationalCode}/{patientNationalCode}/{trackingCode}` | `getReferralCartable` | `PILOT-API /api/cartablenoteDetailsReferral/{docNationalCode}/{patientNationalCode}/{trackingCode}` | `getReferralCartable` | **Ambiguous** — success example modeled, but placeholder scalar types and `patientID` description are suspicious. |
| 31 | 3-5-10 referral-feedback details | GET | `PROD-SOA /interface/epresc/SendEpresc/v7/referral/noteDetail/{id}/{masterParent}` | `getReferralFeedbackDetails` | `PILOT-API /api/noteDetailsReferral/noteheads/{id}/{masterParent}` | `getReferralFeedbackDetails` | **Ambiguous** — declared production route uses v7 while both call examples use v2. |
| 32 | 6-10 patient open referrals for secretary | GET | `PROD-SOA /interface/epresc/SendEpresc/v2/patientNoteDetailsReferral/count/{nationalCode}` | `listPatientOpenReferrals` | `PILOT-API /api/v2/referral/count/{nationalCode}` | `listPatientOpenReferrals` | **Complete success** — documented success example modeled. |
| 33 | 7-10 referral-feedback questions | GET | `PROD-API /ep/referral-feedback-questions/v1` | `listReferralFeedbackQuestions` | `PILOT-API /api/referral-feedback/question-list` | `listReferralFeedbackQuestions` | **Partial** — current IDs/text are listed, but response wire schema is absent and values may change. |
| 34 | 8-10 referral-feedback answers | GET | `PROD-API /ep/referral-feedback-responses/v1` | `listReferralFeedbackAnswers` | `PILOT-API /api/referral-feedback/answer-list` | `listReferralFeedbackAnswers` | **Partial** — current IDs/text are listed, but response wire schema is absent and values may change. |
| 35 | 1-11 nursing cartable | GET | `PROD-SOA /interface/epresc/SendEpresc/v7/cartableNurse/getAll/{siamId}/{nationalCode}` | `listUnclaimedNursingPrescriptions` | `PILOT-API /api/v7/cartable-nurse/get-all/{siamId}/{nationalCode}` | `listUnclaimedNursingPrescriptions` | **Partial** — response omitted. |
| 36 | 2-11 save nursing action | POST | `PROD-SOA /interface/epresc/SendEpresc/v7/cartableNurse/saveEprsc` | `saveNursingActions` | `PILOT-API /ep/api/v7/cartable-nurse/save` | `saveNursingActions` | **Partial** — request example modeled; response omitted. |
| 37 | 1-12 referred-service prescription reaction | GET | `PROD-SOA /interface/epresc/SendEpresc/v2/{nationalCode}/{docId}/{trackingCode}/referentalPrescDetail` | `getReferredServicePrescription` | `PILOT-API /api/v7/ep/{nationalCode}/{docNatioanlCode}/{docId}/{trackingCode}/detail` | `getReferredServicePrescription` | **Ambiguous** — pilot route contains `docNatioanlCode`, but the parameter list omits it; response omitted. |
| 38 | 1-13 hospitalization orders | GET | `PROD-SOA /interface/epresc/SendEpresc/v2/hospitalization-order/fetch/{siam-id}/{secretaryNationalCode}` | `listHospitalizationOrders` | `PILOT-API /api/noteDetailsReferral/hospitalization-order/{siam-id}/{secretaryNationalCode}` | `listHospitalizationOrders` | **Complete success** — documented success example, including `docFamily`, modeled. |
| 39 | 1-14 family-doctor patient share | GET | `PROD-API /ep/patient-admission/v1/{siamid}/{nationalCode}/{docId}` | `calculateFamilyDoctorPatientShare` | `PILOT-API /api/familyDoctor/clinicReceptionAPI/{siamid}/{nationalCode}/{docId}` | `calculateFamilyDoctorPatientShare` | **Complete success** — both documented success variants modeled. |
| 40 | 1-15 selectable patient diseases | GET | `PROD-API /ep/patient-diseases-list/v1/{siamId}/{nationalCode}` | `listPatientDiseases` | `PILOT-API /api/special-disease/patient/{siamId}/{nationalCode}` | `listPatientDiseases` | **Complete success** — documented success example modeled. |
| 41 | 2-15 mark patient disease | POST | `PROD-API /ep/save-patient-info-illness/v1` | `markPatientDisease` | `PILOT-API /api/patient_illness/save-patient-info-illness` | `markPatientDisease` | **Partial** — request and output field table modeled; HTTP statuses, exact success values, and error codes absent. |

## Modeling assumptions and disclosed resolutions

1. **Authentication representation.** The document defines PKCE authorization-code and refresh flows and says the resulting token is used by the remaining services. It does not document an `Authorization` header format or scopes. The specifications therefore use an OAuth 2.0 authorization-code security scheme with an empty scope set, apply it to non-authentication operations, and do not assert a Bearer header contract.
2. **Success status and media type.** Except where examples contain `status: 200`, most operations do not state HTTP status codes or response media types. JSON-facing operations use a `200` success slot for deterministic Kiota method generation. When the payload is absent, that slot references `JsonObject`, an explicitly unconstrained object. This is a client-generation convention, not a claim that the source guarantees HTTP 200 or an object envelope.
3. **Retrieval methods not always stated.** The 12 reference-data URLs and the “recent doctor referrals” URL do not each state a method explicitly. They are modeled as `GET` because they are described solely as list/retrieval services, expose no request body, and neighboring retrieval operations are explicitly GET.
4. **Edit request body.** “The input is the list of registered prescription items” is represented as a raw JSON array of `NoteDetailEprsc`. The source provides no wrapper wire name or edit-body example.
5. **Request requiredness.** Fields marked with `★` or `*` are required. Authentication fields stated as fields that “must” be sent are required. For prescription creation, `patient`, `prescDate`, doctor identity fields, and `prescType` are treated as required inputs; `mobile` is required because the source defines a missing-value error and includes it in the dentistry example even though it is absent from the main field table. Conditional fields are left optional and documented rather than expressed with complex JSON Schema conditionals, which improves Kiota interoperability and avoids inventing conditions not fully specified.
6. **Pilot-only prescription field.** Pilot `clientId` is required and described as equal to the doctor national code, exactly as stated. It is absent from the production request schema.
7. **Canonical item-list wire names.** `noteDetailEprscs` and `noteDetailsReferralList` are used because the schema heading, prose, and multiple examples support them. The table cells `List< NoteDetailEprsc>` / `List<NoteDetailsReferral>` are treated as Java-like type text rather than literal wire names. The one-off `noteDetailExprsc` example spelling is treated as a typo. All discrepancies remain listed below.
8. **Fields found only in examples.** `mobile`, `creatorType`, `referralHijriDate`, `noteDetailsEprscId`, `referenceStatus`, and `referralFeedbackList` are retained because they are documented wire members in examples or explanatory prose. `creatorType` remains optional and unconstrained because its meaning is never defined.
9. **Referral date naming conflict.** `NoteDetailsReferral.id` (described in the table as a date-like value) and `referralHijriDate` (used in examples) are both retained as optional properties. No mutual-exclusion or precedence rule was invented.
10. **Complaint shapes.** The general `Complaint` schema preserves `complaintIDs`. Referral examples instead send `complaints[].id`; `ReferralComplaint` exposes both documented spellings without requiring either because the source never reconciles them.
11. **Dentistry grid shape.** `allGridData` is modeled as an array because the prose explicitly calls it a list of `GridData`, despite the table displaying a singular type.
12. **`isDentalService` type.** It is modeled as string enum `'0' | '1'` because both the field table and prose explicitly say `String`. The dentistry JSON example sends numeric `1` and is treated as a suspected example error rather than widening the client property to an incompatible primitive union.
13. **Localized numeric text.** Eight-character Solar Hijri examples use ASCII digits in JSON/code blocks, so `JalaliDate` uses `^[0-9]{8}$`. `ParTarefGroup` values printed with Persian digits are normalized to the corresponding ASCII wire strings (`0.00` through `0.04`). This normalization should be confirmed with Tamin.
14. **Identifiers.** Fields declared `Long` and identifiers demonstrated above 32-bit range use `int64`. Fields declared only `Integer` have no OpenAPI integer format unless an observed value requires `int64`. `trackingCode` is integer because the output field table declares it as such.
15. **Dynamic referral feedback values.** Question IDs 1–5 and answer IDs 100/101 are provided as schema examples, not closed enums, because sections 7-10 and 8-10 explicitly say these lists may change and must be fetched from their endpoints.
16. **Response extensibility and nullability.** Documented response fields and explicit `null` values are modeled, while response objects permit additional properties because the examples cannot prove a closed payload. Request objects also permit additional properties because the document never states that unknown JSON fields are rejected. Null unions are used only where the source example shows `null` or the response field is explicitly success/failure-dependent.
17. **Multiple production hosts.** Per-operation `servers` entries distinguish `account.tamin.ir`, `soa.tamin.ir`, and `api.tamin.ir`; pilot authentication uses `account-pilot.tamin.ir` and all other pilot paths use `ep-test.tamin.ir`. This preserves routes without merging their base paths.

## Suspected documentation errors and contradictions

| ID | Source observation | Specification treatment |
| --- | --- | --- |
| D-01 | The doctor-token section declares `/auth/server/v2/token`, but its first curl example calls `/auth/server/v1/token`. | Uses the repeatedly declared v2 route; flags both doctor flows as ambiguous. |
| D-02 | The first authorization example contains a bullet and a space inside `code_challenge`, which contradicts the documented PKCE construction. | Does not reproduce the invalid example; the parameter remains a string described as Base64URL(SHA-256). |
| D-03 | The legacy token JSON example contains invalid JSON escape sequences inside `access_token`. | Uses a shortened valid string example and records only documented fields. |
| D-04 | `code_verifier` examples contain `:` and backslash characters, contradicting the explicitly allowed character set. | Enforces the documented 43–128-character `[A-Za-z0-9._~-]` constraint. |
| D-05 | Main prescription table shows `List< NoteDetailEprsc>` and `List<NoteDetailsReferral>` in the “parameter name” column; examples/prose use `noteDetailEprscs`, `noteDetailsReferralList`, and once `noteDetailExprsc`. | Uses the repeated camel-case wire names; records the one-off spelling as a typo. |
| D-06 | The request table omits `mobile`, while the validation section makes it mandatory and the dentistry example sends it. | Includes required `mobile`; no regex is invented because “correct pattern” is not defined. |
| D-07 | `isDentalService` is declared and described as a string, but the JSON example sends a number. | Uses string enum `'0' | '1'`; numeric example is not embedded as a valid OpenAPI example. |
| D-08 | `allGridData` is typed as singular `GridData` in the table but described as a list in prose. | Uses `GridData[]`. |
| D-09 | `NoteDetailsReferral.id` is described as a referral date, while all request examples use `referralHijriDate`. | Retains both optional properties; requires confirmation. |
| D-10 | Referral complaint structure says `complaintIDs`, while referral JSON sends `complaints[].id`. | Retains both documented members in the referral complaint model. |
| D-11 | `DiagnosisID.icdId` is declared integer, while referral `icd10s[].icdId` examples contain ICD-10 strings such as `X46.49`. | Uses separate integer `DiagnosisID` and string `ReferralIcd10` schemas. |
| D-12 | The referral-cartable example labels `patientID` as “doctor national code,” inconsistent with the field name and operation inputs. | Preserves the field as string and flags its semantic description as untrusted. |
| D-13 | Referral-feedback detail declares a production v7 route, but both concrete invocation examples use v2. | Uses the formally declared v7 route; v2 examples are not added as undocumented alternatives. |
| D-14 | Section numbering jumps from 1-5-10 to 3-5-10. | No missing operation is invented. |
| D-15 | Pilot referred-service route spells the parameter `docNatioanlCode`; the accompanying parameter list omits that placeholder entirely. | Preserves exact route spelling and adds the required matching path parameter, described as doctor national code. |
| D-16 | Pilot nursing-save route uniquely contains `/ep/api/` instead of the otherwise common `/api/` prefix. | Preserves `/ep/api/v7/cartable-nurse/save` exactly and flags it for confirmation. |
| D-17 | Production referred-service path uses the spelling `referentalPrescDetail`. | Preserves the exact documented wire path. |
| D-18 | Path parameter casing varies among `siamId`, `siam-id`, and `siamid`, and among `docId`, `doc-id`, and `docID`. | Preserves the exact casing/hyphenation of every route template and response field. |
| D-19 | The open-referral count response is described as “count and tracking code” without wire field names, types, envelope, or example. | Leaves the response payload unconstrained rather than inventing properties. |
| D-20 | The mark-patient response is said to match prescription creation, but the creation section itself does not define the full HTTP response; only a later field table does. | Reuses `PrescriptionMutationResponse` for both and leaves all fields optional/success-or-failure dependent. |

## Finalization status

The audit remediation is final for offline repository verification. D-05 through D-11 are owned by the handwritten `PrescriptionCreateInput`/`PrescriptionRequestMapper` boundary and executable tests. The six documented success contracts are asserted field-for-field for production and exercised against equivalent pilot generated models. The 29 schema-less operations remain opaque. The pinned verifier validates, lints, dereferences, checks route/parameter fidelity, regenerates all six clients, and rejects nondeterministic output.

Two explicit exceptions remain:

1. Live pilot token refresh is not executed because owner-supplied pilot credentials are unavailable; no credential-dependent CI is added.
2. Provider bodies remain verbatim on `TaminApiException`, but credential fields are redacted from logs because the repository secret-handling policy takes precedence over the audit's verbatim-log wording.

## Missing and incomplete response definitions

The source does not define status-code-specific error contracts for any operation. Documented validation messages are preserved as examples on the prescription mutation error response, but their HTTP status codes and `error_Code` values are unknown; no `4XX` response was invented.

Response completeness across the **40 distinct OpenAPI operations**:

| Category | Count | Operations |
| --- | ---: | --- |
| No formal payload schema in the source | 29 | `beginAuthorization`, `signOutUser`, `getPatientEntitlement`, `getPrescriptionDetails`, `deletePrescription`, `updatePrescription`; all 12 `Reference Data` operations; all 3 `Dentistry` operations; `getReferralPrescription`, `listReferralFeedbackPrescriptions`, `listRecentDoctorReferrals`, `listReferralFeedbackQuestions`, `listReferralFeedbackAnswers`, `listUnclaimedNursingPrescriptions`, `saveNursingActions`, `getReferredServicePrescription` |
| Partial/conceptual success definition | 5 | `exchangeAuthorizationCode`, `exchangeOrRefreshDoctorToken`, `createPrescription`, `getOpenReferralCount`, `markPatientDisease` |
| Concrete success example or output table modeled | 6 | `getReferralCartable`, `getReferralFeedbackDetails`, `listPatientOpenReferrals`, `listHospitalizationOrders`, `calculateFamilyDoctorPatientShare`, `listPatientDiseases` |

Additional response gaps:

- OAuth/token error bodies, sign-out behavior, redirect failure parameters, and authorization callback error parameters are absent.
- Entitlement status fields and all reference-data item schemas are absent.
- Prescription reaction, deletion, and edit success/error payloads are absent.
- Dentistry rule result and dentistry service result schemas are absent.
- Most referral list/detail payloads are absent; only three referral response examples are available.
- Nursing and referred-service response payloads are absent.
- `status`, `family`, `reason`, and `traceId` appear in some response examples but the source never defines a universal envelope. They are therefore not imposed on operations lacking such examples.
- Epoch-like `referralDate` numeric values are modeled as integers, but their time unit is not documented.
- Fields shown only as `null` in examples have uncertain non-null scalar types; modeled types should be verified against real provider responses.

## Operation counts and reconciliation

| Measure | Source document | Production specification | Pilot specification |
| --- | ---: | ---: | ---: |
| Logical documented operations | 41 | 41 covered | 41 covered |
| Unique method/path operations | 40 | 40 | 40 |
| OpenAPI Operation Objects | n/a | 40 | 40 |
| Unique non-empty `operationId` values | n/a | 40 | 40 |
| Coverage rows | 41 | 41 mapped | 41 mapped |
| Uncovered documented operations | 0 | 0 | 0 |

The 41-to-40 difference is solely the two logical grant flows sharing `POST /auth/server/v2/token`. No source operation was dropped.

## Validation and lint results

Validation was performed on the final files without generating client code.

| Check | Production | Pilot | Result |
| --- | --- | --- | --- |
| YAML parse and custom integrity checks (operation count, unique IDs, exact path-template parameters, internal references) | Pass | Pass | 40 operations, 40 unique IDs, no missing/extra path parameters, no unresolved internal references |
| `@apidevtools/swagger-cli` 4.0.4 schema validation | Valid | Valid | No structural schema errors |
| Redocly CLI 2.39.0 recommended lint | Valid, 0 errors | Valid, 0 errors | 84 disclosed warnings across both files |
| Redocly CLI 2.39.0 dereferenced bundle | Pass | Pass | All references resolved and both standalone bundles were produced successfully for verification |

The 84 Redocly warnings are intentional and source-driven:

- 80 `operation-4xx-response` warnings: one per OpenAPI operation per environment. The document provides no status-code mapping, so synthetic 4XX contracts were not added.
- 2 `info-license` warnings: the document states ownership/all-rights-reserved but supplies neither a license URL nor an SPDX identifier required by the OpenAPI 3.1 License Object; none was invented.
- 2 `no-ambiguous-paths` warnings: one documented route pair per environment has equal-segment templated overlap. Changing those provider paths would make the specification inaccurate. Production overlap is the dentistry-by-tooth path versus the referred-service path; pilot overlap is prescription detail versus prescription update.

There are no lint errors, invalid examples, duplicate operation IDs, malformed path parameters, or unresolved references. Kiota-oriented choices include stable cross-environment operation IDs, reusable component schemas/parameters/responses, explicit request media types, separate environment documents, no external `$ref` dependencies, and no generated client artifacts.
