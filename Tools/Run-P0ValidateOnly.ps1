# P0 검증만 (메타 DB 재빌드 없음) — CI의 EditMode 테스트와 동일한 검사.
# Unity 저장소 secrets / 로컬 batchmode에 적합.
# 사용: .\Tools\Run-P0ValidateOnly.ps1

param(
    [string]$UnityEditorPath = $env:UNITY_EDITOR_PATH
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$projectRoot = Join-Path $repoRoot "ArchE"

if (-not $UnityEditorPath -or -not (Test-Path -LiteralPath $UnityEditorPath)) {
    Write-Error "Unity.exe 경로를 지정하세요. 예: `$env:UNITY_EDITOR_PATH = '...\\Editor\\Unity.exe'"
}

$unityArgs = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $projectRoot,
    "-executeMethod", "ArchEP0ValidationGate.RunP0ValidateOnlyForCi"
)

Write-Host "Unity:" $UnityEditorPath
Write-Host "Project:" $projectRoot

& $UnityEditorPath @unityArgs
exit $LASTEXITCODE
