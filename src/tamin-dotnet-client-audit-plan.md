# Tamin .NET Client Implementation Audit Plan

Purpose: verify the implementation produced from `tamin-dotnet-client-plan.md` faithfully
executes it, introduces no undocumented behavior beyond what `tamin-openapi-audit.md` discloses,
and is safe to finalize. This is a verification pass against two upstream documents, not a fresh
design review — every finding must trace back to a specific plan section, audit row, or D-## note.

Three source-of-truth documents for this audit:
- `tamin-openapi-audit.md` — the original API contract audit (41 logical ops, 40 unique ops,
  D-01…D-20 disclosed ambiguities).
- `tamin-dotnet-client-plan.md` — the implementation plan (§1–§7) and execution prompt.
- The six host-scoped OpenAPI documents and generated code actually produced.

---

## 1. Structural conformance (does it match the plan's shape?)

| Check | Method | Pass condition |
| --- | --- | --- |
| Six host-scoped documents exist | Diff each against the corresponding slice of the two original specs by operationId | Every operationId from the original prod/pilot docs appears in exactly one host-scoped doc; none dropped, none duplicated across docs |
| Client project layout matches §6 | Inspect solution structure | `Tamin.Client.{Account,Soa,Api}` generated-only; `Tamin.Integration` hand-written; no generated code committed outside the three client projects |
| Base URLs correctly paired | Inspect DI registration / config | Account→`account.tamin.ir`/`account-pilot.tamin.ir`, Soa→`soa.tamin.ir`/`ep-test.tamin.ir`, Api→`api.tamin.ir`/`ep-test.tamin.ir`; environment switch is config-only, not a second client type |
| No route/casing drift | Run the same custom integrity check used in the original audit (operation count, unique IDs, exact path-template parameters) against the six new docs | 40 operations total across the split, same as original; path templates byte-identical to audit rows, including intentionally preserved oddities |
| Intentional "errors" preserved verbatim | Grep generated route templates for: `docNatioanlCode` (D-15), `/ep/api/v7/cartable-nurse/save` (D-16), `referentalPrescDetail` (D-17), inconsistent `siamId`/`siam-id`/`siamid` and `docId`/`doc-id`/`docID` casing (D-18) | All four present exactly as documented; none "fixed" by the implementation |

**Flag as finding, not silent fix:** any place code was written as if a D-## ambiguity had been
resolved without a corresponding note in this audit or a spec update.

## 2. Auth provider verification (plan §2)

| Check | Method | Pass condition |
| --- | --- | --- |
| PKCE `code_verifier` constraint | Unit test generating N verifiers | 100% match `^[A-Za-z0-9._~-]{43,128}$`; no `:` or `\` (D-04 violation check) |
| No invalid doc examples reproduced | Code review of test fixtures/comments | D-02 (malformed `code_challenge`) and D-03 (invalid JSON escapes in `access_token`) examples not present as literal fixtures |
| Grant discrimination | Unit test both `authorization_code` and `refresh_token` paths against `exchangeOrRefreshDoctorToken` | Correct discriminator sent per grant; single operation, two logical flows (audit rows 4–5) both exercised |
| Token caching/refresh | Integration test against pilot | Refresh triggers before expiry without relying on an undocumented expiry contract; provider degrades safely (re-auth, not crash) if server returns unexpected shape |
| Provider is swappable | Code review | `IAccessTokenProvider` interface boundary intact; no direct dependency on a specific header scheme baked into Soa/Api clients |

## 3. Error pipeline verification (plan §4)

| Check | Method | Pass condition |
| --- | --- | --- |
| Non-2xx capture | Fault-inject (mock handler returning 4xx/5xx with arbitrary bodies) | Raw status + raw body captured into `TaminApiException` before any deserialization attempt; nothing swallowed |
| Logging keyed correctly | Inspect log output during fault injection | Every non-2xx logged with `operationId`; body preserved verbatim (this is the audit-correction dataset — verify it's actually usable, not truncated or redacted) |
| No invented error schema | Code review | No `4XX` response type, `error_Code` enum, or status-specific DTO was added anywhere — matches audit's explicit statement that no such contract exists |
| Retry/circuit-breaker scope | Code review of Polly policy registration | Applied at `HttpClientHandler` level (transient 5xx/timeout only), not per-operation; does not retry on documented validation-style 4xx |

## 4. Mapping layer verification against D-## notes (plan §3, §7 step 5)

This is the highest-risk section — verify every disclosed ambiguity was encoded as a *traceable*
decision, not silently resolved one way in code with no comment trail.

| D-## / audit note | Required mapping-layer behavior | Verify |
| --- | --- | --- |
| D-05 | Wire names `noteDetailEprscs`, `noteDetailsReferralList`; `noteDetailExprsc` treated as typo, not a second property | Grep DTOs for the typo spelling — must not exist as a real property |
| D-06 | `mobile` required on prescription creation | Field present and marked required in DTO/validation |
| D-07 | `isDentalService` modeled as string enum `'0'|'1'`, not numeric | DTO type is string enum; mapping from numeric example (if ever received) doesn't throw uncontrolled |
| D-08 | `allGridData` is `GridData[]` | DTO field is array-typed, not singular |
| D-09 | Both `NoteDetailsReferral.id` and `referralHijriDate` retained as optional, no precedence invented | Both fields present; no code path assumes one implies the other |
| D-10 | Both `complaintIDs` and `complaints[].id` retained | Both present in `ReferralComplaint`, neither required |
| D-11 | Separate `DiagnosisID` (int) and `ReferralIcd10` (string) schemas | No shared/merged type; both exist distinctly |
| D-12 | `patientID` in referral cartable kept as string, semantic label ("doctor national code") not trusted | No renaming or type change based on the suspect label |
| D-13 | v7 route used for referral-feedback details, v2 examples not added as fallback | Only one route wired; no dual-path fallback logic |
| D-15 | Pilot referred-service client requires the misspelled path parameter | Path parameter present and required in generated + wrapper signature |
| D-19/D-20 | Open-referral-count and mark-patient-disease responses left unconstrained/optional-heavy | DTOs don't over-specify beyond documented fields |
| §3 general rule | Every mapping method touching an ambiguity has an inline `// D-##` comment | Grep `Tamin.Integration` for `D-0` / `D-1` / `D-2` comment references; cross-check count against the 20 notes — any missing should be explained (not every note requires code, but absence should be a decision, not an oversight) |

