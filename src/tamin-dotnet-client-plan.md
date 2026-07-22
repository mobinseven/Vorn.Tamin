# Tamin .NET Client Implementation Plan

Source of truth: `tamin-openapi-audit.md` (production and pilot OpenAPI 3.1 specs derived from
`EP-TAMIN-API(1).md` v1.9.7). This plan turns that audit into an implementation, addressing the
five structural risks it surfaces: multi-host operations, unverified auth header format,
untyped/absent response schemas, undocumented error contracts, and a hand-maintained (not
vendor-published) spec that will need re-generation as ambiguities get resolved.

---

## 1. Client boundary: split by host, not by environment

Kiota binds one base URL per generated client. The spec expresses environment via per-operation
`servers` overrides across three production hosts (`account.tamin.ir`, `soa.tamin.ir`,
`api.tamin.ir`) and their pilot equivalents — Kiota will not reliably follow per-operation server
overrides at generation time.

**Decision:** split the two source OpenAPI documents into three host-scoped documents *each*,
generate six clients, pair them at runtime by environment config:

| Client project | Prod host | Pilot host | Covers |
| --- | --- | --- | --- |
| `Tamin.Client.Account` | `account.tamin.ir` | `account-pilot.tamin.ir` | authorize, token exchange/refresh, sign-out |
| `Tamin.Client.Soa` | `soa.tamin.ir` | `ep-test.tamin.ir` | prescriptions, all reference data, dentistry, referrals, nursing |
| `Tamin.Client.Api` | `api.tamin.ir` | `ep-test.tamin.ir` | referral-feedback lists, family-doctor share, patient disease ops |

Environment (prod/pilot) is a `BaseUrl` config value per client, never a separate generated
client type.

## 2. Auth: hand-rolled token provider, not Kiota's default

The doc never asserts an `Authorization` header format or scopes, and the token itself comes from
one of the generated operations (`exchangeOrRefreshDoctorToken`, discriminated by grant type).
Kiota's default bearer provider assumes a token already exists — insufficient here.

**Decision:** implement `TaminTokenProvider : IAccessTokenProvider` that:
- owns PKCE `code_verifier` generation (43–128 chars, `[A-Za-z0-9._~-]` only — per D-04, do not
  reproduce the invalid doc examples containing `:` or `\`),
- discriminates `authorization_code` vs `refresh_token` grants against the single
  `exchangeOrRefreshDoctorToken` operation,
- caches and proactively refreshes without assuming an expiry contract beyond what's documented,
- is swappable behind the interface in case pilot testing shows the real header scheme differs
  from the OAuth2 authorization-code assumption in the spec.

Wire it into `Tamin.Client.Soa` and `Tamin.Client.Api`'s `BaseBearerTokenAuthenticationProvider`.
`Tamin.Client.Account` itself needs no bearer auth for the token/authorize endpoints.

## 3. Response mapping layer — required for the 29 schema-less operations

29 of 40 operations have no response schema (modeled as unconstrained `JsonObject`). Application
code must never consume that type directly.

**Decision:** one mapping-service interface per functional area, sitting between generated clients
and callers, e.g.:

```csharp
public interface IPrescriptionReferenceDataService { /* typed reads, JsonObject mapped internally */ }
public interface IPrescriptionMutationService { /* create/edit/delete, typed */ }
public interface IReferralService { /* cartable, feedback, counts */ }
public interface INursingService { }
public interface IPatientDiseaseService { }
```

Every mapping method must carry an inline comment citing the relevant audit row/D-## note it
encodes (e.g. `// D-08: allGridData modeled as array per prose, not table type`). This makes the
audit's disclosed assumptions traceable in code instead of silently baked in.

## 4. Error handling — assume nothing, preserve everything

80 of the 80 possible 4XX slots (2/operation across environments) are undocumented. Do not let
Kiota's generated `ApiException` swallow the body on non-2xx.

**Decision:**
- Custom `IResponseHandler` (or `DelegatingHandler` in the `HttpClient` pipeline) captures raw
  status + body on any non-2xx before deserialization is attempted.
- Wrap into `TaminApiException(int? statusCode, string operationId, string rawBody)`.
- Log every raw body keyed by `operationId` — this is the dataset that eventually lets you correct
  the spec instead of guessing.
- Add Polly retry/circuit-breaker at the `HttpClientHandler` level (transient 5xx/timeout only),
  not per-operation, since failure modes aren't documented.

## 5. Regeneration workflow

These specs are hand-maintained, not vendor-published, and will change as D-01…D-20 ambiguities
get resolved against real provider behavior.

**Decision:**
- Check the six host-scoped OpenAPI documents into the monorepo alongside a `kiota-lock.json` per
  client project.
- Add an MSBuild target or script step (`scripts/generate-tamin-clients.ps1`/`.sh`) wrapping
  `kiota generate` per client — not a manual, undocumented step.
- Any spec edit that resolves a D-## note must be paired with a mapping-layer change in the same
  commit, so regeneration + compiler errors surface exactly what broke.

## 6. Suggested monorepo layout

```
/src/Tamin/
  Tamin.Client.Account/        (generated, Kiota)
  Tamin.Client.Soa/            (generated, Kiota)
  Tamin.Client.Api/            (generated, Kiota)
  Tamin.Integration/           (hand-written: token provider, response handler, mapping services, DTOs)
  Tamin.Integration.Tests/     (contract tests against pilot host)
/openapi/tamin/
  account.prod.yaml  account.pilot.yaml
  soa.prod.yaml      soa.pilot.yaml
  api.prod.yaml      api.pilot.yaml
/scripts/
  generate-tamin-clients.ps1
```

## 7. Implementation phases

1. **Split** the two source documents into six host-scoped documents; re-run the existing
   validation suite (swagger-cli, Redocly lint + dereference) per file.
2. **Generate** six Kiota clients; confirm operation counts match the audit's per-host subset.
3. **Auth**: implement `TaminTokenProvider`, wire into Account client for the exchange calls and
   into Soa/Api clients as the bearer provider.
4. **Error pipeline**: response handler + `TaminApiException` + Polly policy, applied to all three
   host clients uniformly.
5. **Mapping layer**: implement one service interface per functional area (§3), starting with the
   6 "Complete success" operations (fully modeled, lowest risk) before the 29 schema-less ones.
6. **Regeneration tooling**: script + lock files + CI step that fails the build if a spec edit
   wasn't paired with a mapping-layer update (simple heuristic: diff touches `/openapi/` without
   touching `Tamin.Integration/`).
7. **Contract tests**: for each "Complete success"/"Concrete success example" operation, assert
   the mapped DTO against the documented example payload from the audit.
