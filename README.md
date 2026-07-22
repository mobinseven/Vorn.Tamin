# Vorn.Tamin

[![NuGet Version](https://img.shields.io/nuget/v/Vorn.Tamin?style=flat-square&label=Vorn.Tamin)](https://www.nuget.org/packages/Vorn.Tamin)
[![NuGet Version](https://img.shields.io/nuget/v/Tamin.Client.Account?style=flat-square&label=Tamin.Client.Account)](https://www.nuget.org/packages/Tamin.Client.Account)
[![NuGet Version](https://img.shields.io/nuget/v/Tamin.Client.Api?style=flat-square&label=Tamin.Client.Api)](https://www.nuget.org/packages/Tamin.Client.Api)
[![NuGet Version](https://img.shields.io/nuget/v/Tamin.Client.Soa?style=flat-square&label=Tamin.Client.Soa)](https://www.nuget.org/packages/Tamin.Client.Soa)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)

A .NET 10 client SDK for the EP.Tamin electronic prescription APIs of the Social Security Insurance of Iran (سازمان تأمین اجتماعی).

Clients are **generated from audited OpenAPI specifications** using [Kiota](https://github.com/microsoft/kiota), then wrapped by an integration layer that provides authentication, HTTP resilience, and request/response mapping.

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                  Your Application                   │
├─────────────────────────────────────────────────────┤
│  Tamin.Integration                                  │
│  ├── Auth / TaminTokenProvider   (PKCE + refresh)   │
│  ├── Http  / TaminClientFactory  (pipeline wiring)  │
│  ├── Http  / TaminResponseHandler + Retry           │
│  └── Mapping / PrescriptionRequestMapper            │
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
| `Vorn.Tamin` | Integration layer — auth, HTTP pipeline, request/response mapping. Depends on all three clients below. |
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

### 1. Create clients

```csharp
using Tamin.Integration.Auth;
using Tamin.Integration.Http;

// Base URLs for each service.
var bases = new TaminClientBases(
    Account: new Uri("https://auth.tamin.ir/"),
    Soa:     new Uri("https://soa.tamin.ir/"),
    Api:     new Uri("https://api.tamin.ir/")
);

// Token provider handles PKCE and auto-refresh.
var tokenProvider = new TaminTokenProvider(
    exchange: new TaminDoctorTokenExchange(httpClient, new Uri("https://auth.tamin.ir/auth/server/v2/token"), isPilot: false),
    clientId: "YOUR_CLIENT_ID",
    audience: "YOUR_AUDIENCE",
    allowedHosts: new[] { "auth.tamin.ir", "soa.tamin.ir", "api.tamin.ir" }
);

// Wire up the HTTP pipeline and create all three clients.
var clients = TaminClientFactory.CreateProduction(
    bases,
    tokenProvider,
    primaryHandler: new HttpClientHandler(),
    loggerFactory:  LoggerFactory.Create(b => b.AddConsole()),
    operationIdResolver: req => req.RequestUri?.AbsolutePath ?? "unknown"
);

// clients.Account  — Tamin.Client.Account.Prod.ProdAccountClient
// clients.Soa      — Tamin.Client.Soa.Prod.ProdSoaClient
// clients.Api      — Tamin.Client.Api.Prod.ProdApiClient
```

For the pilot environment, use `TaminClientFactory.CreatePilot(...)` which returns `PilotTaminClients`.

### 2. Authenticate with PKCE

```csharp
using Tamin.Integration.Auth;

// Generate PKCE verifier and state before redirecting.
string codeVerifier = TaminTokenProvider.CreateCodeVerifier();
string state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

// Build the authorization URL and redirect the user there.
// After callback, exchange the code:
await tokenProvider.CompleteAuthorizationAsync(
    code: "AUTHORIZATION_CODE",
    redirectUri: new Uri("https://your-app.example/callback"),
    codeVerifier: codeVerifier
);

// Token is now cached. All subsequent Kiota requests include the Bearer token.
```

If the refresh token is expired or missing, `TaminReauthorizationRequiredException` is thrown — the user must re-authorize.

### 3. Make API calls

All generated clients use Kiota's fluent request builder pattern:

```csharp
// Example: fetch reference data via the SOA client
var services = await clients.Soa.Interface.Epresc.SendEpresc.V2.Services
    .GetAsync();

// Example: fetch patient referrals via the API client
var referrals = await clients.Api.V2.Referral
    .Count
    .WithNationalCodeItem("{patientNationalCode}")
    .GetAsync();
```

### 4. Map responses to domain DTOs

`TaminMappingService` converts generated Kiota responses into stable DTOs:

```csharp
var mapper = new TaminMappingService();

// Structured response
var result = mapper.MapCreatePrescription(response);
// result.HeaderId, result.TrackingCode, result.ErrorCode, etc.

// Undocumented / opaque responses remain as JsonElement
var raw = mapper.MapPrescriptionTypes(jsonResponse);
// raw.Payload — JsonElement you can inspect freely
```

### 5. Map domain inputs to Kiota requests

`PrescriptionRequestMapper` maps your domain input records to generated Kiota request objects:

```csharp
var requestMapper = new PrescriptionRequestMapper();

var input = new PrescriptionCreateInput(
    Mobile: "09123456789",
    DocNationalCode: "0012345678",
    Patient: "Patient Name",
    PrescDate: "14030601",
    PrescType: new PrescriptionTypeInput(PrescTypeId: 1),
    NoteDetailEprscs: [/* prescription items */]
);

// For production:
ProdModels.PrescriptionCreateRequest prodRequest = requestMapper.MapProduction(input);

// For pilot:
PilotModels.PrescriptionCreateRequest pilotRequest = requestMapper.MapPilot(input);
```

---

## HTTP pipeline

`TaminClientFactory` wraps each client's HTTP handler with:

1. **`TaminTransientFaultHandler`** — retries GET requests up to 2 times on HTTP 5xx or network errors (Polly). Mutations are **not** retried since the provider contract does not document idempotency keys.

2. **`TaminResponseHandler`** — non-success responses throw `TaminApiException` with the status code, operation ID, and raw body. Logs redact credential fields (`access_token`, `refresh_token`, `code`, `code_verifier`).

3. **Authentication** — Account endpoints use anonymous access; SOA and API endpoints use `BaseBearerTokenAuthenticationProvider` backed by `TaminTokenProvider`.

---

## Contract verification

The repository includes a verification script that ensures generated code matches the source OpenAPI specs:

```bash
npm install
./scripts/verify-tamin-contracts.ps1
```

This validates:
- 6 OpenAPI documents (3 services x 2 environments)
- Redocly lint warnings match expected counts
- 40 operations per environment
- Required route literals (e.g. `docNatioanlCode`, `siamId`)
- Deterministic regeneration (generated code is identical before/after)

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

src/tamin-{pilot,production}.openapi.yaml  # Source OpenAPI specs

src/Tamin/
├── Tamin.Client.Account/         # Generated auth client (Kiota)
│   └── Generated/{Pilot,Prod}/
├── Tamin.Client.Api/             # Generated clinical API client (Kiota)
│   └── Generated/{Pilot,Prod}/
├── Tamin.Client.Soa/             # Generated SOA client (Kiota)
│   └── Generated/{Pilot,Prod}/
├── Tamin.Integration/            # Integration layer
│   ├── Auth/                     # Token provider, PKCE, OAuth exchange
│   ├── Http/                     # Client factory, response handler, retry
│   └── Mapping/                  # Request mapper, response mapper, DTOs
└── Tamin.Integration.Tests/      # Tests

scripts/
├── Split-TaminOpenApi.ps1        # Splits source specs into per-service docs
├── generate-tamin-clients.ps1    # Runs Kiota code generation
└── verify-tamin-contracts.ps1    # Validates specs + determinism
```

---

## Error handling

| Exception | Meaning |
|---|---|
| `TaminApiException` | Non-success HTTP response from the Tamin API. Inspect `StatusCode`, `OperationId`, `RawBody`. |
| `TaminReauthorizationRequiredException` | Token refresh failed or no refresh token available. User must re-authorize via PKCE. |
| `TaminRequestValidationException` | Request field failed validation before the HTTP call was made. |

---

## License

[MIT](LICENSE) © Vorn.Tamin Contributors

---

**Repository:** <https://github.com/mobinseven/Vorn.Tamin>
**Issues:** <https://github.com/mobinseven/Vorn.Tamin/issues>
