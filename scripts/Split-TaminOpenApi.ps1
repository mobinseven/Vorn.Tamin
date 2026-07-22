[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..'))
)

$ErrorActionPreference = 'Stop'
$openApiRoot = Join-Path $RepositoryRoot 'openapi/tamin'
New-Item -ItemType Directory -Force -Path $openApiRoot | Out-Null

function Split-TaminOpenApiDocument {
    param(
        [Parameter(Mandatory)] [string]$InputPath,
        [Parameter(Mandatory)] [string]$Environment,
        [Parameter(Mandatory)] [hashtable]$Servers
    )

    $source = (Get-Content -Raw -LiteralPath $InputPath).Replace("`r`n", "`n").Replace("`r", "`n")
    $pathsMarker = "paths:`n"
    $componentsMarker = "components:`n"
    $pathsStart = $source.IndexOf($pathsMarker, [StringComparison]::Ordinal)
    $componentsStart = $source.IndexOf($componentsMarker, [StringComparison]::Ordinal)
    if ($pathsStart -lt 0 -or $componentsStart -lt 0) {
        throw "Expected paths and components sections in $InputPath."
    }

    $header = $source.Substring(0, $pathsStart)
    $header = [regex]::Replace($header, '(?ms)^servers:\r?\n.*?(?=^security:)', '')
    $components = $source.Substring($componentsStart)
    $pathsContent = $source.Substring($pathsStart + $pathsMarker.Length, $componentsStart - ($pathsStart + $pathsMarker.Length))
    $blocks = [regex]::Matches($pathsContent, '(?ms)^  /.*?(?=^  /|\z)')
    $byClient = @{ Account = [System.Collections.Generic.List[string]]::new(); Soa = [System.Collections.Generic.List[string]]::new(); Api = [System.Collections.Generic.List[string]]::new() }

    foreach ($match in $blocks) {
        $block = $match.Value
        $client = 'Soa'
        if ($block -match '(?m)^      servers:\r?\n        - url: https://account') { $client = 'Account' }
        elseif ($block -match '(?m)^      servers:\r?\n        - url: https://api\.tamin\.ir') { $client = 'Api' }
        elseif ($Environment -eq 'pilot' -and $block -match '(?m)^      servers:\r?\n        - url: https://ep-test\.tamin\.ir') { $client = 'Api' }

        # The document is now host-scoped; a redundant per-operation server would obscure the owner.
        $block = [regex]::Replace($block, '(?m)^      servers:\r?\n        - url: https://[^\r\n]+\r?\n', '')
        $byClient[$client].Add($block.TrimEnd("`r", "`n"))
    }

    foreach ($client in @('Account', 'Soa', 'Api')) {
        $server = $Servers[$client]
        $output = $header + "servers:`n  - url: $server`n" + "paths:`n" + (($byClient[$client] -join "`n") + "`n") + $components
        $outputPath = Join-Path $openApiRoot ("{0}.{1}.yaml" -f $client.ToLowerInvariant(), $Environment)
        [System.IO.File]::WriteAllText($outputPath, $output.Replace("`r`n", "`n"), [System.Text.UTF8Encoding]::new($false))
        $prunedPath = "$outputPath.pruned.yaml"
        & npx --no-install redocly bundle $outputPath --remove-unused-components --output $prunedPath
        if ($LASTEXITCODE -ne 0) { throw "Redocly pruning failed for $outputPath." }
        Move-Item -Force -LiteralPath $prunedPath -Destination $outputPath
    }
}

Split-TaminOpenApiDocument -InputPath (Join-Path $RepositoryRoot 'src/tamin-production.openapi.yaml') -Environment 'prod' -Servers @{
    Account = 'https://account.tamin.ir'; Soa = 'https://soa.tamin.ir'; Api = 'https://api.tamin.ir'
}
Split-TaminOpenApiDocument -InputPath (Join-Path $RepositoryRoot 'src/tamin-pilot.openapi.yaml') -Environment 'pilot' -Servers @{
    Account = 'https://account-pilot.tamin.ir'; Soa = 'https://ep-test.tamin.ir'; Api = 'https://ep-test.tamin.ir'
}
