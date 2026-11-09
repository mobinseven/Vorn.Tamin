# Vorn.Tamin

[![NuGet Version](https://img.shields.io/nuget/v/Vorn.Tamin?style=flat-square&label=Vorn.Tamin)](https://www.nuget.org/packages/Vorn.Tamin)
[![NuGet Version](https://img.shields.io/nuget/v/Tamin.Client.Account?style=flat-square&label=Tamin.Client.Account)](https://www.nuget.org/packages/Tamin.Client.Account)
[![NuGet Version](https://img.shields.io/nuget/v/Tamin.Client.Api?style=flat-square&label=Tamin.Client.Api)](https://www.nuget.org/packages/Tamin.Client.Api)
[![NuGet Version](https://img.shields.io/nuget/v/Tamin.Client.Soa?style=flat-square&label=Tamin.Client.Soa)](https://www.nuget.org/packages/Tamin.Client.Soa)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)

> **Unofficial, internal client.** This library is not provided, endorsed or supported by the Social Security Insurance of Iran. It is built for our company's own use. Nursing operations are not supported yet, two pilot routes currently return 404, and nothing has been validated against the live service. See "Known limitations" in [CHANGELOG.md](CHANGELOG.md).

A .NET 10 client SDK for the EP.Tamin electronic prescription APIs of the Social Security Insurance of Iran (سازمان تأمین اجتماعی).

Clients are **generated from audited OpenAPI specifications** using [Kiota](https://github.com/microsoft/kiota), then wrapped by an environment-neutral client, `ITaminClient`, that provides every operation with authentication, HTTP resilience, and request/response mapping.

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                  Your Application                   │
├─────────────────────────────────────────────────────┤
│  Tamin.Integration                                  │
│  ├── ITaminClient / TaminClient  (operation facade) │
│  ├── Auth    (PKCE + refresh)                       │
│  ├── Http    (retry, failures, operation IDs)       │
│  └── Mapping (request + response mapping)           │
├──────────┬──────────┬───────────────────────────────┤
│ Account  │   SOA    │           API                 │
│ (Auth)   │ (Services│   (Clinical operations)       │
│          │          │                               │ 
│ Kiota    │  Kiota   │         Kiota                 │
│ generated│ generated│       generated               │
└──────────┴──────────┴───────────────────────────────┘
         Pilot + Production variants for each
```

Each service has **two environments** — `Pilot` and `Prod` — generated from separate OpenAPI documents.

---

## Prerequisites

- .NET 10 SDK
- Node.js (for `npm install` — OpenAPI validation tools)
- EP.Tamin credentials or a pre-obtained bearer token

---

## Installation

### NuGet (recommended)

```bash
dotnet add package Vorn.Tamin
```

This installs the integration layer and all three generated client libraries as transitive dependencies.

#### Individual packages

If you only need specific clients without the integration layer:

| Package | Description |
|---|---|
| `Vorn.Tamin` | `ITaminClient`, with auth, HTTP pipeline and request/response mapping. Depends on all three clients below. |
| `Tamin.Client.Account` | Kiota-generated OAuth/auth client (token exchange, PKCE, signout). |
| `Tamin.Client.Api` | Kiota-generated clinical API client (eprescription, referrals, notes, hospitalization, etc.). |
| `Tamin.Client.Soa` | Kiota-generated SOA client (auth, family doctor, patient disease, referral feedback). |

```bash
dotnet add package Tamin.Client.Account
dotnet add package Tamin.Client.Api
dotnet add package Tamin.Client.Soa
```

### PackageReference

```xml
<PackageReference Include="Vorn.Tamin" Version="*" />
```

### Source (project reference)

```xml
<ProjectReference Include="src/Tamin/Tamin.Integration/Tamin.Integration.csproj" />
```

---

## Quick start

### 1. Create the client

`ITaminClient` has one method per contract operation, and the same methods in both environments. Choosing an environment is the only difference in calling code. Hosts come from the OpenAPI documents.

```csharp
using Tamin.Integration;

// Keep one handler for the life of the application. You own it and dispose it.
var handler = new SocketsHttpHandler();
var options = new TaminClientOptions(
    ClientId: "YOUR_CLIENT_ID",     // The pilot contract allows only "portal-js".
    Audience: "YOUR_AUDIENCE",
    PrimaryHandler: handler,
    LoggerFactory: loggerFactory);  // Optional.

ITaminClient tamin = TaminClient.CreateProduction(options); // Or TaminClient.CreatePilot(options).
```

### 2. Authenticate with PKCE

```csharp
string codeVerifier = TaminClient.CreateCodeVerifier();
string state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

// Redirect the doctor's browser here.
Uri login = tamin.CreateAuthorizationUri(redirectUri, TaminClient.CreateCodeChallenge(codeVerifier), state);

// In the callback, after checking state:
await tamin.CompleteAuthorizationAsync(code, redirectUri, codeVerifier);
```

The token is held in memory and refreshed automatically. If the refresh token is missing or rejected, calls throw `TaminReauthorizationRequiredException` and the doctor must log in again. A network failure during refresh surfaces as `HttpRequestException` and keeps the refresh token, so a later call can try again.

### 3. Call operations

```csharp
var cartable = await tamin.GetReferralCartableAsync(docNationalCode, patientNationalCode, trackingCode);
// cartable.Data.Items, cartable.Status, cartable.TraceId

var created = await tamin.CreatePrescriptionAsync(new PrescriptionCreateInput(/* ... */));
// created.HeaderId, created.TrackingCode, created.ErrorCode
```

Method names follow the contract's `operationId`: `getReferralCartable` is `GetReferralCartableAsync`.

Parameters that only the pilot route carries, such as `docNationalCode` on `DeletePrescriptionAsync`, are required in both environments so that calling code does not change. Production validates them and does not send them.

### 4. Read responses without a schema

The guide defines no response schema for 29 operations, so they return `UnconstrainedResponse`. Its `Payload` is the response body as a `JsonElement`, and its `ValueKind` is `Undefined` if the body was empty.

```csharp
var complaints = await tamin.ListComplaintsAsync();
foreach (var item in complaints.Payload.EnumerateArray()) { /* ... */ }
```

---

## HTTP pipeline

1. **Retries**: GET requests are retried up to 2 times on HTTP 5xx or network errors. Mutations are **never** retried, because the provider contract documents no idempotency key.

2. **Failures**: non-success responses throw `TaminApiException` with the status code, operation ID and raw body. Logs contain only the operation ID and status. Bodies and URLs are never logged, because they carry national codes and patient data.

3. **Authentication**: Account operations are anonymous. SOA and API operations send the doctor's bearer token, only to the hosts of those services.

---

## Contract verification

The repository includes a verification script that ensures generated code matches the source OpenAPI specs:

```bash
npm install
./scripts/verify-tamin-contracts.ps1
```

This validates:
- 6 OpenAPI documents (3 services x 2 environments)
- The routes of all 94 operations match `openapi/tamin-route-manifest.json`, which is transcribed by hand from the Tamin guide v1.10.1
- The guide's sample payloads in `tests/tamin-contract-fixtures` validate against the operation schemas (known conflicts are listed in `expected-failures.json`)
- Redocly lint warnings match expected counts
- 47 operations per environment
- Required route literals (e.g. `docNatioanlCode`, `siamId`)
- Deterministic regeneration (generated code is identical before/after)

The conformance checks also run on their own with `./scripts/verify-tamin-conformance.ps1`.

---

## Regenerating clients

To regenerate Kiota clients from updated OpenAPI specs:

```bash
./scripts/generate-tamin-clients.ps1
```

This splits the source OpenAPI docs, then runs Kiota for each service/environment combination.

---

## Project structure

```
openapi/tamin/                    # Split OpenAPI specs (pilot + prod)
├── account.{pilot,prod}.yaml
├── api.{pilot,prod}.yaml
└── soa.{pilot,prod}.yaml
openapi/tamin-route-manifest.json # Routes transcribed from the guide, checked against the specs

tests/tamin-contract-fixtures/    # Guide sample payloads + known conflicts

src/tamin-{pilot,production}.openapi.yaml  # Source OpenAPI specs

src/Tamin/
├── Tamin.Client.Account/         # Generated auth client (Kiota)
│   └── Generated/{Pilot,Prod}/
├── Tamin.Client.Api/             # Generated clinical API client (Kiota)
│   └── Generated/{Pilot,Prod}/
├── Tamin.Client.Soa/             # Generated SOA client (Kiota)
│   └── Generated/{Pilot,Prod}/
├── Tamin.Integration/            # Integration layer
│   ├── ITaminClient.cs           # Public operations
│   ├── TaminClient.cs            # Creates a client for an environment
│   ├── {Production,Pilot}TaminClient.cs  # Operations on each generated client
│   ├── Auth/                     # Token provider, PKCE, OAuth exchange
│   ├── Http/                     # Operation sending, failures, retry
│   └── Mapping/                  # Inputs, DTOs, request and response mapping
└── Tamin.Integration.Tests/      # Tests

scripts/
├── Split-TaminOpenApi.ps1        # Splits source specs into per-service docs
├── generate-tamin-clients.ps1    # Runs Kiota code generation
├── verify-tamin-conformance.ps1  # Route manifest + sample payload checks
├── tamin-contract-checks/        # Node checks used by the conformance script
└── verify-tamin-contracts.ps1    # Validates specs + conformance + determinism
```

---

## Error handling

| Exception | Meaning |
|---|---|
| `TaminApiException` | Non-success HTTP response, or a success whose body is empty or unreadable (`StatusCode` is null). Inspect `StatusCode`, `OperationId`, `RawBody`. `RawBody` can hold personal data. |
| `TaminReauthorizationRequiredException` | Token refresh failed or no refresh token available. User must re-authorize via PKCE. |
| `TaminRequestValidationException` | Request field failed validation before the HTTP call was made. A blank route value throws `ArgumentException`. |

---

## License

[MIT](LICENSE) © Vorn.Tamin Contributors

---

**Repository:** <https://github.com/mobinseven/Vorn.Tamin>
**Issues:** <https://github.com/mobinseven/Vorn.Tamin/issues>
