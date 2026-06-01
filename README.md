# EP.Tamin.NET

[![NuGet](https://img.shields.io/nuget/v/EP.Tamin.NET)](https://www.nuget.org/packages/EP.Tamin.NET)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**کارآور سامانه نسخه الکترونیک تامین اجتماعی در .NET**

A .NET 10 client SDK for the [EP.Tamin](https://ep.tamin.ir) electronic prescription API of the Social Security Insurance of Iran (سازمان تأمین اجتماعی). Inspired by the Python [`Mazafard/tamin-sdk`](https://github.com/Mazafard/tamin-sdk).

---

## Table of contents

1. [What problem does this solve?](#what-problem-does-this-solve)
2. [Installation](#installation)
3. [Prerequisites](#prerequisites)
4. [Quick-start](#quick-start)
5. [Dependency injection setup](#dependency-injection-setup)
6. [Authentication](#authentication)
7. [Reference data (`session.Service`)](#reference-data-sessionservice)
8. [Identity & eligibility (`session.Identity`)](#identity--eligibility-sessionidentity)
9. [E-prescription writing (`session.Prescription`)](#e-prescription-writing-sessionprescription)
10. [Prescription query & mutation (`session.Prescription`)](#prescription-query--mutation-sessionprescription)
11. [Warning services (`session.Prescription`)](#warning-services-sessionprescription)
12. [Pharmacy dispensing (`session.Pharmacy`)](#pharmacy-dispensing-sessionpharmacy)
13. [Paraclinic service delivery (`session.Paraclinic`)](#paraclinic-service-delivery-sessionparaclinic)
14. [Parsing API responses](#parsing-api-responses)
15. [Error handling](#error-handling)
16. [Artifact inventory](#artifact-inventory)
17. [Limitations & compatibility notes](#limitations--compatibility-notes)
18. [Contributing](#contributing)
19. [Changelog](#changelog)
20. [License](#license)

---

## What problem does this solve?

Calling the EP.Tamin REST API from .NET requires assembling authentication headers, serialising request/response bodies, handling custom status envelopes, mapping HTTP status codes to typed exceptions, and managing per-request tracing identifiers. **EP.Tamin.NET** encapsulates all of that in a single, lightweight SDK so you can focus on your application logic.

---

## Installation

### dotnet CLI
```bash
dotnet add package EP.Tamin.NET
```

### Package Manager Console
```powershell
Install-Package EP.Tamin.NET
```

### PackageReference
```xml
<PackageReference Include="EP.Tamin.NET" Version="*" />
```

---

## Prerequisites

| Requirement | Minimum version |
|---|---|
| .NET | 10.0 |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.0 |
| `Microsoft.Extensions.Http` | 10.0.0 |
| `Microsoft.Extensions.Options` | 10.0.0 |

The three `Microsoft.Extensions.*` packages are transitive dependencies — you do not need to install them separately.

---

## Quick-start

```csharp
using EP.Tamin.NET;
using System.Text.Json;

// ── Option A: pre-obtained bearer token ──────────────────────────────────────
var session = new TaminSession(
    new HttpClient(),
    oauthToken: "YOUR_TOKEN",
    clientId:   "YOUR_CLIENT_ID");          // Client-Id header issued at API onboarding

// ── Option B: log in with username / password (+ optional OTP) ───────────────
var session = await TaminSession.CreateAsync(
    new HttpClient(),
    username:            "your-username",
    password:            "your-password",
    otp:                 "123456",          // omit if OTP is not required
    providerIdentifier:  "prov-id",        // omit if not required
    clientId:            "YOUR_CLIENT_ID",
    baseUri:             new Uri("https://ep.tamin.ir/api/")); // production

// ── Reference data ────────────────────────────────────────────────────────────
JsonElement drugs = await session.Service.GetDrugListAsync(
    searchText: "amoxicillin", activeOnly: true);

// ── Verify patient identity ───────────────────────────────────────────────────
JsonElement identity = await session.Identity.VerifyIdentityAsync(
    new VerifyIdentityRequest { NationalId = "1234567890" });

// ── Register a drug prescription ─────────────────────────────────────────────
JsonElement result = await session.Prescription.RegisterDrugPrescriptionAsync(
    new RegisterDrugPrescriptionRequest
    {
        DoctorId          = "doctor-id",
        PatientNationalId = "1234567890",
        VisitDate         = "2024-06-01",
        DrugItems         =
        [
            new DrugItem { DrugCode = "DR001", Quantity = 2, DosageInstruction = "twice daily" }
        ]
    });
```

---

## Dependency injection setup

Register the SDK in `Program.cs` or `Startup.cs`:

```csharp
using EP.Tamin.NET.Extensions;

builder.Services.AddTaminClient(o =>
{
    o.BaseUrl    = "https://ep-test.tamin.ir/api/"; // sandbox (default); set production URL for live
    o.ClientId   = "YOUR_CLIENT_ID";
    o.OAuthToken = "YOUR_TOKEN";                    // supply a token -OR- set Username + Password below
    // o.Username = "your-username";
    // o.Password = "your-password";
});
```

`TaminSession` is registered as a **scoped** service backed by `IHttpClientFactory`. Inject it normally:

```csharp
public class PrescriptionService(TaminSession tamin)
{
    public async Task<JsonElement> IssueAsync(string doctorId, string patientId, string visitDate)
        => await tamin.Prescription.RegisterDrugPrescriptionAsync(
               new RegisterDrugPrescriptionRequest
               {
                   DoctorId          = doctorId,
                   PatientNationalId = patientId,
                   VisitDate         = visitDate,
                   DrugItems         = [new DrugItem { DrugCode = "DR001", Quantity = 1 }]
               });
}
```

### `appsettings.json` binding

You can bind `TaminOptions` from configuration:

```json
{
  "Tamin": {
    "BaseUrl":    "https://ep.tamin.ir/api/",
    "ClientId":   "YOUR_CLIENT_ID",
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
    oauthToken: "YOUR_TOKEN");
```

### Log in at runtime

`TaminSession.CreateAsync` performs a POST to `/ws/api/auth/login` and stores the returned token:

```csharp
var session = await TaminSession.CreateAsync(
    new HttpClient(),
    username: "user",
    password: "pass");
```

### Refresh a token

```csharp
TokenResult refreshed = await session.RefreshTokenAsync(
    refreshToken: "REFRESH_TOKEN",
    clientId:     "YOUR_CLIENT_ID");

string? newToken = refreshed.AccessToken ?? refreshed.Data;
```

### Validate a token

```csharp
ValidateTokenResult status = await session.ValidateTokenAsync("ACCESS_TOKEN");

if (status.Valid)
    Console.WriteLine($"Expires at {status.ExpiresAt}");
```

### Key types

| Type | Description |
|---|---|
| `TokenResult` | Returned by `RefreshTokenAsync`. Contains `AccessToken`, `RefreshToken`, `ExpiresIn`, `UserRoles`. |
| `ValidateTokenResult` | Returned by `ValidateTokenAsync`. Contains `Valid`, `ExpiresAt`, `UserInfo`. |

---

## Reference data (`session.Service`)

`session.Service` is a `ServiceClient` instance. All methods return `JsonElement` for maximum flexibility.

### Search the drug catalogue

```csharp
JsonElement drugs = await session.Service.GetDrugListAsync(
    searchText: "aspirin",
    activeOnly: true,
    page:       1,
    pageSize:   20);
```

### Search the service catalogue

```csharp
JsonElement services = await session.Service.GetServiceListAsync(
    serviceGroup: "imaging",
    activeOnly:   true);
```

### Check allowed quantities/limits

```csharp
JsonElement limit = await session.Service.GetAllowedCountAsync(
    patientNationalId: "1234567890",
    itemCode:          "DR001",
    itemType:          "drug",
    doctorId:          "doctor-id",
    date:              "2024-06-01");
```

### Get price breakdown

```csharp
JsonElement price = await session.Service.GetPriceAsync(
    itemCode:          "DR001",
    itemType:          "drug",
    quantity:          2,
    patientNationalId: "1234567890");
```

### Additional reference endpoints

| Method | Description |
|---|---|
| `GetAllServicesAsync(query)` | Raw service list (legacy; use `GetServiceListAsync` for typed parameters) |
| `GetPrescriptionTypeAsync(query)` | Official prescription-type reference table |
| `GetParaclinicTarefAsync(query)` | Paraclinic tariff table |
| `GetDrugAmountAsync(query)` | Drug amounts / reference data (legacy; use `GetDrugListAsync`) |
| `GetDrugInstructionAsync(query)` | Drug administration instructions |

---

## Identity & eligibility (`session.Identity`)

### Verify patient identity

```csharp
JsonElement result = await session.Identity.VerifyIdentityAsync(
    new VerifyIdentityRequest
    {
        NationalId          = "1234567890",
        BirthDate           = "1370-01-01",  // optional
        MobileNumber        = "09121234567", // optional
        ForeignerIdentifier = null           // for foreign patients
    });
```

### Check treatment entitlement

```csharp
JsonElement entitlement = await session.Identity.CheckEntitlementAsync(
    new CheckEntitlementRequest
    {
        NationalId  = "1234567890",
        ProviderId  = "clinic-id",
        VisitDate   = "2024-06-01",
        ServiceType = "clinic"
    });
```

---

## E-prescription writing (`session.Prescription`)

All registration methods post to the EP.Tamin prescribing endpoint and return a `JsonElement`. The `prescription_type` discriminator is set automatically by each request type.

### Visit-only prescription

```csharp
JsonElement r = await session.Prescription.RegisterVisitPrescriptionAsync(
    new RegisterVisitPrescriptionRequest
    {
        DoctorId          = "doctor-id",
        PatientNationalId = "1234567890",
        VisitDate         = "2024-06-01",
        ClinicId          = "clinic-id",
        DiagnosisCode     = "J06.9"
    });
```

### Drug prescription

```csharp
JsonElement r = await session.Prescription.RegisterDrugPrescriptionAsync(
    new RegisterDrugPrescriptionRequest
    {
        DoctorId          = "doctor-id",
        PatientNationalId = "1234567890",
        VisitDate         = "2024-06-01",
        DrugItems         =
        [
            new DrugItem
            {
                DrugCode          = "DR001",
                Quantity          = 2,
                DosageInstruction = "twice daily",
                Frequency         = "BID",
                Duration          = "7 days",
                Route             = "oral"
            }
        ]
    });
```

### Paraclinic prescription (lab, imaging, diagnostics)

```csharp
JsonElement r = await session.Prescription.RegisterParaclinicPrescriptionAsync(
    new RegisterParaclinicPrescriptionRequest
    {
        DoctorId          = "doctor-id",
        PatientNationalId = "1234567890",
        VisitDate         = "2024-06-01",
        ServiceItems      =
        [
            new ServiceItem { ServiceCode = "LAB001", Quantity = 1, ServiceGroup = "lab" }
        ]
    });
```

### Medical service prescription

```csharp
JsonElement r = await session.Prescription.RegisterMedicalServicePrescriptionAsync(
    new RegisterMedicalServicePrescriptionRequest
    {
        DoctorId          = "doctor-id",
        PatientNationalId = "1234567890",
        VisitDate         = "2024-06-01",
        ServiceItems      = [new ServiceItem { ServiceCode = "SVC001", Quantity = 1 }]
    });
```

### Referral prescription

```csharp
JsonElement r = await session.Prescription.RegisterReferralPrescriptionAsync(
    new RegisterReferralPrescriptionRequest
    {
        DoctorId           = "doctor-id",
        PatientNationalId  = "1234567890",
        VisitDate          = "2024-06-01",
        TargetSpecialty    = "cardiology",
        TargetProviderType = "clinic",
        Reason             = "heart evaluation"
    });
```

### Physiotherapy prescription

```csharp
JsonElement r = await session.Prescription.RegisterPhysiotherapyPrescriptionAsync(
    new RegisterPhysiotherapyPrescriptionRequest
    {
        DoctorId           = "doctor-id",
        PatientNationalId  = "1234567890",
        PhysiotherapyItems = [new PhysiotherapyItem { ServiceCode = "PHY001" }],
        SessionCount       = 10,
        EffectiveDate      = "2024-06-01"
    });
```

---

## Prescription query & mutation (`session.Prescription`)

### Retrieve a prescription

```csharp
JsonElement detail = await session.Prescription.GetRegisteredPrescriptionAsync(
    prescriptionId:    "P12345",
    trackingCode:      "TC67890",
    patientNationalId: "1234567890");
```

### List prescriptions with filters

```csharp
JsonElement list = await session.Prescription.GetPrescriptionListAsync(
    new PrescriptionListFilter
    {
        DoctorId          = "doctor-id",
        PatientNationalId = "1234567890",
        FromDate          = "2024-01-01",
        ToDate            = "2024-06-30",
        PrescriptionType  = PrescriptionType.Drug,
        Status            = "accepted"
    });
```

### Edit a prescription

```csharp
JsonElement edited = await session.Prescription.EditElectronicPrescriptionAsync(
    new EditPrescriptionRequest
    {
        PrescriptionId = "P12345",
        TrackingCode   = "TC67890",
        EditedItems    = [ /* updated items */ ],
        EditReason     = "Dosage correction"
    });
```

### Delete a prescription

```csharp
JsonElement deleted = await session.Prescription.DeleteElectronicPrescriptionAsync(
    new DeletePrescriptionRequest
    {
        PrescriptionId = "P12345",
        TrackingCode   = "TC67890",
        DeleteReason   = "Entry error"
    });
```

### Retrieve a referral prescription

```csharp
JsonElement referral = await session.Prescription.GetReferralPrescriptionAsync(
    referralPrescriptionId: "RP001",
    trackingCode:           "TC001",
    patientNationalId:      "1234567890");
```

---

## Warning services (`session.Prescription`)

Check for clinical or insurance warnings **before** submitting a prescription:

```csharp
JsonElement warnings = await session.Prescription.CheckPrescriptionWarningAsync(
    new CheckWarningRequest
    {
        PatientNationalId  = "1234567890",
        DoctorId           = "doctor-id",
        PrescriptionItems  = [ /* items to check */ ]
    });
```

Each warning item in the response corresponds to `WarningItem`:

| Property | Description |
|---|---|
| `WarningCode` | Machine-readable code |
| `WarningType` | Category of warning |
| `Severity` | e.g. `error`, `warning`, `info` |
| `Message` | Human-readable text |
| `CanContinueFlag` | Whether submission can proceed despite this warning |
| `RequiresConfirmationFlag` | Whether the user must acknowledge the warning |

---

## Pharmacy dispensing (`session.Pharmacy`)

`session.Pharmacy` is a `PharmacyClient`. The typical dispensing workflow is:

1. **Check entitlement** — verify the patient has active coverage.
2. **Register paper prescription** (if applicable) — when converting a paper Rx to electronic.
3. **Get prescription list / details** — fetch the Rx waiting for dispensing.
4. **Check warnings** — look for drug-interaction or insurance warnings.
5. **Dispense** — submit the dispensed items.
6. **Register / activate authenticity codes** — for tracked medications.

```csharp
// 1. Entitlement
await session.Pharmacy.CheckEntitlementAsync(
    new CheckEntitlementRequest
    {
        NationalId  = "1234567890",
        ProviderId  = "pharmacy-id",
        VisitDate   = "2024-06-01",
        ServiceType = "pharmacy"
    });

// 2. Register paper prescription (optional)
await session.Pharmacy.RegisterPaperPrescriptionAsync(
    new RegisterPaperPrescriptionRequest
    {
        PatientNationalId       = "1234567890",
        PaperPrescriptionNumber = "PP001"
    });

// 3. Fetch pending prescription
JsonElement details = await session.Pharmacy.GetPrescriptionDetailsAsync("P001", "TC001");

// 4. Check warnings
await session.Pharmacy.CheckPrescriptionWarningsAsync(
    new CheckWarningRequest
    {
        PatientNationalId = "1234567890",
        DoctorId          = "doctor-id",
        PrescriptionItems = [ /* items */ ]
    });

// 5a. Dispense electronic prescription
await session.Pharmacy.DispenseElectronicPrescriptionAsync(
    new DispensePrescriptionRequest
    {
        PrescriptionId = "P001",
        TrackingCode   = "TC001",
        DispensedItems = [ /* dispensed items */ ],
        PharmacistId   = "pharmacist-id"
    });

// 5b. Dispense with an acknowledged warning
await session.Pharmacy.DispenseWithWarningAsync(
    new DispensePrescriptionRequest { PrescriptionId = "P001", TrackingCode = "TC001", DispensedItems = [] });

// 6. Register and activate drug authenticity/tracking code
await session.Pharmacy.RegisterDrugAuthenticityCodeAsync(
    new DrugAuthenticityRequest
    {
        PrescriptionId   = "P001",
        DrugCode         = "DR001",
        AuthenticityCode = "SERIAL123"
    });

await session.Pharmacy.ActivateDrugAuthenticityCodeAsync(
    new DrugAuthenticityRequest { PrescriptionId = "P001", DrugCode = "DR001", AuthenticityCode = "SERIAL123" });
```

### Additional pharmacy operations

| Method | Description |
|---|---|
| `GetPrescriptionListAsync(query)` | Fetch prescriptions pending dispensing |
| `TwoStepElectronicDispensingAsync(request)` | Two-step electronic dispensing workflow |
| `GetActivatedBarcodeAsync(prescriptionId, trackingCode)` | Show activated barcode |
| `GetPriceAsync(prescriptionId, trackingCode)` | Price and insurance-share breakdown |
| `DeleteDispensingRecordAsync(request)` | Cancel a dispensing record |
| `ReferPrescriptionToDoctorAsync(request)` | Return a prescription to the prescribing doctor |

---

## Paraclinic service delivery (`session.Paraclinic`)

`session.Paraclinic` is a `ParaclinicClient`. The workflow mirrors pharmacy dispensing:

```csharp
// Check entitlement
await session.Paraclinic.CheckEntitlementAsync(
    new CheckEntitlementRequest
    {
        NationalId  = "1234567890",
        ProviderId  = "lab-id",
        VisitDate   = "2024-06-01",
        ServiceType = "lab"
    });

// Get prescription list / details
JsonElement pending = await session.Paraclinic.GetPrescriptionListAsync();
JsonElement detail  = await session.Paraclinic.GetPrescriptionDetailsAsync("P001", "TC001");

// Provide electronic service
await session.Paraclinic.ProvideElectronicPrescriptionServiceAsync(
    new ProvideServiceRequest
    {
        PrescriptionId = "P001",
        TrackingCode   = "TC001",
        DeliveredItems = [ /* delivered services */ ],
        ProviderId     = "lab-id"
    });

// Price breakdown
JsonElement price = await session.Paraclinic.GetPriceAsync("P001", "TC001");
```

### Additional paraclinic operations

| Method | Description |
|---|---|
| `RegisterPaperPrescriptionAsync(request)` | Register a paper prescription for paper-based workflows |
| `ProvidePaperPrescriptionServiceAsync(request)` | Deliver paper prescription services |
| `ProvideServiceWithWarningAsync(request)` | Deliver with an acknowledged warning |
| `DeleteServiceDeliveryRecordAsync(request)` | Cancel a service delivery record |

---

## Parsing API responses

All domain-client methods return `JsonElement`. You can deserialize into a typed DTO manually or use the provided response types:

```csharp
using System.Text.Json;

JsonElement raw = await session.Identity.VerifyIdentityAsync(
    new VerifyIdentityRequest { NationalId = "1234567890" });

// Access individual fields
string? name = raw.TryGetProperty("patient_name", out var n) ? n.GetString() : null;

// Or deserialize to a known type
IdentityResult identity = raw.Deserialize<IdentityResult>()!;
Console.WriteLine(identity.PatientName);
```

### `TaminResponse<T>` — standard API envelope

If you need access to the full envelope including `Success`, `StatusCode`, `Message`, `TrackingCode`, `CorrelationId`, or `Errors`, deserialize the raw HTTP body into `TaminResponse<T>`:

```csharp
// Advanced: manually read the raw response and deserialize the full envelope
var taminResponse = JsonSerializer.Deserialize<TaminResponse<IdentityResult>>(rawJson);
if (taminResponse?.Success == true)
    Console.WriteLine(taminResponse.TrackingCode);
```

> **Note:** The SDK automatically unwraps the `data` field from the envelope. Methods return the unwrapped payload as a `JsonElement`, not the full `TaminResponse<T>`. Use `TaminResponse<T>` only if you are parsing raw API responses outside of the SDK.

---

## Error handling

### SDK exceptions

| Exception | When thrown |
|---|---|
| `AuthTokenNotSuppliedException` | `TaminSession` constructed without a token when `needToken: true` |
| `UserLoginException` | Login failed; exposes `Status`, `Family`, `ReasonText` from the API body |
| `PrescriptionNotCreatedException` | Prescription creation rejected; exposes `ErrorCode` |
| `MissingParamException` | A required method parameter was null or empty |
| `MissingConfigException` | A required configuration key is absent |
| `InvalidConfigException` | A configuration value is present but invalid |

### HTTP exceptions

All HTTP error responses throw a subclass of `ConnectionError`. The base class exposes `StatusCode`, `ReasonPhrase`, and `Content` (raw response body).

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

### Domain / business exceptions

Thrown by higher-level validation logic in your application or middleware, not directly by the SDK HTTP layer:

| Exception | Meaning |
|---|---|
| `AuthenticationError` | Invalid credentials, expired token, or missing OTP |
| `AuthorizationError` | Provider not authorised for the operation |
| `IdentityError` | Patient identity verification failed |
| `EntitlementError` | Patient does not have valid treatment coverage |
| `ValidationError` | Required field missing, invalid code, invalid date |
| `BusinessRuleError` | Prescription violates an insurance or medical rule |
| `DuplicateSubmissionRisk` | Timeout or retry; check status before retrying |
| `TemporaryServiceError` | External service temporarily unavailable |

### Example: catch-all error handling

```csharp
try
{
    var result = await session.Prescription.RegisterDrugPrescriptionAsync(request);
}
catch (UnauthorizedAccess)
{
    // Token expired — refresh and retry
    await session.RefreshTokenAsync(savedRefreshToken);
}
catch (ResourceInvalid ex)
{
    Console.WriteLine($"Validation failed: {ex.Content}");
}
catch (DuplicateSubmissionRisk)
{
    // Query before retrying to avoid double-submission
    var existing = await session.Prescription.GetRegisteredPrescriptionAsync(
        prescriptionId, trackingCode, patientNationalId);
}
catch (ServerError)
{
    // Temporary outage — back off and retry
}
```

---

## Artifact inventory

### Primary user-facing

| Artifact | Type | Description |
|---|---|---|
| `TaminSession` | Class | Main entry point. Holds all domain clients and manages HTTP authentication. |
| `TaminSession.CreateAsync` | Static factory | Creates a session, performing login if no token is provided. |
| `TaminSession.RefreshTokenAsync` | Method | Refreshes the bearer token. |
| `TaminSession.ValidateTokenAsync` | Method | Validates the current bearer token. |
| `ServiceClient` (via `session.Service`) | Class | Reference data: drug list, service list, pricing, allowed counts. |
| `PrescriptionClient` (via `session.Prescription`) | Class | E-prescription writing, query, mutation, and warnings. |
| `IdentityClient` (via `session.Identity`) | Class | Patient identity verification and treatment eligibility. |
| `PharmacyClient` (via `session.Pharmacy`) | Class | Pharmacy dispensing: entitlement check, paper/electronic dispense, barcode, price. |
| `ParaclinicClient` (via `session.Paraclinic`) | Class | Paraclinic service delivery: entitlement check, paper/electronic provision, price. |
| `AddTaminClient` | DI extension method | Registers `TaminSession` as scoped with `IHttpClientFactory`. |
| `TaminOptions` | Options class | Configuration POCO for DI / `appsettings.json` binding. |

### Request / response DTOs

| Type | Domain |
|---|---|
| `GetTokenRequest` | Authentication |
| `TokenResult` | Authentication |
| `ValidateTokenResult` | Authentication |
| `VerifyIdentityRequest` | Identity |
| `IdentityResult` | Identity |
| `CheckEntitlementRequest` | Identity / Pharmacy / Paraclinic |
| `EntitlementResult` | Identity |
| `DrugItem` | Prescription writing |
| `ServiceItem` | Prescription writing |
| `PhysiotherapyItem` | Prescription writing |
| `RegisterVisitPrescriptionRequest` | Prescription writing |
| `RegisterDrugPrescriptionRequest` | Prescription writing |
| `RegisterParaclinicPrescriptionRequest` | Prescription writing |
| `RegisterMedicalServicePrescriptionRequest` | Prescription writing |
| `RegisterReferralPrescriptionRequest` | Prescription writing |
| `RegisterPhysiotherapyPrescriptionRequest` | Prescription writing |
| `PrescriptionResult` | Prescription writing result |
| `PrescriptionListFilter` | Prescription query |
| `EditPrescriptionRequest` | Prescription mutation |
| `DeletePrescriptionRequest` | Prescription mutation |
| `DrugReference` | Reference data |
| `ServiceReference` | Reference data |
| `AllowedCountResult` | Reference data |
| `PriceResult` | Reference data / pricing |
| `WarningItem` | Warning services |
| `CheckWarningRequest` | Warning services |
| `RegisterPaperPrescriptionRequest` | Pharmacy / Paraclinic |
| `DispensePrescriptionRequest` | Pharmacy dispensing |
| `DrugAuthenticityRequest` | Pharmacy authenticity |
| `ReferPrescriptionRequest` | Pharmacy referral |
| `ProvideServiceRequest` | Paraclinic delivery |

### Enums

| Type | Values |
|---|---|
| `PrescriptionType` | `Drug(1)`, `Paraclinic(2)`, `Visit(3)`, `VisitService(4)`, `Service(5)`, `Referral(6)`, `Physiotherapy(7)` |
| `PrescriptionStatus` | `Draft`, `Submitted`, `Accepted`, `Rejected`, `Warning`, `Edited`, `Deleted`, `PendingSync`, `Failed` |

### Supporting / advanced

| Type | Notes |
|---|---|
| `TaminResponse<T>` | Standard API envelope — useful if you consume raw JSON outside the SDK. |
| `Prescription` | Low-level key/value prescription dictionary. Prefer the typed `RegisterXxx` request types. |
| `DocEprsc` | Doctor-type record. Used internally by prescription payloads. |

### Exception hierarchy

| Base | Subclasses |
|---|---|
| `Exception` | `AuthTokenNotSuppliedException`, `UserLoginException`, `PrescriptionNotCreatedException`, `MissingConfigException`, `InvalidConfigException` |
| `ArgumentException` | `MissingParamException` |
| `ConnectionError` | `Redirection`, `ClientError` → (`BadRequest`, `UnauthorizedAccess`, `ForbiddenAccess`, `ResourceNotFound`, `MethodNotAllowed`, `ResourceConflict`, `ResourceGone`, `ResourceInvalid`), `ServerError` |
| `Exception` (domain) | `AuthenticationError`, `AuthorizationError`, `IdentityError`, `EntitlementError`, `ValidationError`, `BusinessRuleError`, `DuplicateSubmissionRisk`, `TemporaryServiceError` |

### Internal (not for direct use)

| Type | Notes |
|---|---|
| `TaminApiClient` | Internal HTTP plumbing. Not part of the public contract. |
| `TaminEndpointClient` | Internal per-domain endpoint wrapper. Not part of the public contract. |
| `ResponseHandling` | Internal response-to-exception mapping. Not part of the public contract. |

---

## Limitations & compatibility notes

- **Target framework:** `net10.0` only. Earlier .NET versions are not supported.
- **Sandbox vs. production:** The default base URL is the sandbox endpoint (`https://ep-test.tamin.ir/api/`). Override `baseUri` / `TaminOptions.BaseUrl` for production (`https://ep.tamin.ir/api/`).
- **No IHttpClientFactory in manual construction:** When constructing `TaminSession` directly (not via DI), supply your own `HttpClient`. Lifetime and socket management are your responsibility.
- **`JsonElement` return type:** Domain-client methods return raw `JsonElement` to remain forward-compatible with API schema changes. Deserialize to the provided DTO types when strong typing is required.
- **Token lifetime:** The SDK does not automatically refresh tokens. Monitor `TokenResult.ExpiresIn` and call `RefreshTokenAsync` proactively.
- **`DuplicateSubmissionRisk`:** On network timeouts during prescription submission, **query the API for an existing prescription before retrying** to avoid double-submitting.
- **Thread safety:** `TaminSession` shares a single `HttpClient`. Concurrent calls are safe. Do not replace `HttpClient.DefaultRequestHeaders` from multiple threads.

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for prerequisites, code-style guidelines, and pull-request instructions.

```bash
# Run all tests
dotnet test EP.Tamin.NET.slnx
```

---

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

---

## License

[MIT](LICENSE) © EP.Tamin.NET Contributors

---

**Repository:** <https://github.com/mobinseven/EP.Tamin.NET>  
**Issues:** <https://github.com/mobinseven/EP.Tamin.NET/issues>

