# Practical implementation plans

This document converts `docs/guidance.md` into implementation workstreams. The plans are intentionally combined so that each remaining plan can be run at the same time as the others with a distinct owner and without overlapping responsibilities.

## Parallel plan map

| Plan | Owner responsibility | Primary outputs | Must not own |
|---|---|---|---|
| [Plan A — Environment and authentication boundary](#plan-a--environment-and-authentication-boundary) | Environment route selection and PKCE authentication | Route table, auth request builders, auth tests, README auth/environment notes | Prescription business validation, role workflow clients, provider error catalog |
| [Plan B — Provider payload rules and pre-send validation](#plan-b--provider-payload-rules-and-pre-send-validation) | Provider value serialization and request validation | String-code serializer, Jalali date serializer, professional identifier formatter, prescription validators, validation tests | HTTP route selection, auth token flow, public role-client orchestration |
| [Plan C — Role-aware workflow clients](#plan-c--role-aware-workflow-clients) | Public SDK surfaces grouped by provider role and workflow | Doctor, secretary, nurse, and reference-data clients with command/query separation and workflow tests | Serialization rule ownership, validation rule ownership, route table definitions |
| [Plan D — Error normalization and support documentation](#plan-d--error-normalization-and-support-documentation) | Provider error catalog and discrepancy documentation | Normalized errors, discrepancy notes, under-documented field notes, support-oriented README sections, error tests | Request validation rules, environment routing, workflow orchestration |

## Shared interface agreement

Before parallel work starts, the owners must agree on these small contracts and then avoid editing each other's internals:

1. Route consumers ask for a route by `environment` and `operation`.
2. Workflow clients pass provider-bound payloads through serializer and validator abstractions before transport.
3. Validators return structured validation failures and do not send HTTP requests.
4. Error normalization receives the operation name, environment, HTTP status, and raw provider message/body.
5. Role clients expose separate command and query methods; command methods do not double as data-query APIs.

## Plan A — Environment and authentication boundary

### Goal

Create the environment and authentication foundation that every workflow can use without assuming that sandbox is just a production base URL swap.

### Scope

1. Implement operation-level production and sandbox route presets.
2. Preserve provider route differences in domain, path, parameter order, route spelling, and semantics.
3. Add explicit failures for unsupported or undefined environment operations.
4. Implement PKCE authorization URL creation with mandatory practical `state` handling.
5. Implement token exchange, refresh v2, and sign-out request construction.
6. Ensure token and refresh requests use `application/x-www-form-urlencoded`.
7. Add focused tests for route resolution, unsupported routes, PKCE verifier/challenge generation, token form fields, refresh v2 fields, and sign-out routing.
8. Add README notes explaining that environment switching is operation-route based, not a global `baseUrl` replacement.

### Artifacts

| Artifact | Type | Responsibility |
|---|---|---|
| `TaminEnvironmentRoutes` | Configuration | Own operation-level route definitions for production and sandbox. |
| `TaminOperation` | Value definition | Name provider operations used for route lookup and error context. |
| `AuthClient` | Application client | Build and send PKCE authorize, token, refresh v2, and sign-out requests. |
| `PkceChallenge` | Value object | Represent a validated verifier and its derived challenge. |

### Dependency direction

`AuthClient -> TaminEnvironmentRoutes -> TaminHttpTransport abstraction`

`PkceChallenge` must remain independent from HTTP, persistence, UI, and route configuration.

### State ownership and source of truth

- Routes: `TaminEnvironmentRoutes`.
- Operation names: `TaminOperation`.
- PKCE verifier/challenge rules: `PkceChallenge`.
- Token storage: outside this plan unless an explicit token-store port already exists.

### Command/query split

- Query: create authorization URL.
- Commands: token exchange, refresh v2, sign-out.

### Failure handling

- Reject invalid PKCE verifier length or characters before sending.
- Require `state` when creating authorize URLs.
- Fail explicitly when a route is not defined for the selected environment.
- Preserve operation and environment details on auth transport failures.

## Plan B — Provider payload rules and pre-send validation

### Goal

Centralize all provider payload formatting and known business validations so invalid calls are blocked before transport.

### Scope

1. Serialize number-looking provider fields as strings, including `docId`, `docNationalCode`, `patient`, `srvCode`, `srvType`, `siamId`, ICD codes, complaint identifiers, midwife variants, and foreign doctor variants.
2. Serialize provider dates as 8-character Jalali strings for fields such as `prescDate`, `expireDate`, `dateDo`, and `referralHijriDate`.
3. Add professional identifier formatting for midwives and foreign doctors.
4. Validate cross-cutting doctor enrollment fields supplied by the caller: `docId`, `docNationalCode`, and `docMobileNo`.
5. Validate prescription rules for medications, repeats, paraclinical/laboratory orders, Ministry terminology, visits, medical services, nursing, dentistry, physiotherapy, referrals, referral feedback, referred services, hospitalization, and eligibility private-practice identifiers.
6. Support under-documented request fields such as `mobile` and `creatorType` only where provider examples or errors require them.
7. Add serializer and validator tests proving preservation of leading zeros, dashes, alphabetic prefixes/suffixes, Jalali date shape, and each pre-send rule.
8. Add README examples showing strict string serialization and client-side validation failures.

### Artifacts

| Artifact | Type | Responsibility |
|---|---|---|
| `TaminProviderSerializer` | Adapter | Convert SDK request values into provider-safe strings and date shapes. |
| `ProfessionalIdentifierFormatter` | Adapter | Apply midwife and foreign-doctor identifier formatting rules. |
| `PrescriptionValidationRules` | Application validation | Own pre-send prescription rule checks. |
| `EligibilityValidationRules` | Application validation | Own pre-send eligibility identifier checks. |
| `ValidationFailure` | Data transfer shape | Represent a structured client-side validation failure. |

### Dependency direction

`Workflow clients -> validation abstractions -> serializer abstractions`

Validation must not depend on HTTP routes, HTTP clients, generated Kiota builders, UI, or persistence.

### State ownership and source of truth

- String-code serialization rules: `TaminProviderSerializer`.
- Professional identifier transformations: `ProfessionalIdentifierFormatter`.
- Prescription business validations: `PrescriptionValidationRules`.
- Eligibility identifier validation: `EligibilityValidationRules`.

### Command/query split

Validation and serialization are pure query-style operations: they return converted values or failures and do not mutate observable state.

### Failure handling

- Return structured validation failures rather than raw strings.
- Reject ISO dates unless explicitly converted into the provider Jalali format first.
- Reject unsafe numeric coercion for string-code fields.
- Do not silently ignore unknown prescription-type rules.

## Plan C — Role-aware workflow clients

### Goal

Expose the SDK as a business wrapper grouped by user role and provider workflow, not as a flat collection of endpoint calls.

### Scope

1. Add or refine public clients for doctor, secretary, nurse, reference data, prescriptions, dentistry, referral, nursing, hospitalization, and eligibility workflows.
2. Keep role clients thin: they orchestrate request construction, validation, serialization, route lookup, transport, and error normalization but do not own those rules.
3. Separate command and query methods for prescription creation, retrieval, editing, deletion, referral feedback, nursing action recording, hospitalization creation, reference-data lookup, eligibility lookup, and list retrieval.
4. Annotate or structure role-specific access so secretary-only and nurse-only operations are not presented as generic doctor operations.
5. Preserve current implemented surfaces while marking not-yet-implemented groups explicitly until generated request builders or hand-written adapters exist.
6. Add workflow-level tests that verify each public operation calls validation, serialization, route lookup, transport, and error normalization in the correct order.
7. Add README usage examples for medication prescription, paraclinical laboratory prescription, referral, dental rule check, eligibility lookup, nurse to-do list, and hospitalization.

### Artifacts

| Artifact | Type | Responsibility |
|---|---|---|
| `TaminClient` | Application facade | Expose role-aware workflow clients from one SDK entry point. |
| `DoctorClient` | Application client | Expose doctor workflows without owning validation or transport details. |
| `SecretaryClient` | Application client | Expose secretary workflows such as eligibility and hospitalization list retrieval. |
| `NurseClient` | Application client | Expose nurse to-do retrieval and action recording workflows. |
| `ReferenceDataClient` | Query client | Retrieve provider reference data. |
| `PrescriptionClient` | Application client | Orchestrate prescription create, retrieve, edit, delete, and warning-check workflows. |
| `DentistryClient` | Application client | Orchestrate dental rule checks and dental service search. |
| `ReferralClient` | Application client | Orchestrate referral counts, retrieval, feedback, chart, and feedback-detail workflows. |
| `HospitalizationClient` | Application client | Orchestrate hospitalization creation and secretary list retrieval. |
| `EligibilityClient` | Query client | Orchestrate patient eligibility lookup. |

### Dependency direction

`TaminClient -> role clients -> workflow clients -> validator/serializer/route/transport/error abstractions`

Workflow clients must not reach into internals of generated clients or concrete HTTP adapters except through a defined boundary.

### State ownership and source of truth

- Public role composition: `TaminClient`.
- Workflow orchestration: the matching workflow client.
- Business rules, serialization, routes, and error mapping remain owned by Plans A, B, and D.

### Command/query split

- Commands: create, edit, delete, token-affecting calls, referral feedback, nursing action recording, hospitalization creation.
- Queries: reference data, eligibility, retrieval, warning checks if they do not mutate provider state, referral counts/details, nurse to-do retrieval, hospitalization lists.

### Failure handling

- Validate before serializing provider-bound requests.
- Serialize before transport.
- Normalize provider failures before returning them to consumers.
- Surface not-implemented operations explicitly rather than leaving placeholder clients that appear usable.

## Plan D — Error normalization and support documentation

### Goal

Make provider failures and specification inconsistencies understandable, typed, and supportable.

### Scope

1. Build a normalized error catalog for known provider failures: invalid `prescType`/`srvType` pairings, missing laboratory subgroup, null or negative quantities, doctor enrollment/activation problems, doctor national-code/mobile mismatch, empty payloads, missing or malformed patient mobile numbers, invalid patient national codes, unknown `srvCode`, missing or invalid prescription types, duplicates, date format errors, future dates, and invalid `drugAmntId` or `drugInstId`.
2. Preserve raw provider status, message/body, operation name, and environment on normalized errors.
3. Mark errors as client-preventable, support-required, retryable, or provider-contract mismatch where possible.
4. Document provider specification inconsistencies: version `1.9.4` versus `1.9.3`, `id_client` versus `client_id`, string-versus-number ambiguity, `isDentalService` string/numeric ambiguity, and sandbox route spelling/path discrepancies.
5. Document that payloads and routes must be reconciled across tables, examples, business notes, and the error section, not generated from a single table.
6. Add error-normalization tests and documentation-link tests where the project has a documentation test pattern.
7. Add support-oriented README sections explaining how to prevent, normalize, and escalate failures.

### Artifacts

| Artifact | Type | Responsibility |
|---|---|---|
| `TaminErrorNormalizer` | Adapter | Convert provider failures into typed SDK errors. |
| `TaminErrorCategory` | Value definition | Classify normalized errors by remediation path. |
| `TaminProviderError` | Data transfer shape | Preserve raw provider failure details and normalized context. |
| README compatibility section | Documentation | Explain provider version, naming, typing, dental, and route inconsistencies. |

### Dependency direction

`Workflow clients -> TaminErrorNormalizer`

The normalizer can know provider message patterns, but provider business validation remains owned by Plan B.

### State ownership and source of truth

- Error mapping: `TaminErrorNormalizer`.
- Remediation categories: `TaminErrorCategory`.
- Provider discrepancy documentation: README compatibility section.

### Command/query split

Error normalization is pure mapping. It must not retry, mutate requests, refresh tokens, or send HTTP requests.

### Failure handling

- Preserve raw provider details for support.
- Prefer dedicated typed errors for client-preventable failures.
- Avoid swallowing unknown provider messages; wrap them as unknown provider errors with full context.

## Coordination rules for simultaneous execution

1. Each plan owns only the artifacts listed in its section.
2. If a plan needs a type from another plan, it depends on the shared interface agreement rather than editing the other plan's implementation.
3. Tests must stay inside the owning plan's responsibility and use fakes for other plans' boundaries.
4. README edits must be split by section to avoid documentation conflicts:
   - Plan A owns authentication and environment sections.
   - Plan B owns serialization and validation sections.
   - Plan C owns role workflow examples and implementation-status sections.
   - Plan D owns errors, compatibility notes, and support sections.
5. Any new provider rule discovered during implementation must be assigned to exactly one owner before code is changed.

## Trade-offs

- The plans are larger than endpoint-by-endpoint tasks, but they can run in parallel because their state ownership is explicit.
- Validation and serialization are grouped together because both protect provider-bound payload shape and would otherwise cause duplicated rule ownership.
- Error normalization and discrepancy documentation are grouped together because both support diagnosis after provider interaction and provider-spec reconciliation.
- Workflow clients are separated from validation and route definitions so public API work can proceed against stable abstractions.

## Rejected alternatives

1. **One plan per endpoint group** was rejected because route, validation, serializer, and error work would overlap across endpoint owners.
2. **One global implementation plan** was rejected because it cannot be run simultaneously by multiple owners.
3. **Separate test-only plan** was rejected because tests would depend on all implementation plans and could not run independently at the same time.
4. **Single base URL configuration plan** was rejected because the provider specifications require operation-level route differences between production and sandbox.
