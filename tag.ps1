param(
    [string]$TagName = "v1.0.0",
    [switch]$Force,

    # Pack NuGet packages for project files under the current solution folder.
    [switch]$Pack,

    # Push NuGet packages after packing.
    # Tags are always pushed to origin, matching your original script behavior.
    [switch]$Push,

    [string]$PackageSource = "https://api.nuget.org/v3/index.json",
    [string]$SolutionPath = (Get-Location).Path,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

# ----------------------------------------
# Helpers
# ----------------------------------------

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,

        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [Parameter(Mandatory)]
        [string]$WorkingDirectory,

        [switch]$ContinueOnError
    )

    # Do not use 2>&1 here.
    # Git often writes normal progress/status text to stderr.
    $oldErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"

    try {
        Push-Location $WorkingDirectory

        try {
            $output = & $FilePath @Arguments
            $exitCode = $LASTEXITCODE
        }
        finally {
            Pop-Location
        }
    }
    finally {
        $ErrorActionPreference = $oldErrorActionPreference
    }

    if ($exitCode -ne 0 -and -not $ContinueOnError) {
        throw "$FilePath $($Arguments -join ' ') failed in '$WorkingDirectory' with exit code $exitCode"
    }

    return [pscustomobject]@{
        ExitCode = $exitCode
        Output   = @($output)
    }
}

function Invoke-Git {
    param(
        [Parameter(Mandatory)]
        [string]$Repo,

        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [switch]$ContinueOnError
    )

    # Build the full argument array first.
    # Do not write this inline as:
    # -Arguments @("-C", $Repo) + $Arguments
    # because PowerShell can treat + as a positional argument.
    $gitArguments = @("-C", $Repo) + $Arguments

    return Invoke-NativeCommand `
        -FilePath "git" `
        -Arguments $gitArguments `
        -WorkingDirectory $Repo `
        -ContinueOnError:$ContinueOnError
}

function Test-CommandAvailable {
    param(
        [Parameter(Mandatory)]
        [string]$Command
    )

    $oldErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"

    try {
        & $Command --version 1>$null 2>$null
        return ($LASTEXITCODE -eq 0)
    }
    finally {
        $ErrorActionPreference = $oldErrorActionPreference
    }
}

function Get-GitRoot {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    $oldErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"

    try {
        $root = & git -C $Path rev-parse --show-toplevel 2>$null

        if ($LASTEXITCODE -eq 0 -and $root) {
            return (Resolve-Path -LiteralPath $root.Trim()).ProviderPath
        }

        return $null
    }
    finally {
        $ErrorActionPreference = $oldErrorActionPreference
    }
}

function Test-OriginExists {
    param(
        [Parameter(Mandatory)]
        [string]$Repo
    )

    $oldErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"

    try {
        & git -C $Repo remote get-url origin 1>$null 2>$null
        return ($LASTEXITCODE -eq 0)
    }
    finally {
        $ErrorActionPreference = $oldErrorActionPreference
    }
}

function Test-RemoteTagExists {
    param(
        [Parameter(Mandatory)]
        [string]$Repo,

        [Parameter(Mandatory)]
        [string]$TagName
    )

    $oldErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"

    try {
        $remoteTag = & git -C $Repo ls-remote --tags --refs origin "refs/tags/$TagName" 2>$null

        if ($LASTEXITCODE -ne 0) {
            return $false
        }

        return [bool]$remoteTag
    }
    finally {
        $ErrorActionPreference = $oldErrorActionPreference
    }
}

function Add-Note {
    param(
        [Parameter(Mandatory)]
        [hashtable]$Entry,

        [Parameter(Mandatory)]
        [string]$Note
    )

    if ($Entry.Notes) {
        $Entry.Notes = "$($Entry.Notes); $Note"
    }
    else {
        $Entry.Notes = $Note
    }
}

# ----------------------------------------
# Start
# ----------------------------------------

if (-not (Test-CommandAvailable -Command "git")) {
    Write-Error "Git was not found. Install Git or ensure it is available on PATH."
    exit 1
}

