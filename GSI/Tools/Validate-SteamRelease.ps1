# Steam release preflight checks that do not require launching Unity.
# Usage:
#   .\Tools\Validate-SteamRelease.ps1
#   .\Tools\Validate-SteamRelease.ps1 -BuildOutput "C:\path\to\TheAxiomBuild"
#   .\Tools\Validate-SteamRelease.ps1 -BetaRelease
#   .\Tools\Validate-SteamRelease.ps1 -StrictRelease

param(
    [string] $GsiRoot = "",
    [string] $BuildOutput = "",
    [switch] $BetaRelease,
    [switch] $StrictRelease
)

$ErrorActionPreference = "Stop"
$failed = $false

if (-not $GsiRoot) {
    $GsiRoot = Split-Path -Parent $PSScriptRoot
}

$GsiRoot = [System.IO.Path]::GetFullPath($GsiRoot)
Write-Host "GSI root: $GsiRoot" -ForegroundColor Cyan

function Require-File([string] $RelPath) {
    $full = Join-Path $GsiRoot $RelPath
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
        Write-Host "MISSING: $RelPath" -ForegroundColor Red
        $script:failed = $true
    } else {
        Write-Host "OK       $RelPath" -ForegroundColor DarkGray
    }
}

Require-File "ProjectSettings\EditorBuildSettings.asset"
Require-File "ProjectSettings\ProjectSettings.asset"
Require-File "Packages\manifest.json"
Require-File "Assets\Docs\STEAM_RELEASE_CHECKLIST.md"
Require-File "Assets\Docs\STEAM_EULA_TEMPLATE.md"
Require-File "Assets\Docs\STEAM_PRIVACY_TEMPLATE.md"
Require-File "Assets\Docs\STEAM_SUPPORT_RUNBOOK.md"
Require-File "Assets\Docs\STEAM_STORE_PAGE_CHECKLIST.md"
Require-File "Assets\Docs\STEAM_QA_MATRIX.md"
Require-File "Assets\AddressableAssetsData\AddressableAssetSettings.asset"
Require-File "Assets\Editor\SteamReleaseReadinessMenu.cs"
Require-File "Assets\Editor\SteamWindowsReleaseBuilder.cs"
Require-File "Tools\SteamPipe\README.md"
Require-File "Tools\SteamPipe\New-SteamPipeBuildFiles.ps1"
Require-File "Tools\SteamPipe\app_build_the_axiom.vdf.template"
Require-File "Tools\SteamPipe\depot_build_windows.vdf.template"

$ebs = Join-Path $GsiRoot "ProjectSettings\EditorBuildSettings.asset"
if (Test-Path -LiteralPath $ebs) {
    $scenePaths = @(Select-String -LiteralPath $ebs -Pattern 'path: Assets/.+\.unity' | ForEach-Object {
        if ($_.Line -match 'path:\s+(Assets/.+\.unity)') { $matches[1] }
    })
    if ($scenePaths.Count -eq 0) {
        Write-Host "ERROR    No scenes in EditorBuildSettings.asset" -ForegroundColor Red
        $failed = $true
    } elseif ($scenePaths[0] -ne "Assets/Scenes/IntroScene.unity") {
        Write-Host "ERROR    IntroScene must be first. Current first: $($scenePaths[0])" -ForegroundColor Red
        $failed = $true
    } else {
        Write-Host "OK       IntroScene is first in Build Settings" -ForegroundColor DarkGray
    }
}

$projectSettings = Join-Path $GsiRoot "ProjectSettings\ProjectSettings.asset"
if (Test-Path -LiteralPath $projectSettings) {
    $raw = Get-Content -LiteralPath $projectSettings -Raw
    if ($raw -notmatch "STEAMWORKS_ENABLED") {
        Write-Host "ERROR    STEAMWORKS_ENABLED is not present in ProjectSettings.asset" -ForegroundColor Red
        $failed = $true
    } else {
        Write-Host "OK       STEAMWORKS_ENABLED present" -ForegroundColor DarkGray
    }

    if ($raw -match "bundleVersion:\s*0\.") {
        if ($StrictRelease -and -not $BetaRelease) {
            Write-Host "ERROR    bundleVersion appears pre-1.0. Set final Steam release version before upload." -ForegroundColor Red
            $failed = $true
        } elseif ($BetaRelease) {
            Write-Host "OK       beta release allows pre-1.0 bundleVersion" -ForegroundColor DarkGray
        } else {
            Write-Host "WARN     bundleVersion appears pre-1.0. Set final Steam release version before upload." -ForegroundColor Yellow
        }
    }
}

$localAppId = Join-Path $GsiRoot "steam_appid.txt"
if (Test-Path -LiteralPath $localAppId -PathType Leaf) {
    $appId = (Get-Content -LiteralPath $localAppId -Raw).Trim()
    if ($appId -eq "480") {
        Write-Host "WARN     steam_appid.txt is 480 (Spacewar). Replace with real App ID for final local QA." -ForegroundColor Yellow
    } else {
        Write-Host "OK       local steam_appid.txt uses non-Spacewar App ID" -ForegroundColor DarkGray
    }
}

if ($BuildOutput) {
    $BuildOutput = [System.IO.Path]::GetFullPath($BuildOutput)
    if (-not (Test-Path -LiteralPath $BuildOutput)) {
        Write-Host "ERROR    BuildOutput not found: $BuildOutput" -ForegroundColor Red
        $failed = $true
    } else {
        $bad = @(Get-ChildItem -LiteralPath $BuildOutput -Filter "steam_appid.txt" -Recurse -File -ErrorAction SilentlyContinue)
        if ($bad.Count -gt 0) {
            Write-Host "ERROR    steam_appid.txt found in build output/depot candidate:" -ForegroundColor Red
            $bad | ForEach-Object { Write-Host "         $($_.FullName)" -ForegroundColor Red }
            $failed = $true
        } else {
            Write-Host "OK       no steam_appid.txt in build output" -ForegroundColor DarkGray
        }
    }
}

if ($failed) {
    Write-Host ""
    Write-Host "Steam release validation failed." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Steam release validation passed with any warnings shown above." -ForegroundColor Green
exit 0
