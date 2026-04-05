# P0 검증: 메타 빌드 + Codex 본문 (Unity batchmode, 종료 코드 0/1)
# 사용: UNITY_EDITOR_PATH에 Unity.exe 전체 경로 설정 후
#   .\Tools\Run-P0Validation.ps1
# 또는
#   .\Tools\Run-P0Validation.ps1 -UnityEditorPath "C:\Program Files\Unity\Hub\Editor\6000.x.x\Editor\Unity.exe"

param(
    [string]$UnityEditorPath = $env:UNITY_EDITOR_PATH
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$projectRoot = Join-Path $repoRoot "ArchE"

if (-not $UnityEditorPath -or -not (Test-Path -LiteralPath $UnityEditorPath)) {
    Write-Error "Unity.exe 경로를 지정하세요. 예: `$env:UNITY_EDITOR_PATH = '...\\Editor\\Unity.exe' 또는 -UnityEditorPath"
}

$unityArgs = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $projectRoot,
    "-executeMethod", "ArchEP0ValidationGate.RunP0GateForCi"
)

Write-Host "Unity:" $UnityEditorPath
Write-Host "Project:" $projectRoot

& $UnityEditorPath @unityArgs
exit $LASTEXITCODE
