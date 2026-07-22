[CmdletBinding()]
param([string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')))

$ErrorActionPreference = 'Stop'
$documents = Get-ChildItem -LiteralPath (Join-Path $RepositoryRoot 'openapi/tamin') -Filter '*.yaml' | Sort-Object Name
if ($documents.Count -ne 6) { throw "Expected six host-scoped OpenAPI documents; found $($documents.Count)." }

function Invoke-Checked([scriptblock]$Command, [string]$Failure) {
    & $Command
    if ($LASTEXITCODE -ne 0) { throw $Failure }
}

function Get-ContractSnapshot {
    $roots = @((Join-Path $RepositoryRoot 'openapi/tamin'), (Join-Path $RepositoryRoot 'src/Tamin'))
    $files = foreach ($root in $roots) {
        Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object { ($_.FullName -match '\\Generated\\|\\openapi\\tamin\\') -and $_.Name -ne '.kiota.log' } |
            ForEach-Object { [pscustomobject]@{ Path = $_.FullName.Substring($RepositoryRoot.Length).Replace('\','/'); Hash = Get-NormalizedTextHash $_.FullName } }
    }
    return ($files | Sort-Object Path | ConvertTo-Json -Compress)
}

function Get-NormalizedTextHash([string]$Path) {
    $text = [IO.File]::ReadAllText($Path).Replace("`r`n", "`n").Replace("`r", "`n")
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($text))).ToLowerInvariant()
}

$sourceHashes = @(
    (Get-NormalizedTextHash (Join-Path $RepositoryRoot 'src/tamin-production.openapi.yaml')),
    (Get-NormalizedTextHash (Join-Path $RepositoryRoot 'src/tamin-pilot.openapi.yaml'))
) -join "`n"
$actualFingerprint = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($sourceHashes))).ToLowerInvariant()
$expectedFingerprint = (Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'tamin-contract-source.sha256')).Trim()
$mappingTests = Get-Content -Raw -LiteralPath (Join-Path $RepositoryRoot 'src/Tamin/Tamin.Integration.Tests/PrescriptionRequestMapperTests.cs')
if ($actualFingerprint -ne $expectedFingerprint -or !$mappingTests.Contains($expectedFingerprint)) {
    throw 'Source OpenAPI changed without updating the contract fingerprint and D-05-D-11 request-mapping tests.'
}

$warningCounts = @{}
foreach ($document in $documents) {
    Invoke-Checked { npx --no-install swagger-cli validate $document.FullName } "swagger-cli validation failed for $($document.Name)."
    $lintText = (& npx --no-install redocly lint $document.FullName --format json 2>$null | Out-String)
    if ($LASTEXITCODE -ne 0) { throw "Redocly lint failed for $($document.Name)." }
    $lint = $lintText | ConvertFrom-Json
    foreach ($problem in $lint.problems) { $warningCounts[$problem.ruleId] = 1 + $warningCounts[$problem.ruleId] }
}
$expectedWarnings = @{ 'operation-4xx-response' = 80; 'info-license' = 6; 'no-ambiguous-paths' = 2 }
if (($warningCounts.Keys | Where-Object { !$expectedWarnings.ContainsKey($_) }).Count -ne 0) { throw "Unexpected Redocly warning category: $($warningCounts.Keys -join ', ')." }
foreach ($rule in $expectedWarnings.Keys) {
    if ($warningCounts[$rule] -ne $expectedWarnings[$rule]) { throw "Expected $($expectedWarnings[$rule]) $rule warnings; found $($warningCounts[$rule])." }
}

$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ("vorn-tamin-contracts-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
try {
    $operationsByEnvironment = @{ prod = @(); pilot = @() }
    $allPaths = [Collections.Generic.List[string]]::new()
    $allBundleText = [Text.StringBuilder]::new()
    foreach ($document in $documents) {
        $bundlePath = Join-Path $temporaryRoot ($document.BaseName + '.json')
        Invoke-Checked { npx --no-install redocly bundle $document.FullName --dereferenced --output $bundlePath } "Dereferenced bundle failed for $($document.Name)."
        $rawBundle = Get-Content -Raw -LiteralPath $bundlePath
        [void]$allBundleText.AppendLine($rawBundle)
        if ($rawBundle.Contains('"$ref"')) { throw "Internal reference remained in $($document.Name)." }
        $bundle = $rawBundle | ConvertFrom-Json -AsHashtable
        $environment = $document.BaseName.Split('.')[-1]
        foreach ($path in $bundle.paths.Keys) {
            $allPaths.Add($path)
            foreach ($method in @('get','post','put','patch','delete','options','head','trace')) {
                if (!$bundle.paths[$path].ContainsKey($method)) { continue }
                $operation = $bundle.paths[$path][$method]
                $operationsByEnvironment[$environment] += $operation.operationId
                $placeholders = [regex]::Matches($path, '\{([^}]+)\}') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
                $pathParameters = @($operation.parameters | Where-Object { $_.in -eq 'path' } | ForEach-Object { $_.name } | Sort-Object -Unique)
                if (($placeholders -join '|') -ne ($pathParameters -join '|')) { throw "Path parameter mismatch for $method $path in $($document.Name)." }
            }
        }
    }
    foreach ($environment in @('prod','pilot')) {
        $ids = @($operationsByEnvironment[$environment])
        if ($ids.Count -ne 40) { throw "Expected 40 $environment operations; found $($ids.Count)." }
        if (($ids | Sort-Object -Unique).Count -ne 40) { throw "$environment operation IDs are not unique." }
    }
    $routeText = $allPaths -join "`n"
    foreach ($literal in @('docNatioanlCode','/ep/api/v7/cartable-nurse/save','referentalPrescDetail','siamId','siam-id','siamid','docId','doc-id')) {
        if (!$routeText.Contains($literal)) { throw "Required D-15-D-18 route literal '$literal' is absent." }
    }
    if (!$allBundleText.ToString().Contains('docID')) { throw "Required D-18 field literal 'docID' is absent." }
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) { Remove-Item -LiteralPath $temporaryRoot -Recurse -Force }
}

$beforeGeneration = Get-ContractSnapshot
& (Join-Path $PSScriptRoot 'generate-tamin-clients.ps1') -RepositoryRoot $RepositoryRoot
if ($LASTEXITCODE -ne 0) { throw 'Canonical client generation failed.' }
$afterGeneration = Get-ContractSnapshot
if ($beforeGeneration -ne $afterGeneration) {
    $changedPaths = Compare-Object ($beforeGeneration | ConvertFrom-Json) ($afterGeneration | ConvertFrom-Json) -Property Path, Hash |
        Select-Object -ExpandProperty Path -Unique
    throw "Canonical generation changed split specs, generated code, or Kiota lock files: $($changedPaths -join ', ')."
}

Write-Host 'Tamin contract verification passed: 6 documents, 80/6/2 allowed warnings, 40 operations per environment, deterministic generation.'
