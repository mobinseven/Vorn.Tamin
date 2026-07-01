# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- Removed public placeholder client surfaces that were not backed by `docs/EP-TAMIN-API.md` or generated Kiota paths.
- Removed `TaminSession.Identity`, `TaminSession.Pharmacy`, and `TaminSession.Paraclinic` because their standalone workflows are not implemented from the provider contract.
- Updated implementation-status documentation to describe only contract-backed public SDK surfaces.

### Fixed
- Corrected stale documentation and changelog claims for non-existent identity, pharmacy, paraclinic, pricing, allowed-count, and entitlement helper APIs.

## [0.1.0] - Initial Release

### Added
- `TaminSession` with bearer token and username/password login.
- `ServiceClient`: `GetAllServicesAsync`, `GetPrescriptionTypeAsync`, `GetParaclinicTarefAsync`, `GetDrugAmountAsync`, `GetDrugInstructionAsync`.
- `PrescriptionClient`: `CreatePrescriptionAsync<T>`, `GetPrescriptionDetailAsync`.
- HTTP exception hierarchy mirroring the Python tamin-sdk.
- `PrescriptionType` enum and `DocEprsc` type.
