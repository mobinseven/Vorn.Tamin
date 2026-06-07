# Guide

This guide **starts primarily from the provider specifications** and treats them as the normative source to be transformed into operational documentation for a real application client. The most important point to document is not just the list of endpoints, but also the **business rules, the differences in behavior between production and sandbox, the constraints per prescription type, the user roles, and the error messages to prevent on the client side**. The provider specifications precisely mix these three levels — transport, business, and operations — so a good guide must bring them together in a single documentation surface. The cover and history pages also show a **version inconsistency**: the cover displays `1.9.4`, while the internal headers display `1.9.3`, all while describing the changes of version `1.9.4`. This must be explicitly flagged in your README to avoid misunderstandings during support and regressions.

The client documentation must also explain that a simple “base URL change” is not enough to switch from production to sandbox. On the contrary, the provider specifications show that several routes change **in domain, path, parameter order, and sometimes semantics** between the two environments, particularly for authentication, eligibility lookup, prescription retrieval, editing, deletion, and certain referral workflows. This demands a **route table per operation and per environment**, not just a global `baseUrl`.

## Recommended client architecture

The best way to document — and to design — a “detail-aware” client is to expose an architecture made of **functional blocks** and **roles**. The provider specifications cover at least authentication, sending prescriptions, patient eligibility, retrieval/editing/deletion, reference data, dentistry, referral workflows, nursing to-do list, referred services, and hospitalization orders. A complete client should therefore document a surface comparable to this:

| Documentation block | What to expose |
|---|---|
| Auth | PKCE URL creation, `code -> token` exchange, refresh, sign-out |
| Prescriptions | creation, retrieval, editing, deletion |
| Reference data | prescription types, service types, services, lab sub-groups, intake usages/times/quantities, plans/conditions, complaints, ICD10, specialties |
| Dentistry | business rule checking, service search with or without tooth |
| Referral | open referral prescription count, retrieval, feedbacks, patient chart, feedback detail |
| Nursing | nurse to-do list retrieval, action recording |
| Hospitalization | creation via `prescTypeId = 9`, list retrieval for secretaries |
| User-friendly wrapper | sandbox presets, local validations, automatic injection of conditional parameters, error normalization |

The client documentation must also enforce very strict **typing rules**. In these provider specifications, many values that look like numbers must be treated as **strings**: `docId`, `docNationalCode`, `patient`, `srvCode`, `srvType`, `siamId`, ICD codes in referrals, some complaint identifiers, and even certain variants related to midwives or foreign doctors. This is essential because several examples contain **leading zeros**, **dashes**, or **alphabetic suffixes/prefixes**; converting these fields to numbers would break the payloads. The same principle applies to dates: they are documented as **8‑character strings in the Persian/Jalali calendar format**, such as `14030717`, not as ISO dates. Your guide should therefore explicitly recommend a Jalali serializer on the client side.

Finally, the documentation should present the client as **role-aware**, not just endpoint-aware. The provider specifications show that some services are intended for the doctor, others for the secretary, and still others for the nurse. In practice, this justifies either separate sub-clients (`doctor`, `secretary`, `nurse`), or at least role annotations in the usage guide. Eligibility services and some referral services can be called by the secretary, the nurse to-do list services by the nurse, and the retrieval of hospitalization orders only by the secretary.

## Authentication and per-environment configuration

The provider specifications document a two‑step **PKCE** authentication. The client guide must clearly explain that `code_verifier` is a random string of **43 to 128 characters**, composed of `A-Z`, `a-z`, `0-9`, `-`, `.`, `_`, `~`, and that `code_challenge` is calculated as `BASE64URL(SHA256(ASCII(code_verifier)))`. It must also explain that the authorization call is a **GET** that returns a **Tamin login HTML page**, followed by a redirect with a `code` and possibly `state` in the return URL. The presence of `state` is not a secondary detail: the provider specifications explicitly recommend it to distinguish the current user or to separate multiple systems/users, and the server returns it as‑is to the callback. In your documentation, `state` must therefore be presented as **mandatory in practice**, even though the provider specifications present it as an “important note” rather than a formally required field.

The guide must then separate **four authentication operations**: `authorize`, `token`, `refresh v2`, and `signout`. Both token calls are documented as `application/x-www-form-urlencoded`. For the initial exchange, the client must send `redirect_uri`, `grant_type=authorization_code`, `client_id`, `code` and `code_verifier`. For the v2 refresh, a distinct endpoint must be handled with `grant_type=refresh_token`, `client_id`, `refresh_token` and `audience`, where `audience` is described as the entity for which the token is issued, for example the doctor’s national code. Your guide must also note that the provider specifications say the v2 flow result includes **both** `access_token` **and** `refresh_token`, and that the latter is reused on the next call.

