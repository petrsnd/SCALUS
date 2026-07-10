#!/usr/bin/env pwsh
#
# Build the SCALUS Windows MSI from a published payload (WiX v4+ via the `wix`
# dotnet tool). Replaces the old Cake "MsiInstaller" task.
#
# Produces Output\<config>\<rid>\scalus-setup-<version>-<rid>.msi
#
# Prerequisites:
#   dotnet tool install --global wix
#   wix extension add -g WixToolset.UI.wixext
#
# Usage:
#   scripts\Win\package.ps1 -Runtime win-x64 [-Configuration Release] [-Version 2.0.0]
#                           [-SignToolPath <path>] [-SignFiles]
#
# When -Version is omitted it defaults to <VersionPrefix> from Directory.Build.props
# (the single checked-in version source).
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Runtime,
    [string]$Configuration = "Release",
    [string]$Version = "",
    [string]$SignToolPath = "",
    [switch]$SignFiles
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = (Resolve-Path (Join-Path $scriptDir "..\..")).Path
Set-Location $rootDir

if ([string]::IsNullOrWhiteSpace($Version)) {
    $propsFile = Join-Path $rootDir "Directory.Build.props"
    $match = Select-String -Path $propsFile -Pattern '<VersionPrefix>([^<]+)</VersionPrefix>' | Select-Object -First 1
    if (-not $match) { throw "Could not read <VersionPrefix> from $propsFile" }
    $Version = $match.Matches[0].Groups[1].Value.Trim()
    Write-Host "==> No -Version supplied; using VersionPrefix '$Version' from Directory.Build.props"
}

$publishDir = Join-Path $rootDir "Publish\$Configuration\$Runtime"
$outputDir = Join-Path $rootDir "Output\$Configuration\$Runtime"
$tmpDir = Join-Path $outputDir "tmp"

$scalusExe = Join-Path $publishDir "scalus.exe"
if (-not (Test-Path $scalusExe)) {
    throw "$scalusExe not found. Run scripts\publish.ps1 -Runtime $Runtime first."
}

# Map the .NET RID to the WiX/MSI target architecture.
switch ($Runtime) {
    "win-x64"   { $arch = "x64" }
    "win-arm64" { $arch = "arm64" }
    default     { throw "Unsupported Windows runtime: $Runtime (expected win-x64 or win-arm64)" }
}

$canSign = $false
if ($SignFiles) {
    if ([string]::IsNullOrEmpty($SignToolPath)) {
        Write-Host "SignFiles was set but SignToolPath is empty; skipping signing."
    }
    elseif (-not (Test-Path $SignToolPath)) {
        Write-Host "signtool not found at $SignToolPath; skipping signing."
    }
    else {
        $canSign = $true
    }
}

function Invoke-Sign([string]$file) {
    if (-not $canSign) { return }
    Write-Host "==> Signing $file"
    & $SignToolPath sign /fd sha256 /tr http://ts.ssl.com /td sha256 /n "One Identity LLC" $file
    if ($LASTEXITCODE -ne 0) { throw "signtool failed for $file" }
}

Write-Host "==> Packaging Windows MSI for $Runtime ($arch)"

# Sign the launcher before it is harvested into the MSI.
Invoke-Sign $scalusExe

# Stage the installer chrome assets the .wxs references via $(var.tmpdir).
if (Test-Path $tmpDir) { Remove-Item -Recurse -Force $tmpDir }
New-Item -ItemType Directory -Force -Path $tmpDir | Out-Null
Copy-Item -Force "src\scalus.ico" (Join-Path $tmpDir "icon.ico")
Copy-Item -Force "src\Banner-Scalus.bmp" (Join-Path $tmpDir "banner.bmp")
Copy-Item -Force "src\Dialog-Scalus.bmp" (Join-Path $tmpDir "dialog.bmp")
Copy-Item -Force "scripts\license.rtf" (Join-Path $tmpDir "license.rtf")

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
$msiPath = Join-Path $outputDir "scalus-setup-$Version-$Runtime.msi"
if (Test-Path $msiPath) { Remove-Item -Force $msiPath }

# Harvest the whole published payload and link the MSI.
wix build `
    "scripts\Win\Product.wxs" `
    -arch $arch `
    -d "sourcedir=$publishDir" `
    -d "tmpdir=$tmpDir" `
    -d "Version=$Version" `
    -ext WixToolset.UI.wixext `
    -o $msiPath
if ($LASTEXITCODE -ne 0) { throw "wix build failed" }

Invoke-Sign $msiPath

Remove-Item -Recurse -Force $tmpDir

Write-Host "==> Built $msiPath"
Get-Item $msiPath | Format-Table Name, Length
