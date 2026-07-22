[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..'))
)

$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Split-TaminOpenApi.ps1') -RepositoryRoot $RepositoryRoot

$clients = @(
    @{ Name = 'Account' },
    @{ Name = 'Soa' },
    @{ Name = 'Api' }
)

foreach ($client in $clients) {
    foreach ($environment in @('Prod', 'Pilot')) {
        $input = Join-Path $RepositoryRoot ("openapi/tamin/{0}.{1}.yaml" -f $client.Name.ToLowerInvariant(), $environment.ToLowerInvariant())
        $output = Join-Path $RepositoryRoot ("src/Tamin/Tamin.Client.{0}/Generated/{1}" -f $client.Name, $environment)
        $namespace = "Tamin.Client.$($client.Name).$environment"
        $className = "$environment$($client.Name)Client"
        & dotnet tool run kiota -- generate --openapi $input --language CSharp --class-name $className --namespace-name $namespace --output $output --clean-output --exclude-backward-compatible
        if ($LASTEXITCODE -ne 0) { throw "Kiota generation failed for $environment $($client.Name)." }
    }
}
