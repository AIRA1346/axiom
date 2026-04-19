# GSI 프로젝트 구조 검증 — GitHub Actions gsi-smoke-validation.yml 과 동일합니다.
# (필수 파일/폴더 + EditorBuildSettings에 등록된 씬 파일이 디스크에 있는지)
# 사용: 독립 저장소(gsi-repos) 루트에서 .\Tools\Validate-GsiProject.ps1
#      모노레포(RuneAtelier)에서는 .\GSI\Tools\Validate-GsiProject.ps1
#      또는 -GsiRoot "C:\path\to\GSI" 로 Unity 프로젝트 루트를 직접 지정

param(
    [string] $GsiRoot = ""
)

$ErrorActionPreference = "Stop"
$failed = $false

if (-not $GsiRoot) {
    $toolsParent = Split-Path -Parent $PSScriptRoot
    if (Test-Path -LiteralPath (Join-Path $toolsParent "ProjectSettings\ProjectVersion.txt")) {
        $GsiRoot = $toolsParent
    } else {
        $repoRoot = Split-Path -Parent $toolsParent
        $GsiRoot = Join-Path $repoRoot "GSI"
    }
}

$GsiRoot = [System.IO.Path]::GetFullPath($GsiRoot)

if (-not (Test-Path -LiteralPath $GsiRoot -PathType Container)) {
    Write-Host "GSI root not found: $GsiRoot" -ForegroundColor Red
    exit 1
}

Write-Host "GSI root: $GsiRoot" -ForegroundColor Cyan

$dirs = @(
    "Assets",
    "Assets\Scenes",
    "Assets\Scripts",
    "Assets\Scripts\Core",
    "Assets\Scripts\Economy",
    "Assets\Scripts\GSI",
    "Assets\Scripts\Inventory",
    "Assets\Scripts\Shop",
    "Packages",
    "ProjectSettings"
)

foreach ($d in $dirs) {
    $full = Join-Path $GsiRoot $d
    if (-not (Test-Path -LiteralPath $full -PathType Container)) {
        Write-Host "MISSING DIR:  $d" -ForegroundColor Red
        $failed = $true
    }
    else {
        Write-Host "OK            $d/" -ForegroundColor DarkGray
    }
}

$files = @(
    "ProjectSettings\ProjectVersion.txt",
    "Packages\manifest.json",
    "Packages\packages-lock.json",
    "Assets\Scripts\Core\SceneNames.cs",
    "Assets\Scripts\Core\GsiGameplayWorldCameraHooks.cs",
    "Assets\Scripts\Core\GsiAudioService.cs",
    "Assets\Scripts\Core\GameLocalization.cs",
    "Assets\Scripts\Core\GameManager.cs",
    "Assets\Scripts\AsyncVersus\VersusAsyncContracts.cs",
    "Assets\Scripts\AsyncVersus\VersusAsyncBridge.cs",
    "Assets\Scripts\AsyncVersus\VersusAsyncBackendSettings.cs",
    "Assets\Scripts\AsyncVersus\VersusAsyncHttpTransport.cs",
    "Assets\Scripts\AsyncVersus\VersusAsyncMainThreadRunner.cs",
    "Assets\Scripts\AsyncVersus\VersusAsyncOutbox.cs",
    "Assets\Scripts\Core\UnifiedExamHistoryEntry.cs",
    "Assets\Scripts\Core\GsiUnifiedExamHistoryOverlayRoot.cs",
    "Assets\Scripts\Core\GsiSceneNavigation.cs",
    "Assets\Scripts\Core\GsiSceneTransition.cs",
    "Assets\Scripts\Core\GsiRuntimeUiBootstrap.cs",
    "Assets\Scripts\Core\GsiShopLikeCanvasHeaderUi.cs",
    "Assets\Scripts\Core\GsiShopLikeEconomyBarTexts.cs",
    "Assets\Scripts\Core\GsiShopLikeListScrollUi.cs",
    "Assets\Scripts\Core\GsiShopLikeOfferDisplayNames.cs",
    "Assets\Scripts\Core\GsiRuntimeUiRowPool.cs",
    "Assets\Scripts\Core\GsiSettingsAudioApplier.cs",
    "Assets\Scripts\Core\GsiUiAppearance.cs",
    "Assets\Scripts\Core\GsiUiScreenLayout.cs",
    "Assets\Scripts\Core\GsiUiRuntimeWidgets.cs",
    "Assets\Scripts\Core\GsiTestBriefingTexts.cs",
    "Assets\Scripts\Core\GsiTestBriefingUi.cs",
    "Assets\Scripts\Core\GsiUserSettings.cs",
    "Assets\Scripts\Core\TmpFontCache.cs",
    "Assets\Scripts\Core\TouchInputFeedback.cs",
    "Assets\Scripts\Core\UIManager.cs",
    "Assets\Scripts\Core\UiStringKeys.cs",
    "Assets\Scripts\Core\GlobalSettingsOverlay.cs",
    "Assets\Scripts\Core\IntroController.cs",
    "Assets\Scripts\Core\LobbyButtonLabelHoverBoost.cs",
    "Assets\Scripts\Core\MainMenuController.cs",
    "Assets\Scripts\Economy\EconomyManager.cs",
    "Assets\Scripts\GSI\GSIHubMenuController.cs",
    "Assets\Scripts\Inventory\InventorySceneController.cs",
    "Assets\Scripts\Core\AltarOfVeritySceneController.cs",
    "Assets\Editor\AltarOfVeritySceneSetupMenu.cs",
    "Assets\Editor\VersusAsyncBackendEditorWindow.cs",
    "Assets\Editor\SteamworksBuildDefinesMenu.cs",
    "Assets\Scripts\Shop\CosmeticTheme.cs",
    "Assets\Scripts\Shop\PlayerCosmetics.cs",
    "Assets\Scripts\Shop\ShopCatalog.cs",
    "Assets\Scripts\Shop\ShopSceneController.cs"
)

