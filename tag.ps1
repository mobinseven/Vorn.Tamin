param(
    [string]$TagName = "v1.0.0",
    [switch]$Force,
    [switch]$Push,
    [string]$SolutionPath = (Get-Location).Path
)

$ErrorActionPreference = "Stop"

$solutionDir = (Resolve-Path -LiteralPath $SolutionPath).ProviderPath
Write-Host "Scanning solution folder: $solutionDir"

function Invoke-Git {
    param(
        [Parameter(Mandatory)]
        [string]$Repo,

        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    $output = & git -C $Repo @Arguments 2>&1

    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed in '$Repo':`n$($output -join "`n")"
    }

    return $output
}

function Get-GitRoot {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    $root = & git -C $Path rev-parse --show-toplevel 2>$null

    if ($LASTEXITCODE -eq 0 -and $root) {
        return (Resolve-Path -LiteralPath $root.Trim()).ProviderPath
    }

    return $null
}

# Find projects only under the current solution folder.
$projectFiles = Get-ChildItem -Path $solutionDir -Recurse -File |
    Where-Object {
        $_.Extension -in ".csproj", ".fsproj", ".vbproj" -and
        $_.FullName -notmatch '[\\/](bin|obj|\.git)[\\/]'
    }

if (-not $projectFiles) {
    Write-Error "No project files found under '$solutionDir'."
    exit 1
}

# Resolve distinct Git repos that contain those projects.
$repos = foreach ($project in $projectFiles) {
    $repo = Get-GitRoot -Path $project.DirectoryName

    if ($repo) {
        $repo
    } else {
        Write-Warning "Project is not inside a Git repo: $($project.FullName)"
    }
}

$repos = $repos | Sort-Object -Unique

if (-not $repos) {
    Write-Error "No Git repositories found for projects under '$solutionDir'."
    exit 1
}

Write-Host "Discovered repositories:"
$repos | ForEach-Object { Write-Host " - $_" }

$summary = @()

foreach ($repo in $repos) {
    $repoName = Split-Path -Leaf $repo

    $result = [ordered]@{
        Repository = $repoName
        Path        = $repo
        TagCreated  = $false
        TagPushed   = $false
        Notes       = ""
    }

    Write-Host ""
    Write-Host "----------------------------------------"
    Write-Host "Repo: $repo"

    try {
        $branch = (Invoke-Git -Repo $repo -Arguments @("rev-parse", "--abbrev-ref", "HEAD") | Select-Object -First 1).Trim()

        if ($branch -ne "main" -and -not $Force) {
            Write-Warning "Current branch is '$branch', not 'main'. Use -Force to continue."
            $result.Notes = "Skipped: branch '$branch'"
            continue
        }

        $status = Invoke-Git -Repo $repo -Arguments @("status", "--porcelain")

        if (@($status).Count -gt 0 -and -not $Force) {
            Write-Warning "Working tree is not clean. Use -Force to continue."
            $result.Notes = "Skipped: working tree not clean"
            continue
        }

        $hasOrigin = $false

        try {
            Invoke-Git -Repo $repo -Arguments @("remote", "get-url", "origin") | Out-Null
            $hasOrigin = $true
        } catch {
            Write-Warning "No usable 'origin' remote found."
            if ($Push) {
                $result.Notes = "Skipped: cannot push without origin"
                continue
            }
        }

        if ($hasOrigin) {
            try {
                Invoke-Git -Repo $repo -Arguments @("fetch", "--tags", "origin") | Out-Null
            } catch {
                if (-not $Force) {
                    Write-Warning "Failed to fetch tags from origin. Use -Force to continue anyway."
                    $result.Notes = "Skipped: fetch tags failed"
                    continue
                }

                Write-Warning "Failed to fetch tags from origin, continuing because -Force was used."
            }

            $remoteTag = & git -C $repo ls-remote --tags --refs origin "refs/tags/$TagName" 2>$null

            if ($LASTEXITCODE -eq 0 -and $remoteTag) {
                Write-Host "Tag '$TagName' already exists on origin. Skipping."
                $result.Notes = "Skipped: tag exists on origin"
                continue
            }
        }

        $localTags = Invoke-Git -Repo $repo -Arguments @("tag", "-l", $TagName)
        $localTagExists = @($localTags) -contains $TagName

        if ($localTagExists) {
            Write-Host "Tag '$TagName' already exists locally."
        } else {
            Invoke-Git -Repo $repo -Arguments @("tag", "-a", $TagName, "-m", "Release $TagName") | Out-Null
            Write-Host "Created tag '$TagName'."
            $result.TagCreated = $true
        }

        if ($Push) {
            Invoke-Git -Repo $repo -Arguments @("push", "origin", $TagName) | Out-Null
            Write-Host "Pushed tag '$TagName' to origin."
            $result.TagPushed = $true
        }
    } catch {
        Write-Error "Error processing '$repo': $($_.Exception.Message)"
        $result.Notes = "Error: $($_.Exception.Message)"
    } finally {
        $summary += [pscustomobject]$result
    }
}

Write-Host ""
Write-Host "===== Run Summary ====="
$summary | Format-Table Repository, TagCreated, TagPushed, Notes -AutoSize
