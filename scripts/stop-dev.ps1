[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$backendPidFile = Join-Path $projectRoot ".tmp-backend.pid"
$frontendPidFile = Join-Path $projectRoot ".tmp-frontend.pid"

function Stop-FromPidFile {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    $pidValue = Get-Content -LiteralPath $Path -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($pidValue -and ($pidValue -as [int])) {
        Stop-Process -Id ([int]$pidValue) -Force -ErrorAction SilentlyContinue
    }

    Remove-Item -LiteralPath $Path -Force -ErrorAction SilentlyContinue
}

Stop-FromPidFile -Path $backendPidFile
Stop-FromPidFile -Path $frontendPidFile

$targets = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
    Where-Object { $_.LocalPort -in 3007, 5099 } |
    Select-Object -ExpandProperty OwningProcess -Unique

foreach ($targetPid in $targets) {
    if ($targetPid) {
        Stop-Process -Id $targetPid -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "Frontend и backend остановлены." -ForegroundColor Green