if (($Pack -or $Push) -and -not (Test-CommandAvailable -Command "dotnet")) {
    Write-Error ".NET SDK was not found. Install the .NET SDK or ensure dotnet is available on PATH."
    exit 1
}

if ($Push) {
    # Pushing packages requires packages to exist.
    $Pack = $true
}

try {
    $solutionDir = (Resolve-Path -LiteralPath $SolutionPath).ProviderPath
}
catch {
    Write-Error "Solution path does not exist: $SolutionPath"
    exit 1
}

$version = $TagName -replace '^[vV]', ''

Write-Host "Scanning solution folder: $solutionDir"
Write-Host "Tag: $TagName"
Write-Host "Package version: $version"
Write-Host "Force: $Force"
Write-Host "Pack: $Pack"
Write-Host "Push packages: $Push"

# Find project files only under the current solution folder.
$projectFiles = Get-ChildItem -Path $solutionDir -Recurse -File -Include *.csproj, *.fsproj, *.vbproj |
    Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj|\.git|\.vs|node_modules)[\\/]'
    }

if (-not $projectFiles -or $projectFiles.Count -eq 0) {
    Write-Error "No project files found under '$solutionDir'."
    exit 1
}

Write-Host "Found $($projectFiles.Count) project file(s)."

# Attach repo root to each project.
$projectItems = foreach ($project in $projectFiles) {
    $repo = Get-GitRoot -Path $project.DirectoryName

    if ($repo) {
        [pscustomobject]@{
            ProjectPath = $project.FullName
            Repo        = $repo
        }
    }
    else {
        Write-Warning "Project is not inside a Git repo: $($project.FullName)"
    }
}

$repos = $projectItems.Repo | Sort-Object -Unique

if (-not $repos -or $repos.Count -eq 0) {
    Write-Error "No Git repositories found for projects under '$solutionDir'."
    exit 1
}

Write-Host ""
Write-Host "Discovered repositories:"
$repos | ForEach-Object { Write-Host " - $_" }

$summary = @()