The most useful part for a **friendly wrapper** consists in documenting the environment differences as built‑in behaviors, and not as notes scattered throughout the provider specifications. An explicit section of the guide should contain something like this:

```yaml
sandbox:
  auth:
    authorizeUrl: "account-pilot.tamin.ir/auth/server/authorize"
    tokenUrl: "account-pilot.tamin.ir/auth/server/token"
    refreshUrl: "account-pilot.tamin.ir/auth/server/v2/token"
    signoutUrl: "account-pilot.tamin.ir/auth/signout"
  defaults:
    refreshClientId: "portal-js"
    testDoctor:
      docId: "2000200092"
      docNationalCode: "1234567891"
      docMobileNo: "09991111111"
    prescriptionClientIdStrategy: "use doctor national code"
    deserveInfoRequestByStrategy: "use doctor national code"
  redirectUri:
    mustNotContainPort: true

production:
  auth:
    authorizeUrl: "account.tamin.ir/auth/server/authorize"
    tokenUrl: "account.tamin.ir/auth/server/token"
    refreshUrl: "account.tamin.ir/auth/server/v2/token"
    signoutUrl: "account.tamin.ir/auth/signout"
  onboarding:
    redirectUriMustBeDeclaredByTicket: true
    redirectUriMustBeStatic: true
    redirectUriMustUseHttps: true
    redirectUriMustUseSsl: true
    redirectUriMustNotUseDynamicParams: true
```

This part deserves to be over-documented, because the provider specifications provide several constraints that must become high‑level client behaviors. In sandbox, the `client_id` value for the refresh flow must be `portal-js`, and the provider specifications also provide a **test doctor triplet** (`docId`, `docNationalCode`, `docMobileNo`) to be used in the test environment. Also in sandbox, prescription creation additionally requires a `clientId` field equal to the **doctor’s national code**, and the eligibility service requires a `requestBy` segment that is, likewise, the doctor’s national code. In production, the return URL must be **declared by ticket**, be **static**, in **HTTPS**, with **SSL**, and **without dynamic parameters**; moreover, in sandbox, this URL **must not contain a port**. Truly useful documentation must surface all these rules at the top level, without forcing users to search for them page by page in the provider specifications.

## Functional coverage of a complete client

A truly complete client must not be limited to “sending a prescription”. The provider specifications actually describe a **medical workflow platform**. Your guide must therefore document all the usage families below, because they are all present in the reference material.

| Domain | What the client must document |
|---|---|
| Core prescription | electronic prescription creation, patient eligibility, retrieval, deletion, editing |
| Reference data | prescription types, line types, service catalogs, lab sub-types, quantities/rhythms/intake times, treatment plans, diseases, complaints, ICD10, specialties |
| Dentistry | reimbursement and compatibility rule checking, service search without tooth or by tooth |
| Referral | open referral prescription count, reading a referral, reading feedbacks, list of referrals from the last 3 days, patient chart, feedback detail |
| Nursing | clinic nurse to-do list, recording actions on prescription lines |
| Referred services | retrieval of a referred-service prescription for the second doctor |
| Hospitalization | creation of a hospitalization order in the prescription flow, retrieval of the hospitalization order list for the secretary |

The guide must give special attention to the **retrieval / editing / deletion** workflow, because the provider specifications add functional rules that cannot be inferred from the routes alone. Prescription retrieval returns the detail based on the `headerID` and the doctor’s identity; in sandbox, the route also requires `docNationalCode`. Deletion is only possible **until the end of the day of creation**. Editing is only allowed **as long as no action has been carried out on the prescription** by the pharmacy or paraclinical centers, each prescription can be **edited only once**, and prescriptions of type **visit** and **medical services** are not editable. Moreover, the provider specifications specify that retrieval and editing are linked to the **same `clientId` used at creation**, which implies that the client documentation must explain the persistence of this context, not just the method signatures.

The referral workflow must also be documented as a **business sequence** and not as a series of isolated routes. The provider specifications describe a sequence where you can count open referrals, retrieve a referral via a `trackingCode`, retrieve the associated feedbacks, and then request the detail of a feedback with the `id` / `masterParent` pair. Crucially, the provider specifications say that at the feedback level, an element with `id != 0` represents one of the prescriptions created by the second doctor, whereas an element with `id == 0` represents the **referral feedback itself**. This is a key nuance to document, because a friendly client should ideally expose a **discriminated type** or at least two distinct helpers.

## Business validations to implement before the API call

The value of a well-documented client lies above all in its **upfront validations**. The provider specifications contain enough business logic to avoid a large number of invalid calls if it is encoded on the client side. The rules below must appear explicitly in the guide, as **pre‑send** constraints rather than as mere server errors to be handled after the fact. They are drawn from the sections dedicated to prescription creation and from the “important notes” at the end of the provider specifications.

