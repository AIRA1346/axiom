# Closes nothing for you: run ONLY after exiting Cursor, Unity, Unity Hub, terminals using this repo.
# Renames C:\Users\rkdwl\G.S.I  ->  C:\Users\rkdwl\RuneAtelier
# Then reopen Cursor/Unity Hub and add the project from the new path.

$ErrorActionPreference = "Stop"
$oldPath = "C:\Users\rkdwl\G.S.I"
$newName = "RuneAtelier"

if (-not (Test-Path -LiteralPath $oldPath)) {
    Write-Host "Skip: not found:" $oldPath
    exit 0
}

$parent = Split-Path -Parent $oldPath
$newPath = Join-Path $parent $newName

if (Test-Path -LiteralPath $newPath) {
    Write-Error "Already exists: $newPath"
}

Rename-Item -LiteralPath $oldPath -NewName $newName
Write-Host "OK:" $newPath
Write-Host "Reopen Cursor/Unity from that folder."