foreach ($repo in $repos) {
    $repoName = Split-Path -Leaf $repo
    $repoProjects = @($projectItems | Where-Object { $_.Repo -eq $repo } | Select-Object -ExpandProperty ProjectPath)

    $entry = [ordered]@{
        Repository      = $repoName
        Path            = $repo
        TagCreated      = $false
        TagPushed       = $false
        PackedProjects  = 0
        CreatedPackages = 0
        PushedPackages  = 0
        Skipped         = $false
        Notes           = ""
    }

    Write-Host ""
    Write-Host "----------------------------------------"
    Write-Host "Repo: $repo"

    try {
        $branchResult = Invoke-Git -Repo $repo -Arguments @("rev-parse", "--abbrev-ref", "HEAD") -ContinueOnError:$Force

        if ($branchResult.ExitCode -ne 0) {
            Add-Note -Entry $entry -Note "Could not read branch"

            if (-not $Force) {
                $entry.Skipped = $true
                continue
            }
        }
        else {
            $branch = ($branchResult.Output | Select-Object -First 1).Trim()

            if ($branch -ne "main") {
                if ($Force) {
                    Write-Warning "Current branch is '$branch', not 'main'. Continuing because -Force was used."
                    Add-Note -Entry $entry -Note "Forced on branch '$branch'"
                }
                else {
                    Write-Warning "Current branch is '$branch', not 'main'. Use -Force to continue."
                    $entry.Skipped = $true
                    Add-Note -Entry $entry -Note "Wrong branch: $branch"
                    continue
                }
            }
        }

        $statusResult = Invoke-Git -Repo $repo -Arguments @("status", "--porcelain") -ContinueOnError:$Force

        if ($statusResult.ExitCode -ne 0) {
            Add-Note -Entry $entry -Note "Could not read status"

            if (-not $Force) {
                $entry.Skipped = $true
                continue
            }
        }
        elseif (@($statusResult.Output).Count -gt 0) {
            if ($Force) {
                Write-Warning "Working tree is not clean. Continuing because -Force was used."
                Add-Note -Entry $entry -Note "Forced with dirty working tree"
            }
            else {
                Write-Warning "Working tree is not clean. Use -Force to continue."
                $entry.Skipped = $true
                Add-Note -Entry $entry -Note "Working tree not clean"
                continue
            }
        }

        $hasOrigin = Test-OriginExists -Repo $repo

        if (-not $hasOrigin) {
            Write-Warning "No 'origin' remote found."
            Add-Note -Entry $entry -Note "No origin remote"

            if (-not $Force) {
                $entry.Skipped = $true
                continue
            }
        }

        if ($hasOrigin) {
            $fetchArgs = if ($Force) {
                @("fetch", "--tags", "--force", "origin")
            }
            else {
                @("fetch", "--tags", "origin")
            }

            $fetchResult = Invoke-Git -Repo $repo -Arguments $fetchArgs -ContinueOnError:$Force

            if ($fetchResult.ExitCode -ne 0) {
                Write-Warning "Failed to fetch tags from origin."

                Add-Note -Entry $entry -Note "Fetch tags failed"

                if (-not $Force) {
                    $entry.Skipped = $true
                    continue
                }
            }

            $remoteTagExists = Test-RemoteTagExists -Repo $repo -TagName $TagName

            if ($remoteTagExists -and -not $Force) {
                Write-Host "Tag '$TagName' already exists on origin. Skipping. Use -Force to overwrite it."
                $entry.Skipped = $true
                Add-Note -Entry $entry -Note "Tag exists on origin"
                continue
            }

            if ($remoteTagExists -and $Force) {
                Write-Warning "Tag '$TagName' already exists on origin. It will be force-updated."
                Add-Note -Entry $entry -Note "Remote tag force-updated"
            }
        }

        $localTagResult = Invoke-Git -Repo $repo -Arguments @("tag", "-l", $TagName) -ContinueOnError:$Force
        $localTagExists = $localTagResult.Output -contains $TagName

        if ($localTagExists -and -not $Force) {
            Write-Host "Tag '$TagName' already exists locally. Skipping local tag creation."
        }
        else {
            $tagArgs = if ($Force) {
                @("tag", "-f", "-a", $TagName, "-m", "Release $TagName")
            }
            else {
                @("tag", "-a", $TagName, "-m", "Release $TagName")
            }

            $tagResult = Invoke-Git -Repo $repo -Arguments $tagArgs -ContinueOnError:$Force

            if ($tagResult.ExitCode -eq 0) {
                if ($localTagExists -and $Force) {
                    Write-Host "Force-updated local tag '$TagName'."
                }
                else {
                    Write-Host "Created tag '$TagName'."
                }

                $entry.TagCreated = $true
            }
            else {
                Write-Warning "Failed to create/update local tag '$TagName'."
                Add-Note -Entry $entry -Note "Local tag failed"

                if (-not $Force) {
                    $entry.Skipped = $true
                    continue
                }
            }
        }

        if ($hasOrigin) {
            $pushTagArgs = if ($Force) {
                @("push", "origin", "+refs/tags/$TagName`:refs/tags/$TagName")
            }
            else {
                @("push", "origin", "refs/tags/$TagName`:refs/tags/$TagName")
            }

            $pushTagResult = Invoke-Git -Repo $repo -Arguments $pushTagArgs -ContinueOnError:$Force

            if ($pushTagResult.ExitCode -eq 0) {
                Write-Host "Pushed tag '$TagName' to origin."
                $entry.TagPushed = $true
            }
            else {
                Write-Warning "Failed to push tag '$TagName' to origin."
                Add-Note -Entry $entry -Note "Tag push failed"

                if (-not $Force) {
                    $entry.Skipped = $true
                    continue
                }
            }
        }

        # ----------------------------------------
        # Pack / push packages
        # ----------------------------------------

        if ($Pack) {
            $outDir = Join-Path $repo "artifacts\nupkgs"
            New-Item -ItemType Directory -Path $outDir -Force | Out-Null

            if ($Force) {
                # Remove existing packages for this version so this run recreates them.
                Get-ChildItem -Path $outDir -File -ErrorAction SilentlyContinue |
                    Where-Object {
                        $_.Name -like "*.$version.nupkg" -or
                        $_.Name -like "*.$version.snupkg"
                    } |
                    Remove-Item -Force -ErrorAction SilentlyContinue
            }

            $createdPackagesForRepo = @()

            foreach ($projectPath in $repoProjects) {
                Write-Host "Packing project: $projectPath"

                $packStart = (Get-Date).ToUniversalTime().AddSeconds(-2)

                $packArgs = @(
                    "pack",
                    $projectPath,
                    "-c", $Configuration,
                    "-o", $outDir,
                    "/p:PackageVersion=$version",
                    "/p:NoBuild=false"
                )

                $packResult = Invoke-NativeCommand `
                    -FilePath "dotnet" `
                    -Arguments $packArgs `
                    -WorkingDirectory $repo `
                    -ContinueOnError:$Force

                if ($packResult.ExitCode -eq 0) {
                    $entry.PackedProjects++

                    $newPackages = @(Get-ChildItem -Path $outDir -File -ErrorAction SilentlyContinue |
                        Where-Object {
                            $_.LastWriteTimeUtc -ge $packStart -and
                            ($_.Name -like "*.$version.nupkg" -or $_.Name -like "*.$version.snupkg")
                        } |
                        Select-Object -ExpandProperty FullName)

                    foreach ($pkg in $newPackages) {
                        if ($createdPackagesForRepo -notcontains $pkg) {
                            $createdPackagesForRepo += $pkg
                        }
                    }

                    $entry.CreatedPackages = $createdPackagesForRepo.Count
                }
                else {
                    Write-Warning "dotnet pack failed for: $projectPath"
                    Add-Note -Entry $entry -Note "Pack failed: $(Split-Path -Leaf $projectPath)"

                    if (-not $Force) {
                        $entry.Skipped = $true
                        break
                    }
                }
            }

            if ($Push) {
                if (-not $createdPackagesForRepo -or $createdPackagesForRepo.Count -eq 0) {
                    Write-Warning "No packages were created for repo '$repo'. Nothing to push."
                    Add-Note -Entry $entry -Note "No packages to push"
                }
                else {
                    foreach ($pkgPath in $createdPackagesForRepo) {
                        Write-Host "Pushing package: $pkgPath"

                        # --skip-duplicate keeps the run moving if the feed already has this version.
                        # Most NuGet feeds do not allow true overwrite of an existing package version.
                        $nugetPushArgs = @(
                            "nuget",
                            "push",
                            $pkgPath,
                            "--source",
                            $PackageSource,
                            "--skip-duplicate"
                        )

                        $packagePushResult = Invoke-NativeCommand `
                            -FilePath "dotnet" `
                            -Arguments $nugetPushArgs `
                            -WorkingDirectory $repo `
                            -ContinueOnError:$Force

                        if ($packagePushResult.ExitCode -eq 0) {
                            $entry.PushedPackages++
                        }
                        else {
                            Write-Warning "Failed to push package: $pkgPath"
                            Add-Note -Entry $entry -Note "Package push failed: $(Split-Path -Leaf $pkgPath)"

                            if (-not $Force) {
                                $entry.Skipped = $true
                                break
                            }
                        }
                    }
                }
            }
        }
    }
    catch {
        Write-Warning "Error processing '$repo': $($_.Exception.Message)"
        Add-Note -Entry $entry -Note "Error: $($_.Exception.Message)"

        if (-not $Force) {
            $entry.Skipped = $true
        }
    }
    finally {
        $summary += [pscustomobject]$entry
    }
}

Write-Host ""
Write-Host "===== Run Summary ====="
$summary | Format-Table Repository, TagCreated, TagPushed, PackedProjects, CreatedPackages, PushedPackages, Skipped, Notes -AutoSize
