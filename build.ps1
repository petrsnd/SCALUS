#!/usr/bin/env pwsh
#
# Local build entry point for Windows. Thin wrapper that replaces the old Cake
# build: it tests, publishes, then builds the MSI for the given (or win-x64) RID.
#
# Usage:
#   .\build.ps1 [-Runtime win-x64|win-arm64] [-Configuration Release]
#               [-Version 2.0.0] [-SignToolPath <path>] [-SignFiles] [-SkipTests]
#
# When -Version is omitted it defaults to <VersionPrefix> from Directory.Build.props
# (the single checked-in version source), so a local build matches CI.
[CmdletBinding()]
param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$Version = "",
    [string]$SignToolPath = "",
    [switch]$SignFiles,
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

$rootDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $rootDir

if ([string]::IsNullOrWhiteSpace($Version)) {
    $propsFile = Join-Path $rootDir "Directory.Build.props"
    $match = Select-String -Path $propsFile -Pattern '<VersionPrefix>([^<]+)</VersionPrefix>' | Select-Object -First 1
    if (-not $match) { throw "Could not read <VersionPrefix> from $propsFile" }
    $Version = $match.Matches[0].Groups[1].Value.Trim()
    Write-Host "==> No -Version supplied; using VersionPrefix '$Version' from Directory.Build.props"
}

if (-not $SkipTests) {
    Write-Host "==> Testing"
    dotnet test "$rootDir\test\OneIdentity.Scalus.Test.csproj" --configuration $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Tests failed" }
}

Write-Host "==> Publishing"
& "$rootDir\scripts\publish.ps1" -Runtime $Runtime -Configuration $Configuration -Version $Version

Write-Host "==> Packaging $Runtime"
$packageArgs = @{
    Runtime       = $Runtime
    Configuration = $Configuration
    Version       = $Version
    SignToolPath  = $SignToolPath
}
if ($SignFiles) { $packageArgs["SignFiles"] = $true }
& "$rootDir\scripts\Win\package.ps1" @packageArgs

$outputDir = Join-Path $rootDir "Output\$Configuration\$Runtime"
Write-Host "==> Done. Artifacts in $outputDir"
Get-ChildItem $outputDir | Format-Table Name, Length
