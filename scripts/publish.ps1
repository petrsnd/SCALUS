#!/usr/bin/env pwsh
#
# Publish the SCALUS payload for one Windows runtime.
#
# Produces, under Publish\<config>\<rid>\:
#   scalus.exe        - the NativeAOT command-line launcher (OS protocol hot path)
#   ui\scalus-ui.exe  - the self-contained Photino configuration GUI + webview
#   examples\         - example templates + the SCALUS.json seed + readme
#
# The Windows packager (scripts\Win\package.ps1) consumes this directory.
# This replaces the old Cake "Publish"/"PublishUi" tasks.
#
# Usage: scripts\publish.ps1 -Runtime win-x64 [-Configuration Release] [-Version 1.0.0]
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Runtime,
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = (Resolve-Path (Join-Path $scriptDir "..")).Path
Set-Location $rootDir

$publishDir = Join-Path $rootDir "Publish\$Configuration\$Runtime"
$uiDir = Join-Path $publishDir "ui"
$exDir = Join-Path $publishDir "examples"

Write-Host "==> Publishing SCALUS $Version for $Runtime ($Configuration)"
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

# 1. NativeAOT command-line launcher. PublishAot=true in the csproj already
#    yields a single native binary, so no PublishSingleFile is needed.
Write-Host "==> Publishing CLI (scalus.exe)"
dotnet publish "src\Cli\Scalus.Cli.csproj" `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    -p:Version=$Version `
    --output $publishDir
if ($LASTEXITCODE -ne 0) { throw "CLI publish failed" }

# 2. Photino configuration GUI. Self-contained but NOT single-file/AOT: the
#    apphost needs its native webview .dll, wwwroot and runtime files loose.
Write-Host "==> Publishing UI (scalus-ui.exe)"
dotnet publish "src\Scalus.Ui\Scalus.Ui.csproj" `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    -p:Version=$Version `
    --output $uiDir
if ($LASTEXITCODE -ne 0) { throw "UI publish failed" }

# 3. Example templates + seed + readme.
Write-Host "==> Staging examples"
New-Item -ItemType Directory -Force -Path $exDir | Out-Null
Copy-Item -Recurse -Force "scripts\examples\*" $exDir
Copy-Item -Force "src\SCALUS.json" (Join-Path $exDir "SCALUS.json")
$readme = Join-Path $exDir "readme.txt"
Copy-Item -Force "scripts\readme.txt" $readme
(Get-Content $readme) -replace "SCALUSVERSION", $Version | Set-Content $readme

Write-Host "==> Publish complete: $publishDir"
Get-ChildItem $publishDir | Format-Table Name, Length
