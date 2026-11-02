[CmdletBinding()]
param([string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')))

# Contract conformance against the Tamin e-prescription guide v1.10.1:
#   route-check.mjs   - openapi/tamin-route-manifest.json (hand-derived from the PDF) must match the six host-scoped specs exactly.
#   payload-check.mjs - PDF sample payloads in tests/tamin-contract-fixtures must validate against the dereferenced operation schemas,
#                       except for the conflicts listed in expected-failures.json.
# Runs on its own; scripts/verify-tamin-contracts.ps1 calls it. Needs only node and the installed npm tools (npm ci).
$ErrorActionPreference = 'Stop'
$checks = Join-Path $PSScriptRoot 'tamin-contract-checks'
$failed = @()
foreach ($check in @('route-check.mjs', 'payload-check.mjs')) {
    & node (Join-Path $checks $check) --root $RepositoryRoot
    if ($LASTEXITCODE -ne 0) { $failed += $check }
}
if ($failed.Count -gt 0) { throw "Tamin contract conformance failed: $($failed -join ', ')." }
Write-Host 'Tamin contract conformance passed: route manifest and PDF payload fixtures.'