foreach ($f in $files) {
    $full = Join-Path $GsiRoot $f
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
        Write-Host "MISSING FILE: $f" -ForegroundColor Red
        $failed = $true
    }
    else {
        Write-Host "OK            $f" -ForegroundColor DarkGray
    }
}

$rootExtra = Join-Path $GsiRoot "steam_appid.txt.example"
if (-not (Test-Path -LiteralPath $rootExtra -PathType Leaf)) {
    Write-Host "MISSING FILE: steam_appid.txt.example (project root)" -ForegroundColor Red
    $failed = $true
}
else {
    Write-Host "OK            steam_appid.txt.example" -ForegroundColor DarkGray
}

$audioReadme = Join-Path $GsiRoot "Assets\Audio\README.txt"
if (-not (Test-Path -LiteralPath $audioReadme -PathType Leaf)) {
    Write-Host "MISSING FILE: Assets\Audio\README.txt" -ForegroundColor Red
    $failed = $true
}
else {
    Write-Host "OK            Assets\Audio\README.txt" -ForegroundColor DarkGray
}

$ebsPath = Join-Path $GsiRoot "ProjectSettings\EditorBuildSettings.asset"
if (Test-Path -LiteralPath $ebsPath) {
    Write-Host ""
    Write-Host "Editor build scenes (EditorBuildSettings.asset):" -ForegroundColor Cyan
    $sceneCount = 0
    Get-Content -LiteralPath $ebsPath | ForEach-Object {
        if ($_ -match '^\s+path:\s+(Assets/.+\.unity)\s*$') {
            $sceneCount++
            $rel = $matches[1] -replace '/', [System.IO.Path]::DirectorySeparatorChar
            $sceneFull = Join-Path $GsiRoot $rel
            if (-not (Test-Path -LiteralPath $sceneFull -PathType Leaf)) {
                Write-Host "MISSING SCENE (in build order): $rel" -ForegroundColor Red
                $failed = $true
            }
            else {
                Write-Host "OK            $rel" -ForegroundColor DarkGray
            }
        }
    }
    if ($sceneCount -eq 0) {
        Write-Host "No Assets/*.unity entries in EditorBuildSettings.asset" -ForegroundColor Red
        $failed = $true
    }
}

$snPath = Join-Path $GsiRoot "Assets\Scripts\Core\SceneNames.cs"
if ((Test-Path -LiteralPath $snPath) -and (Test-Path -LiteralPath $ebsPath)) {
    Write-Host ""
    Write-Host "SceneNames.cs vs EditorBuildSettings (scene name sets):" -ForegroundColor Cyan
    $fromCs = New-Object 'System.Collections.Generic.HashSet[string]'
    $rawSn = Get-Content -LiteralPath $snPath -Raw
    foreach ($m in [regex]::Matches($rawSn, 'public const string \w+ = "([^"]+)"')) {
        $val = $m.Groups[1].Value
        # Scene file paths (e.g. SceneNames.AltarOfVerityAssetPath) are not runtime scene names.
        if ($val -match '/' -or $val -match '\.unity$') { continue }
        [void]$fromCs.Add($val)
    }
    $fromEbs = New-Object 'System.Collections.Generic.HashSet[string]'
    Get-Content -LiteralPath $ebsPath | ForEach-Object {
        if ($_ -match '^\s+path:\s+(Assets/.+\.unity)\s*$') {
            $leaf = [System.IO.Path]::GetFileName($matches[1])
            $base = [System.IO.Path]::GetFileNameWithoutExtension($leaf)
            [void]$fromEbs.Add($base)
        }
    }
    $onlyCs = @($fromCs | Where-Object { -not $fromEbs.Contains($_) })
    $onlyEbs = @($fromEbs | Where-Object { -not $fromCs.Contains($_) })
    if ($onlyCs.Count -gt 0 -or $onlyEbs.Count -gt 0) {
        Write-Host "Mismatch: SceneNames constants and EditorBuildSettings scenes must match." -ForegroundColor Red
        if ($onlyCs.Count -gt 0) {
            Write-Host "  Only in SceneNames.cs: $($onlyCs -join ', ')" -ForegroundColor Red
        }
        if ($onlyEbs.Count -gt 0) {
            Write-Host "  Only in EditorBuildSettings: $($onlyEbs -join ', ')" -ForegroundColor Red
        }
        $failed = $true
    }
    else {
        foreach ($n in ($fromCs | Sort-Object)) {
            Write-Host "OK            scene name: $n" -ForegroundColor DarkGray
        }
    }
}

if ($failed) {
    Write-Host ""
    Write-Host "GSI structure check failed." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "GSI smoke structure check passed." -ForegroundColor Green
exit 0
