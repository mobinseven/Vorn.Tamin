# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Full **EP-TAMIN-API.md** surface coverage across implemented domains:
  - Authentication: `RefreshTokenAsync`, `ValidateTokenAsync`, OTP and `provider_identifier` support in `CreateAsync`
  - Eligibility (`EligibilityClient`): `LookupPrivatePracticeAsync` for patient eligibility verification (deserve-info endpoint)
  - E-Prescription Writing (`PrescriptionClient`): `RegisterVisitPrescriptionAsync`, `RegisterDrugPrescriptionAsync`, `RegisterParaclinicPrescriptionAsync`, `RegisterMedicalServicePrescriptionAsync`, `RegisterReferralPrescriptionAsync`, `RegisterPhysiotherapyPrescriptionAsync`
  - Prescription Query: `GetRegisteredPrescriptionAsync`, `GetReferralPrescriptionAsync`, `GetPrescriptionListAsync`
  - Prescription Mutation: `EditElectronicPrescriptionAsync`, `DeleteElectronicPrescriptionAsync`
  - Reference Data (`ServiceClient`): `GetDrugListAsync`, `GetServiceListAsync`, `GetAllowedCountAsync`, `GetPriceAsync`
  - Warning Services: `CheckPrescriptionWarningAsync`
- Strongly-typed request/response DTOs (`Models.cs`) for all API domains
- `TaminResponse<T>` wrapper matching the standard API envelope
- `PrescriptionStatus` enum (Draft → Submitted → Accepted → … → Failed)
- Domain error exception types: `AuthenticationError`, `AuthorizationError`, `IdentityError`, `EntitlementError`, `ValidationError`, `BusinessRuleError`, `DuplicateSubmissionRisk`, `TemporaryServiceError`
- Missing SDK exceptions: `MissingParamException`, `MissingConfigException`, `InvalidConfigException`, `PrescriptionNotCreatedException`
- Common request headers: `Client-Id` (per-session), `Request-Id` (per-request UUID), `Correlation-Id` (optional, per-request)
- Dependency injection support: `AddTaminClient(IServiceCollection, Action<TaminOptions>)` extension and `TaminOptions` configuration class
- NuGet package metadata (`PackageId`, `Description`, `PackageTags`, `PackageLicenseExpression`, `GenerateDocumentationFile`, etc.)
- Solution-level `Directory.Build.props`, `global.json`, and `.editorconfig`
- GitHub Actions CI/CD workflow: build + test on every push/PR, pack + publish to NuGet.org on `v*` tags
- XML documentation comments on all public APIs

### Not Implemented
- **Identity Client**: `IdentityClient` remains an empty placeholder. Patient identity verification and eligibility checking are available through `EligibilityClient.LookupPrivatePracticeAsync()`, which uses the EP.Tamin "deserve-info" endpoint. No separate identity-specific endpoints exist in the current generated Kiota clients.
- **Pharmacy Dispensing**: `PharmacyClient` is a placeholder awaiting generated Kiota request builders for pharmacy operations.
- **Paraclinic Dispensing**: `ParaclinicClient` is a placeholder awaiting generated Kiota request builders for paraclinic operations.

## [0.1.0] - Initial Release

### Added
- `TaminSession` with bearer token and username/password login
- `ServiceClient`: `GetAllServicesAsync`, `GetPrescriptionTypeAsync`, `GetParaclinicTarefAsync`, `GetDrugAmountAsync`, `GetDrugInstructionAsync`
- `PrescriptionClient`: `CreatePrescriptionAsync<T>`, `GetPrescriptionDetailAsync`
- HTTP exception hierarchy mirroring the Python tamin-sdk
- `PrescriptionType` enum and `DocEprsc` type
