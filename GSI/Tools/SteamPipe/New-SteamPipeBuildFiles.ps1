# Creates local SteamPipe .vdf files from templates.
# Generated .vdf files can contain Partner IDs and should not be committed.
#
# Usage:
#   .\New-SteamPipeBuildFiles.ps1 -AppId 1234560 -WindowsDepotId 1234561

param(
    [Parameter(Mandatory = $true)]
    [string] $AppId,

    [Parameter(Mandatory = $true)]
    [string] $WindowsDepotId
)

$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$appTemplate = Join-Path $root "app_build_the_axiom.vdf.template"
$depotTemplate = Join-Path $root "depot_build_windows.vdf.template"
$appOut = Join-Path $root "app_build_the_axiom.vdf"
$depotOut = Join-Path $root "depot_build_windows.vdf"

if (-not (Test-Path -LiteralPath $appTemplate -PathType Leaf)) {
    throw "Missing template: $appTemplate"
}

if (-not (Test-Path -LiteralPath $depotTemplate -PathType Leaf)) {
    throw "Missing template: $depotTemplate"
}

(Get-Content -LiteralPath $appTemplate -Raw).
    Replace("TODO_STEAM_APP_ID", $AppId).
    Replace("TODO_WINDOWS_DEPOT_ID", $WindowsDepotId) |
    Set-Content -LiteralPath $appOut -Encoding UTF8

(Get-Content -LiteralPath $depotTemplate -Raw).
    Replace("TODO_WINDOWS_DEPOT_ID", $WindowsDepotId) |
    Set-Content -LiteralPath $depotOut -Encoding UTF8

Write-Host "Created:" -ForegroundColor Green
Write-Host "  $appOut"
Write-Host "  $depotOut"
Write-Host "Do not commit generated .vdf files." -ForegroundColor Yellow