## 5. Response-completeness fidelity (plan §7 step 5, audit's coverage table)

| Category (from audit) | Count | Verify |
| --- | ---: | --- |
| No formal payload schema | 29 | Mapping returns a deliberately loose/optional-heavy DTO or passes through `JsonObject`-equivalent; nothing invents a strict shape |
| Partial/conceptual success | 5 | Mapped fields match exactly what the audit table/prose documents — no extra fields guessed in |
| Concrete success example modeled | 6 | Contract tests exist and pass for all 6 (getReferralCartable, getReferralFeedbackDetails, listPatientOpenReferrals, listHospitalizationOrders, calculateFamilyDoctorPatientShare, listPatientDiseases) |

Contract test check: assert each of the 6 fully-modeled operations' mapped DTO against the
documented example payload from the audit, field-for-field, including the `docFamily` field
called out for `listHospitalizationOrders` and both success variants for
`calculateFamilyDoctorPatientShare`.

## 6. Regeneration tooling verification (plan §5)

| Check | Method | Pass condition |
| --- | --- | --- |
| Generation is scripted, not manual | Run `scripts/generate-tamin-clients.*` from clean checkout | Regenerates all six clients deterministically; diff against committed generated code is empty |
| Lock files committed | Inspect repo | One `kiota-lock.json` per client project, in sync with the source OpenAPI doc hashes |
| Spec-edit/mapping-layer pairing enforced | Inspect CI config | A check exists (or is documented as manual review policy) that flags commits touching `/openapi/` without touching `Tamin.Integration/` |

## 7. Validation and lint parity with the original audit

Re-run the same tooling the original audit used, against all six new documents:

- `@apidevtools/swagger-cli` schema validation — expect valid, 0 structural errors.
- Redocly CLI recommended lint — expect the same *category* of warnings as before
  (`operation-4xx-response`, `info-license`, `no-ambiguous-paths`), scaled to the new
  document count; any new warning category is a regression to explain, not ignore.
- Redocly dereferenced bundle build — must succeed for all six documents.
- Custom integrity check (operation count, unique operationIds, path-parameter match,
  no unresolved internal refs) — must pass per document and in aggregate (40 operations total).

## 8. Finalization gate

Do not mark the implementation final until:

1. All checks in §1–§7 pass or have an explicit, written exception tied to a specific D-## note
   or plan section (no silent deviations).
2. Every one of the 6 "Complete success" / "Concrete success example" operations has a passing
   contract test against the documented payload.
3. Fault-injection tests confirm the error pipeline captures raw bodies for at least one
   synthetic 4xx and one 5xx per host client (Account/Soa/Api).
4. A finding log exists listing: which D-## notes required a code-level judgment call, what was
   decided, and why — mirroring the audit's own "Modeling assumptions and disclosed resolutions"
   format, so the implementation is auditable the same way the spec was.