| Case | What the client must validate/document |
|---|---|
| Cross‑cutting rules | `docId`, `docNationalCode` and `docMobileNo` must exactly match the information registered at the time of the doctor’s enrollment |
| Dates | `prescDate`, `expireDate`, `dateDo`, `referralHijriDate` are 8‑character Jalali strings; `prescDate` cannot be in the future |
| Medication prescription | `prescTypeId = 1`; each line must have `srvType = "01"`; `timesAday` and `drugInstruction` are required |
| Prescription repeat | if `repeat` is sent, `dateDo` must also be sent; the maximum number of repeats is `3` |
| Paraclinical prescription | `prescTypeId = 2`; only certain `srvType` are allowed; if `srvType = "02"` (laboratory), `parGrpCode` becomes required |
| Ministry terminology | if `terminology = "thritha.BEHDASHT"`, `srvCode` must follow the coding of the Ministry of Health |
| Visit | `prescTypeId = 3` implies that `noteDetailEprscs` must be empty |
| Medical services | `prescTypeId = 5` implies services of type `17`; for nursing, at least one medical service and one nursing service are required, plus `siamId` |
| Dentistry | `isDentalService` is required; if the treatment depends on a tooth, `toothId` must be sent; for radiography, the rule check may require `allGridData` |
| Physiotherapy | `illnessId` and `planId` are optional, but multiple values can be sent separated by commas; all services in the prescription must share the same disease type and the same quantity |
| Doctor referral | `prescTypeId = 7` requires `noteDetailsReferralList`; `referralHijriDate` must be between D+1 and 3 months; max `quantity` = `3` |
| Referral feedback | `prescTypeId = 8` requires `comments` and `referralFeedbackId` |
| Referred services | `prescTypeId = 6`; all items must be medical services; the second doctor must reuse `referenceStatus` and `noteDetailsEprscId` obtained from the retrieval |
| Hospitalization | `prescTypeId = 9`; `siamId` must be provided for the concerned center; the hospitalization date is carried via `referralHijriDate` in the referral structure; the diagnosis must be sent as in the example |
| Eligibility | if the doctor works from their private practice, they must send their `docId` instead of `siamId` |

Two other points deserve to be documented very prominently. First, the provider specifications describe specific formats for certain professionals: for **midwives**, if the order number contains the Persian letter `م`, it must be sent as `*` as a suffix; for **foreign doctors**, the **FIDA code** must be sent in place of the national code, preceded by `FDA`, and the suffix `ات` must be added to the `docId` field. Second, several fields seem under‑documented in the main table but appear in the examples or in errors, in particular `mobile` and `creatorType`. The client documentation must therefore present them as **supported fields and potentially necessary in practice**, even if the provider specifications do not dedicate as clear a structured definition to them as for the other parameters.

## Error handling and document inconsistencies

The last section of the provider specifications must be transformed into a **normalized client‑side error catalog**. The server notably reports bad pairings between `prescType` and `srvType`, missing laboratory sub‑group, null or negative quantities, doctor not enrolled/not activated, mismatch between doctor’s national code and mobile, empty payloads, missing or malformed patient mobile numbers, invalid patient national codes, unknown `srvCode`, missing or invalid prescription types, duplicate prescriptions, incorrect date formats, future dates, and invalid values for `drugAmntId` or `drugInstId`. A good guide must not just “list the errors”: it must explain how to **prevent** them, how to **normalize** them in the SDK, and which errors deserve a dedicated type rather than a simple raw message.

The provider specifications also contain several **internal inconsistencies** that a serious guide must honestly document. There is the version inconsistency `1.9.4` / `1.9.3`. There are also naming discrepancies between `id_client` and `client_id`, data structures that suggest numeric identifiers while the explanatory text on the contrary requires **string codes** for certain diagnoses and complaints in the referral workflow, a field `isDentalService` described as a `0/1` string but shown as numeric in an example, and sandbox routes that differ strongly from production routes, including variable spellings in some segments (`docNationalCode`, `docNatioanlCode`, etc.). The best practice to document is therefore the following: **do not derive payloads or routes from a single tabular source**; systematically cross‑reference the tables, business notes, JSON examples and the error section. It is precisely this reconciliation that will make the difference between a “generic” client and a client truly usable in both production and sandbox.

The practical conclusion to write in your README is simple: the client must be presented as a **business wrapper** on top of HTTP transports, with **environment presets**, **pre‑send validators**, **strict string serialization of codes**, **role support**, and **normalization of the provider specifications' discrepancies**. In other words, good documentation is not only “how to call the API”, but also “how to avoid the tricky cases that the provider specifications scatter over 46 pages”.