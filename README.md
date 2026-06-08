# Vorn.Tamin

[![NuGet Version](https://img.shields.io/nuget/v/vorn.tamin?style=flat-square)](https://www.nuget.org/packages/Vorn.Tamin)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)

A .NET 10 client SDK for selected EP.Tamin electronic prescription API endpoints of the Social Security Insurance of Iran (سازمان تأمین اجتماعی).

> **Implementation status:** the new version is Kiota-backed and currently exposes only the endpoint groups listed as **Implemented** below. Older README examples for identity, pharmacy dispensing, paraclinic delivery, pricing, and some response DTOs described planned or previous surfaces and are now explicitly marked **Not implemented** until matching generated request builders are available.

---

## Table of contents

1. [Implementation status](#implementation-status)
2. [Installation](#installation)
3. [Prerequisites](#prerequisites)
4. [Quick-start](#quick-start)
5. [Choosing a client surface](#choosing-a-client-surface)
6. [Role-aware workflows](#role-aware-workflows)
7. [Dependency injection setup](#dependency-injection-setup)
8. [Authentication](#authentication)
9. [Reference data](#reference-data)
10. [Prescription operations](#prescription-operations)
11. [Parsing API responses](#parsing-api-responses)
12. [Error handling](#error-handling)
13. [Artifact inventory](#artifact-inventory)
14. [Limitations & compatibility notes](#limitations--compatibility-notes)
15. [Contributing](#contributing)
16. [Changelog](#changelog)
17. [License](#license)

---

## Implementation status

| Area | Status | Public surface |
|---|---|---|
| Session construction with a pre-obtained token | **Implemented** | `new TaminSession(...)` |
| Runtime login | **Implemented** | `TaminSession.CreateAsync(...)` |
| Token refresh | **Implemented** | `TaminSession.RefreshTokenAsync(...)`, `AuthClient.RefreshTokenV2Async(...)` |
| PKCE authorization URL and token exchange | **Implemented** | `AuthClient`, `PkceChallenge` |
| Operation-level environment routes | **Implemented** | `TaminEnvironmentRoutes`, `TaminOperation` |
| Token validation | **Implemented** | `TaminSession.ValidateTokenAsync(...)` |
| Production / sandbox endpoint selection | **Implemented** | `TaminEndpoint.Production`, `TaminEndpoint.Sandbox` |
| Dependency injection registration | **Implemented** | `AddTaminClient(...)`, `TaminOptions` |
| Role-aware facade | **Implemented** | `new TaminClient(session)`, `session.Doctor`, `session.Secretary`, `session.Nurse` |
| Reference data: services | **Implemented** | `session.ReferenceData.GetServiceListAsync(...)`, `session.Service.GetServiceListAsync(...)`, `GetAllServicesAsync(...)` |
| Reference data: prescription types | **Implemented** | `session.Service.GetPrescriptionTypeAsync(...)` |
| Reference data: paraclinic tariffs | **Implemented** | `session.Service.GetParaclinicTarefAsync(...)` |
| Reference data: drug amounts/list | **Implemented** | `session.Service.GetDrugListAsync(...)`, `GetDrugAmountAsync(...)` |
| Reference data: drug instructions | **Implemented** | `session.Service.GetDrugInstructionAsync(...)` |
| Prescription writing | **Implemented** | `RegisterVisitPrescriptionAsync`, `RegisterDrugPrescriptionAsync`, `RegisterParaclinicPrescriptionAsync`, `RegisterMedicalServicePrescriptionAsync`, `RegisterReferralPrescriptionAsync`, `RegisterPhysiotherapyPrescriptionAsync` |
| Prescription lookup | **Implemented** | `session.Prescription.GetRegisteredPrescriptionAsync(...)` |
| Prescription edit/delete | **Implemented** | `EditElectronicPrescriptionAsync(...)`, `DeleteElectronicPrescriptionAsync(...)` |
| Prescription warning check | **Implemented** | `session.Dentistry.CheckRulesAsync(...)`, `CheckPrescriptionWarningAsync(...)` |
| Referral workflow | **Partially implemented** | `session.Doctor.Referrals.RegisterPrescriptionAsync(...)`; feedback/count/detail operations fail with `TaminWorkflowNotImplementedException`. |
| Eligibility lookup | **Implemented** | `session.Secretary.Eligibility.LookupPrivatePracticeAsync(...)` |
| Nurse workflow | **Explicitly not implemented** | `session.Nurse` methods fail with `TaminWorkflowNotImplementedException`. |
| Hospitalization workflow | **Explicitly not implemented** | `session.Secretary.Hospitalization` methods fail with `TaminWorkflowNotImplementedException`. |
| Identity verification | **Not implemented** | `session.Identity` exists only as an empty placeholder. |
| Pharmacy dispensing | **Not implemented** | `session.Pharmacy` exists only as an empty placeholder. |
| Paraclinic service delivery | **Not implemented** | `session.Paraclinic` exists only as an empty placeholder. |
| Allowed-count and pricing helpers | **Not implemented** | No public methods are exposed in this version. |
| Strong typed response DTOs for reference, identity, pharmacy, and paraclinic flows | **Not implemented** | Methods currently return `JsonElement`. |

---

## Installation

### dotnet CLI

```bash
dotnet add package Vorn.Tamin
```

### Package Manager Console

```powershell
Install-Package Vorn.Tamin
```

### PackageReference

```xml
<PackageReference Include="Vorn.Tamin" Version="*" />
```

---

## Prerequisites

- .NET 10 SDK.
- EP.Tamin credentials or a pre-obtained bearer token.
- A `Client-Id` value when your EP.Tamin onboarding requires that header.

---

## Quick-start

```csharp
using System.Text.Json;
using Vorn.Tamin;

using var httpClient = new HttpClient();

// Option A: pre-obtained bearer token.
TaminSession session = new(
    httpClient,
    oauthToken: "YOUR_TOKEN",
    clientId: "YOUR_CLIENT_ID",
    endpoint: TaminEndpoint.Production);

// Option B: log in with username / password (+ optional OTP/provider identifier).
// TaminSession session = await TaminSession.CreateAsync(
//     httpClient,
//     username: "your-username",
//     password: "your-password",
//     otp: "123456",
//     providerIdentifier: "prov-id",
//     clientId: "YOUR_CLIENT_ID",
//     endpoint: TaminEndpoint.Sandbox);

// Reference data.
JsonElement drugs = await session.Service.GetDrugListAsync(
    searchText: "amoxicillin",
    activeOnly: true);

// Register a drug prescription.
JsonElement result = await session.Prescription.RegisterDrugPrescriptionAsync(
    new RegisterDrugPrescriptionRequest
    {
        DoctorId = "doctor-id",
        PatientNationalId = "1234567890",
        VisitDate = "14030601",
        DrugItems =
        [
            new DrugItem
            {
                DrugCode = "DR001",
                Quantity = 2,
                DosageInstruction = "twice daily"
            }
        ]
    });
```

> **Not implemented:** identity verification, pharmacy dispensing, paraclinic delivery, nurse action submission, and hospitalization submission are not available in this version. Unavailable role workflows fail with `TaminWorkflowNotImplementedException` instead of appearing usable.

---

## Choosing a client surface

Use one session as the source of truth for endpoint selection, token state, `Client-Id`, route resolution, and the generated Kiota gateway. Build other surfaces from that session instead of constructing parallel clients with their own `HttpClient` or environment settings.

| Surface | Use when | Notes |
|---|---|---|
| `TaminSession` | You want direct access to every currently exposed workflow client. | Owns `BaseUri`, `ClientId`, endpoint selection, generated gateway construction, and backward-compatible properties such as `session.Service` and `session.Prescription`. |
| `TaminClient` | You want role-oriented grouping for application code. | Wraps an existing `TaminSession`; it does not own separate provider state or transport. |
| `AddTaminClient(...)` | You use ASP.NET Core or Microsoft dependency injection. | Registers a single configured `TaminSession` through `IHttpClientFactory`; prefer this for managed `HttpClient` lifetime. |
| Generated Kiota projects | You are maintaining this SDK, not consuming it. | Treat generated request builders as infrastructure details behind the public workflow clients. |

Commands and queries are intentionally exposed as separate methods: registration, edit, delete, token refresh, sign-out, referral feedback, nurse action recording, and hospitalization creation are commands; reference-data lookup, eligibility lookup, prescription retrieval, warning checks, referral counts/details, nurse to-do retrieval, and hospitalization lists are queries.

---

## Role-aware workflows

`TaminClient` is a role-aware facade over `TaminSession`. Existing session clients remain available for compatibility, while new code can group operations by provider role.

```csharp
var tamin = new TaminClient(session);

// Doctor workflow: medication prescription.
JsonElement medication = await tamin.Doctor.Prescriptions.RegisterDrugPrescriptionAsync(
    new RegisterDrugPrescriptionRequest
    {
        DoctorId = "doctor-id",
        PatientNationalId = "1234567890",
        VisitDate = "14030601",
        DrugItems = [new DrugItem { DrugCode = "DR001", Quantity = 1 }]
    });

// Doctor workflow: paraclinical laboratory prescription.
JsonElement laboratory = await tamin.Doctor.Prescriptions.RegisterParaclinicPrescriptionAsync(
    new RegisterParaclinicPrescriptionRequest
    {
        DoctorId = "doctor-id",
        PatientNationalId = "1234567890",
        VisitDate = "14030601",
        ServiceItems = [new ServiceItem { ServiceCode = "LAB001", ServiceGroup = "laboratory", Quantity = 1 }]
    });

// Doctor workflow: referral prescription.
JsonElement referral = await tamin.Doctor.Referrals.RegisterPrescriptionAsync(
    new RegisterReferralPrescriptionRequest
    {
        DoctorId = "doctor-id",
        PatientNationalId = "1234567890",
        TargetSpecialty = "cardiology",
        TargetProviderType = "specialist",
        Reason = "consultation",
        VisitDate = "14030601"
    });

// Doctor workflow: dental rule check.
JsonElement dentalWarnings = await tamin.Doctor.Dentistry.CheckRulesAsync(
    new CheckWarningRequest
    {
        DoctorId = "doctor-id",
        PatientNationalId = "1234567890",
        PrescriptionItems = [new { serviceCode = "DENT001", toothId = "11" }]
    });

// Secretary workflow: eligibility lookup.
JsonElement eligibility = await tamin.Secretary.Eligibility.LookupPrivatePracticeAsync(
    new EligibilityLookupRequest
    {
        SiamId = "clinic-siam-id",
        DoctorId = "doctor-id",
        DoctorNationalCode = "0012345678",
        PatientNationalCode = "1234567890"
    });

// Nurse and hospitalization workflows are intentionally explicit until their generated adapters are wired.
try
{
    await tamin.Nurse.GetTodoListAsync(new NurseTodoListRequest
    {
        SiamId = "clinic-siam-id",
        PatientNationalCode = "1234567890"
    });
}
catch (TaminWorkflowNotImplementedException ex)
{
    Console.WriteLine(ex.WorkflowName);
}

try
{
    await tamin.Secretary.Hospitalization.GetSecretaryListAsync(new HospitalizationSecretaryListRequest
    {
        SiamId = "clinic-siam-id",
        SecretaryNationalCode = "0012345678"
    });
}
catch (TaminWorkflowNotImplementedException ex)
{
    Console.WriteLine(ex.WorkflowName);
}
```


---

## Dependency injection setup

Register the SDK in `Program.cs` or `Startup.cs`:

```csharp
using Vorn.Tamin;
using Vorn.Tamin.Extensions;

builder.Services.AddTaminClient(o =>
{
    o.Endpoint = TaminEndpoint.Sandbox;
    o.BaseUrl = "https://ep-test.tamin.ir/api/"; // optional; defaults from Endpoint when blank
    o.ClientId = "YOUR_CLIENT_ID";
    o.OAuthToken = "YOUR_TOKEN"; // optional; omit when a token is acquired elsewhere
});
```

`TaminSession` is registered as a scoped service backed by `IHttpClientFactory`:

```csharp
public sealed class PrescriptionIssuer(TaminSession tamin)
{
    public Task<JsonElement> IssueAsync(string doctorId, string patientId, string visitDate)
        => tamin.Prescription.RegisterDrugPrescriptionAsync(
            new RegisterDrugPrescriptionRequest
            {
                DoctorId = doctorId,
                PatientNationalId = patientId,
                VisitDate = visitDate,
                DrugItems = [new DrugItem { DrugCode = "DR001", Quantity = 1 }]
            });
}
```

### `appsettings.json` binding

```json
{
  "Tamin": {
    "Endpoint": "Sandbox",
    "BaseUrl": "https://ep-test.tamin.ir/api/",
    "ClientId": "YOUR_CLIENT_ID",
    "OAuthToken": "YOUR_TOKEN"
  }
}
```

```csharp
builder.Services.AddTaminClient(
    o => builder.Configuration.GetSection("Tamin").Bind(o));
```

---

## Authentication

### Authenticate with a pre-obtained token

```csharp
var session = new TaminSession(
    new HttpClient(),
    oauthToken: "YOUR_TOKEN",
    clientId: "YOUR_CLIENT_ID");
```

### Log in at runtime

`TaminSession.CreateAsync` posts credentials to the configured endpoint and stores the returned token for the generated Kiota gateway:

```csharp
var session = await TaminSession.CreateAsync(
    new HttpClient(),
    username: "user",
    password: "pass",
    otp: "123456",
    providerIdentifier: "provider-id");
```

### PKCE authorization URL and token exchange

`AuthClient` owns the practical OAuth/PKCE boundary. The caller must generate and persist both the PKCE verifier and the `state` value before redirecting the user; the SDK intentionally does not store either value. Token and refresh calls are sent as `application/x-www-form-urlencoded`, matching the provider token endpoints.

```csharp
using System.Security.Cryptography;

var auth = new AuthClient(new HttpClient(), TaminEndpoint.Sandbox);
var pkce = PkceChallenge.Create();
var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

Uri authorizeUrl = auth.CreateAuthorizationUrl(
    clientId: "YOUR_CLIENT_ID",
    redirectUri: new Uri("https://your-app.example/callback"),
    pkce: pkce,
    state: state);

// After callback: first verify the returned state against your stored state, then exchange the code.
TokenResult token = await auth.ExchangeCodeAsync(
    clientId: "YOUR_CLIENT_ID",
    code: "AUTHORIZATION_CODE",
    redirectUri: new Uri("https://your-app.example/callback"),
    pkce: pkce);
```

### Operation-level environment routing

Production and sandbox are not modeled as a single global `baseUrl` replacement. `TaminEnvironmentRoutes` resolves routes by `TaminEndpoint` and `TaminOperation` because provider routes differ by operation, including path spelling and parameter order. For example, the service-list route resolves to `https://soa.tamin.ir/interface/epresc/SendEpresc/v2/services` in production and `https://ep-test.tamin.ir/api/v2/ws-services` in sandbox. Unsupported environment/operation pairs throw `TaminRouteNotDefinedException` instead of falling back to an accidental URL.

```csharp
var routes = new TaminEnvironmentRoutes();
Uri sandboxServices = routes
    .Resolve(TaminEndpoint.Sandbox, TaminOperation.GetServices)
    .Uri;
```

### Refresh a token

```csharp
TokenResult refreshed = await session.RefreshTokenAsync(
    refreshToken: "REFRESH_TOKEN",
    clientId: "YOUR_CLIENT_ID");

string? newToken = refreshed.AccessToken ?? refreshed.Data;
```

For the provider v2 refresh endpoint, use the PKCE auth client so the refresh request is sent as form data and includes the required `audience` field:

```csharp
TokenResult refreshedV2 = await auth.RefreshTokenV2Async(
    clientId: "YOUR_CLIENT_ID",
    refreshToken: "REFRESH_TOKEN",
    audience: "NATIONAL_CODE_OR_AUDIENCE");
```

### Sign out

```csharp
Uri signOutUrl = auth.CreateSignOutUrl(new Uri("https://your-app.example/signed-out"));
await auth.SignOutAsync(new Uri("https://your-app.example/signed-out"));
```

### Validate a token

```csharp
ValidateTokenResult status = await session.ValidateTokenAsync("ACCESS_TOKEN");

if (status.Valid)
    Console.WriteLine($"Expires at {status.ExpiresAt}");
```

---

## Reference data

`session.Service` is a `ServiceClient`. Methods return `JsonElement` so callers can handle EP.Tamin schema changes without waiting for a package update.

### Typed query helpers

```csharp
JsonElement drugs = await session.Service.GetDrugListAsync(
    searchText: "aspirin",
    activeOnly: true,
    page: 1,
    pageSize: 20);

JsonElement services = await session.Service.GetServiceListAsync(
    serviceGroup: "imaging",
    activeOnly: true);
```

### Raw generated endpoint helpers

| Method | Description |
|---|---|
| `GetAllServicesAsync(query)` | Generated service list endpoint. |
| `GetPrescriptionTypeAsync(query)` | Generated prescription type endpoint. |
| `GetParaclinicTarefAsync(query)` | Generated paraclinic tariff endpoint. |
| `GetDrugAmountAsync(query)` | Generated drug amount endpoint. |
| `GetDrugInstructionAsync(query)` | Generated drug instruction endpoint. |

> **Not implemented:** previous README references to `GetAllowedCountAsync(...)` and `GetPriceAsync(...)` do not apply to this version.

---

## Prescription operations

`session.Prescription` is a `PrescriptionClient` for implemented e-prescription flows backed by generated Kiota request builders.

### Register prescriptions

```csharp
await session.Prescription.RegisterVisitPrescriptionAsync(new RegisterVisitPrescriptionRequest
{
    DoctorId = "doctor-id",
    PatientNationalId = "1234567890",
    VisitDate = "14030601",
    ClinicId = "clinic-id"
});

await session.Prescription.RegisterDrugPrescriptionAsync(new RegisterDrugPrescriptionRequest
{
    DoctorId = "doctor-id",
    PatientNationalId = "1234567890",
    VisitDate = "14030601",
    DrugItems = [new DrugItem { DrugCode = "DR001", Quantity = 2 }]
});

await session.Prescription.RegisterParaclinicPrescriptionAsync(new RegisterParaclinicPrescriptionRequest
{
    DoctorId = "doctor-id",
    PatientNationalId = "1234567890",
    VisitDate = "14030601",
    ServiceItems = [new ServiceItem { ServiceCode = "LAB001", ServiceGroup = "LAB", Quantity = 1 }]
});
```

Other implemented registration methods are:

- `RegisterMedicalServicePrescriptionAsync(...)`
- `RegisterReferralPrescriptionAsync(...)`
- `RegisterPhysiotherapyPrescriptionAsync(...)`

### Provider serialization and pre-send validation

Provider-bound identifiers that look numeric are intentionally modeled and serialized as strings. Keep values such as `DoctorId`, `PatientNationalId`, `DrugCode`, `ServiceCode`, referral identifiers, and private-practice identifiers in string form so leading zeros, dashes, and alphabetic prefixes or suffixes are preserved.

Provider date fields must already be eight-character Jalali strings, for example `14030601`. ISO/Gregorian-looking dates such as `2026-06-01` are rejected before any HTTP request is sent; convert dates in your application before calling the SDK.

```csharp
try
{
    await session.Prescription.RegisterDrugPrescriptionAsync(
        new RegisterDrugPrescriptionRequest
        {
            DoctorId = "000-DOC",
            PatientNationalId = "0012345678",
            VisitDate = "14030601",
            MobileNumber = "09123456789",
            DrugItems = [new DrugItem { DrugCode = "000-DR-A", Quantity = 1 }]
        });
}
catch (TaminValidationException ex)
{
    foreach (ValidationFailure failure in ex.Failures)
        Console.WriteLine($"{failure.Field}: {failure.Code} - {failure.Message}");
}
```

Use `ProfessionalIdentifierFormatter` when you need provider-specific professional identifier formatting:

```csharp
var formatter = new ProfessionalIdentifierFormatter();
string midwifeDoctorId = formatter.FormatMidwifeDoctorId("12م345"); // 12*345
string foreignDoctorCode = formatter.FormatForeignDoctorNationalCode("001-A"); // FIDA001-A
```

### Query, edit, delete, and warnings

```csharp
JsonElement registered = await session.Prescription.GetRegisteredPrescriptionAsync(
    headerId: 123,
    doctorNationalCode: "0012345678",
    doctorId: "doctor-id");

JsonElement edited = await session.Prescription.EditElectronicPrescriptionAsync(
    new EditPrescriptionRequest
    {
        HeaderId = 123,
        DoctorNationalCode = "0012345678",
        DoctorId = "doctor-id",
        EditedItems = [new Vorn.Tamin.Kiota.Models.NoteDetailEprsc { SrvQty = 1 }]
    });

JsonElement deleted = await session.Prescription.DeleteElectronicPrescriptionAsync(
    new DeletePrescriptionRequest
    {
        HeaderId = 123,
        DoctorNationalCode = "0012345678",
        DoctorId = "doctor-id"
    });

JsonElement warnings = await session.Prescription.CheckPrescriptionWarningAsync(
    new CheckWarningRequest
    {
        PatientNationalId = "1234567890",
        DoctorId = "doctor-id",
        PrescriptionItems = [new Vorn.Tamin.Kiota.Models.GridData()]
    });
```

---

## Parsing API responses

Most public client methods return the unwrapped API payload as `JsonElement`:

```csharp
JsonElement raw = await session.Service.GetDrugListAsync(searchText: "aspirin");

if (raw.ValueKind == JsonValueKind.Array)
{
    foreach (JsonElement item in raw.EnumerateArray())
        Console.WriteLine(item);
}
```

When you manually consume raw EP.Tamin JSON outside of the SDK, deserialize the complete envelope with `TaminResponse<T>`:

```csharp
var taminResponse = JsonSerializer.Deserialize<TaminResponse<TokenResult>>(rawJson);
if (taminResponse?.Success == true)
    Console.WriteLine(taminResponse.TrackingCode);
```

---

## Error handling

### SDK exceptions

| Exception | When thrown |
|---|---|
| `AuthTokenNotSuppliedException` | `TaminSession` is constructed without a token while `needToken` is `true`. |
| `UserLoginException` | Login failed; exposes `Status`, `Family`, and `ReasonText` when the API body supplies them. |
| `PrescriptionNotCreatedException` | Prescription creation rejected; exposes `ErrorCode`. |
| `MissingParamException` | A required method parameter was null or empty. |
| `MissingConfigException` | A required configuration key is absent. |
| `InvalidConfigException` | A configuration value is present but invalid. |
| `TaminValidationException` | Provider-bound values fail structured client-side validation before transport. |

### Normalized provider errors

Every HTTP error derived from `ConnectionError` exposes a `ProviderError` property when the SDK can normalize the provider response. `TaminProviderError` preserves the raw HTTP status, reason phrase, provider body, extracted provider message, operation, and environment so support teams can reconcile failures without losing the original provider payload.

`TaminErrorNormalizer` maps known provider failures into `TaminErrorCategory` values:

| Category | Use it for | Typical action |
|---|---|---|
| `ClientPreventable` | Invalid `prescType`/`srvType` pairs, missing laboratory subgroup, null or negative quantities, empty payloads, missing or malformed patient mobile numbers, invalid patient national codes, unknown `srvCode`, missing/invalid prescription types, date format errors, future dates, and invalid `drugAmntId`/`drugInstId`. | Fix request data before resending. |
| `SupportRequired` | Doctor enrollment/activation failures and doctor national-code/mobile mismatches. | Escalate with onboarding or provider-support evidence. |
| `Retryable` | Temporary provider failures and duplicate-submission risk. | Do not blindly resubmit state-changing calls; check existing prescription state first. |
| `ProviderContractMismatch` | Responses that reflect documented provider specification conflicts such as `id_client` versus `client_id` or string-versus-number ambiguity. | Capture the raw provider body and reconcile against the provider specification. |
| `UnknownProviderError` | Any provider message outside the current catalog. | Preserve the raw payload and decide whether a new catalog rule is justified. |

### Prevent, normalize, escalate

1. **Prevent:** run SDK validation before transport; prescription clients already validate provider-bound request shapes and throw `TaminValidationException` before sending invalid payloads.
2. **Normalize:** catch `ConnectionError` and inspect `ex.ProviderError` for the normalized code, category, operation, environment, status, and raw body.
3. **Escalate:** for `SupportRequired`, `ProviderContractMismatch`, or unknown errors, include `ProviderError.Code`, `OperationName`, `Environment`, `StatusCode`, and `ProviderBody` in support tickets.

```csharp
try
{
    JsonElement result = await session.Prescription.RegisterDrugPrescriptionAsync(request);
}
catch (ConnectionError ex) when (ex.ProviderError is { } providerError)
{
    Console.WriteLine($"{providerError.Category}: {providerError.Code}");
    Console.WriteLine($"{providerError.OperationName} / {providerError.Environment}");
    Console.WriteLine(providerError.ProviderBody);
}
```

### HTTP exceptions

All HTTP error responses throw a subclass of `ConnectionError`. The base class exposes `StatusCode`, `ReasonPhrase`, and `Content`.

| Exception | HTTP status |
|---|---|
| `Redirection` | 301 / 302 / 303 / 307 |
| `BadRequest` | 400 |
| `UnauthorizedAccess` | 401 |
| `ForbiddenAccess` | 403 |
| `ResourceNotFound` | 404 |
| `MethodNotAllowed` | 405 |
| `ResourceConflict` | 409 |
| `ResourceGone` | 410 |
| `ResourceInvalid` | 422 |
| `ClientError` | Other 4xx |
| `ServerError` | 5xx |

### Example

```csharp
try
{
    JsonElement result = await session.Prescription.RegisterDrugPrescriptionAsync(request);
}
catch (UnauthorizedAccess)
{
    await session.RefreshTokenAsync(savedRefreshToken);
}
catch (TaminValidationException ex)
{
    foreach (ValidationFailure failure in ex.Failures)
        Console.WriteLine($"{failure.Field}: {failure.Code} - {failure.Message}");
}
catch (ResourceInvalid ex)
{
    Console.WriteLine($"Provider validation failed: {ex.ProviderError?.Code ?? ex.Content}");
}
catch (ServerError)
{
    // Temporary outage: back off and retry according to your application's policy.
}
```

---

## Artifact inventory

### Primary user-facing artifacts

| Artifact | Status | Description |
|---|---|---|
| `TaminClient` | Implemented | Role-aware facade over `TaminSession`. |
| `TaminSession` | Implemented | Main entry point. Creates domain clients and selects production or sandbox generated gateways. |
| `TaminSession.CreateAsync` | Implemented | Creates a session and optionally performs login. |
| `TaminSession.RefreshTokenAsync` | Implemented | Refreshes a bearer token. |
| `TaminSession.ValidateTokenAsync` | Implemented | Validates an access token. |
| `ReferenceDataClient` (`session.ReferenceData`) | Implemented | Reference-data queries. |
| `ServiceClient` (`session.Service`) | Implemented | Backward-compatible reference-data and service lookups. |
| `PrescriptionClient` (`session.Prescription`) | Implemented | Prescription registration, lookup, mutation, and warning checks. |
| `DoctorClient` (`session.Doctor`) | Implemented | Doctor-facing prescription, dental, referral, and reference-data workflows. |
| `SecretaryClient` (`session.Secretary`) | Implemented | Secretary-facing eligibility and hospitalization workflow grouping. |
| `NurseClient` (`session.Nurse`) | Explicitly not implemented | Nurse methods fail with `TaminWorkflowNotImplementedException` until adapters are wired. |
| `IdentityClient` (`session.Identity`) | Not implemented | Placeholder only; no public operation methods. |
| `PharmacyClient` (`session.Pharmacy`) | Not implemented | Placeholder only; no public operation methods. |
| `ParaclinicClient` (`session.Paraclinic`) | Not implemented | Placeholder only; no public operation methods. |
| `AddTaminClient` | Implemented | Registers `TaminSession` with `IHttpClientFactory`. |
| `TaminOptions` | Implemented | Configuration POCO for DI and `appsettings.json` binding. |

### DTOs and enums

| Type | Status | Domain |
|---|---|---|
| `TokenResult` | Implemented | Authentication |
| `ValidateTokenResult` | Implemented | Authentication |
| `DrugItem` | Implemented | Prescription writing |
| `ServiceItem` | Implemented | Prescription writing |
| `PhysiotherapyItem` | Implemented | Prescription writing |
| `RegisterVisitPrescriptionRequest` | Implemented | Prescription writing |
| `RegisterDrugPrescriptionRequest` | Implemented | Prescription writing |
| `RegisterParaclinicPrescriptionRequest` | Implemented | Prescription writing |
| `RegisterMedicalServicePrescriptionRequest` | Implemented | Prescription writing |
| `RegisterReferralPrescriptionRequest` | Implemented | Prescription writing |
| `RegisterPhysiotherapyPrescriptionRequest` | Implemented | Prescription writing |
| `EditPrescriptionRequest` | Implemented | Prescription mutation |
| `DeletePrescriptionRequest` | Implemented | Prescription mutation |
| `CheckWarningRequest` | Implemented | Warning services |
| `EligibilityLookupRequest` | Implemented | Eligibility lookup |
| `NurseTodoListRequest` | Explicitly not implemented | Nurse workflow request shape |
| `NurseActionWorkflowRequest` | Explicitly not implemented | Nurse workflow request shape |
| `HospitalizationCreateRequest` | Explicitly not implemented | Hospitalization workflow request shape |
| `HospitalizationSecretaryListRequest` | Explicitly not implemented | Hospitalization workflow request shape |
| `PrescriptionType` | Implemented | Prescription writing |
| Identity, entitlement, pharmacy, paraclinic dispensing, pricing, and typed reference result DTOs | Not implemented | Use `JsonElement` or wait for generated endpoint support. |

---

## Limitations & compatibility notes

- **Target framework:** `net10.0` only. Earlier .NET versions are not supported.
- **Default endpoint:** `TaminSession` and `TaminOptions` default to `TaminEndpoint.Production`; set `TaminEndpoint.Sandbox` for sandbox request builders and default sandbox base URL.
- **Manual `HttpClient` ownership:** when constructing `TaminSession` directly, supply and manage your own `HttpClient` lifetime.
- **`JsonElement` return type:** client methods return raw unwrapped payloads to stay compatible with EP.Tamin response changes.
- **No automatic token refresh:** monitor token expiry and call `RefreshTokenAsync` from your application policy.
- **Not implemented placeholders:** `IdentityClient`, `PharmacyClient`, and `ParaclinicClient` are intentionally empty until generated Kiota builders exist for those endpoint groups. Nurse, referral feedback/count/detail, and hospitalization methods throw `TaminWorkflowNotImplementedException` so unavailable role workflows are explicit.

### Provider compatibility notes

The EP.Tamin provider materials contain specification inconsistencies that affect code generation, request construction, and support diagnosis:

- **Version labels:** provider materials reference both version `1.9.4` and version `1.9.3`; verify the table, example, business note, and error section before treating a field as stable.
- **Client identifier naming:** authentication examples and notes may use both `id_client` and `client_id`. The SDK sends the documented operation-specific shape, but support escalation should mention the exact field name used in the failed payload.
- **String-versus-number ambiguity:** provider tables sometimes describe numeric values while examples carry string values. The SDK preserves provider-bound identifiers as strings where leading zeros or code semantics matter.
- **Dental flag typing:** `isDentalService` appears with string/numeric ambiguity, so callers should not assume boolean coercion unless a specific provider operation documents it consistently.
- **Sandbox route discrepancies:** sandbox paths and spellings can differ from production paths, including `api/v2/ws-services`, `api/v2/SendEpresc`, and prescription detail/update path segments. `TaminEnvironmentRoutes` keeps operation-level routes instead of deriving sandbox URLs from production URLs.
- **Cross-section reconciliation:** payloads and routes must be reconciled across provider tables, JSON examples, business notes, and error sections. Do not generate behavior from a single table when another provider section contradicts it.

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for prerequisites, code-style guidelines, and pull-request instructions.

```bash
dotnet test Vorn.Tamin.slnx
```

---

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

---

## License

[MIT](LICENSE) © Vorn.Tamin Contributors

---

**Repository:** <https://github.com/mobinseven/Vorn.Tamin>  
**Issues:** <https://github.com/mobinseven/Vorn.Tamin/issues>
