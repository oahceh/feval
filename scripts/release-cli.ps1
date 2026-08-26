#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Bump Feval.Cli's <Version>, commit, tag as cli-vX.Y.Z, and optionally push.
    Pushing the tag triggers .github/workflows/publish-cli.yml on GitHub,
    which packs and pushes the CLI to nuget.org.

.EXAMPLE
    ./scripts/release-cli.ps1 -Version 1.8.0
    ./scripts/release-cli.ps1 -Version 1.8.0 -Push
    ./scripts/release-cli.ps1 -Version 1.8.0-rc.1 -Push
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[A-Za-z0-9\.-]+)?$')]
    [string]$Version,

    [switch]$SkipTest,
    [switch]$Push
)

$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

$branch = (git rev-parse --abbrev-ref HEAD).Trim()
if ($branch -ne 'master') {
    throw "Refusing to release from branch '$branch'. Check out master first."
}

if ((git status --porcelain).Trim()) {
    throw "Working tree is dirty. Commit or stash before running this script."
}

$csprojPath = 'Feval.Cli/Feval.Cli.csproj'
$content = Get-Content $csprojPath -Raw
if (-not ($content -match '<Version>([^<]+)</Version>')) {
    throw "Could not find <Version> in $csprojPath."
}
$oldVersion = $Matches[1]
if ($oldVersion -eq $Version) {
    throw "csproj is already at version $Version. Nothing to bump."
}

Write-Host "Bumping Feval.Cli: $oldVersion -> $Version"
$updated = $content -replace '<Version>[^<]+</Version>', "<Version>$Version</Version>"
Set-Content -Path $csprojPath -Value $updated -NoNewline

if (-not $SkipTest) {
    Write-Host 'Running tests...'
    dotnet test Feval.UnitTests/Feval.UnitTests.csproj -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed. Aborting; revert csproj manually."
    }
}

Write-Host 'Packing locally to verify...'
dotnet pack $csprojPath -c Release -o artifacts --nologo
if ($LASTEXITCODE -ne 0) {
    throw "Local pack failed. Aborting; revert csproj manually."
}

$tag = "cli-v$Version"
git add $csprojPath
git commit -m "chore: Bump Feval.Cli version to $Version"
git tag -a $tag -m "Feval.Cli $Version"
Write-Host "Committed and tagged $tag."

if ($Push) {
    git push origin master
    git push origin $tag
    Write-Host "Pushed. Watch the workflow at: https://github.com/oahceh/feval/actions"
} else {
    Write-Host ""
    Write-Host "Not pushing. To publish, run:"
    Write-Host "  git push origin master; git push origin $tag"
}
