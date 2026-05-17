[CmdletBinding()]
param(
    [string]$BackendUrl = "http://127.0.0.1:5099",
    [int]$FrontendPort = 3007,
    [switch]$ForceRestart
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$frontendRoot = Join-Path $projectRoot "frontend"

$backendStdOut = Join-Path $projectRoot ".tmp-backend-stdout.log"
$backendStdErr = Join-Path $projectRoot ".tmp-backend-stderr.log"
$frontendStdOut = Join-Path $projectRoot ".tmp-frontend-stdout.log"
$frontendStdErr = Join-Path $projectRoot ".tmp-frontend-stderr.log"
$backendPidFile = Join-Path $projectRoot ".tmp-backend.pid"
$frontendPidFile = Join-Path $projectRoot ".tmp-frontend.pid"

function Remove-StaleFile {
    param([string]$Path)

    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Force
    }
}

function Get-PortProcessIds {
    param([int[]]$Ports)

    $listeners = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
        Where-Object { $_.LocalPort -in $Ports } |
        Select-Object -ExpandProperty OwningProcess -Unique

    return @($listeners | Where-Object { $_ })
}

function Wait-ForHttpOk {
    param(
        [string]$Url,
        [int]$TimeoutSeconds = 25
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    do {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -eq 200) {
                return $true
            }
        }
        catch {
            Start-Sleep -Milliseconds 700
        }
    } while ((Get-Date) -lt $deadline)

    return $false
}

$ports = @(5099, $FrontendPort)
$runningProcessIds = Get-PortProcessIds -Ports $ports

if ($runningProcessIds.Count -gt 0) {
    if (-not $ForceRestart) {
        Write-Host "Services are already running on ports $($ports -join ', ')." -ForegroundColor Yellow
        Write-Host "If you want this script to restart them, run:"
        Write-Host "  .\\scripts\\start-dev.ps1 -ForceRestart" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Current URLs:"
        Write-Host "  frontend: http://127.0.0.1:$FrontendPort"
        Write-Host "  backend : $BackendUrl/swagger/index.html"
        exit 0
    }

    foreach ($processId in $runningProcessIds) {
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    }

    Start-Sleep -Seconds 1
}

Remove-StaleFile -Path $backendStdOut
Remove-StaleFile -Path $backendStdErr
Remove-StaleFile -Path $frontendStdOut
Remove-StaleFile -Path $frontendStdErr
Remove-StaleFile -Path $backendPidFile
Remove-StaleFile -Path $frontendPidFile

Remove-Item Env:HTTP_PROXY -ErrorAction SilentlyContinue
Remove-Item Env:HTTPS_PROXY -ErrorAction SilentlyContinue
Remove-Item Env:ALL_PROXY -ErrorAction SilentlyContinue
$env:NO_PROXY = "localhost,127.0.0.1,::1,api.deepseek.com"

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DOTNET_CLI_HOME = $projectRoot
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

$backendProcess = Start-Process `
    -FilePath "C:\Program Files\dotnet\dotnet.exe" `
    -ArgumentList @("run", "--no-build", "--project", "src/RentGen.Api/RentGen.Api.csproj", "--urls", $BackendUrl) `
    -WorkingDirectory $projectRoot `
    -RedirectStandardOutput $backendStdOut `
    -RedirectStandardError $backendStdErr `
    -WindowStyle Hidden `
    -PassThru

$backendProcess.Id | Set-Content -Path $backendPidFile -Encoding ascii

$frontendProcess = Start-Process `
    -FilePath "C:\WINDOWS\System32\WindowsPowerShell\v1.0\powershell.exe" `
    -ArgumentList @("-Command", "npm run dev -- --hostname 127.0.0.1 --port $FrontendPort") `
    -WorkingDirectory $frontendRoot `
    -RedirectStandardOutput $frontendStdOut `
    -RedirectStandardError $frontendStdErr `
    -WindowStyle Hidden `
    -PassThru

$frontendProcess.Id | Set-Content -Path $frontendPidFile -Encoding ascii

$backendOk = Wait-ForHttpOk -Url "$BackendUrl/swagger/index.html"
$frontendOk = Wait-ForHttpOk -Url "http://127.0.0.1:$FrontendPort"

if (-not $backendOk -or -not $frontendOk) {
    Write-Host "Failed to wait for one of the services to start." -ForegroundColor Red
    Write-Host "Check logs:"
    Write-Host "  backend out: $backendStdOut"
    Write-Host "  backend err: $backendStdErr"
    Write-Host "  frontend out: $frontendStdOut"
    Write-Host "  frontend err: $frontendStdErr"
    exit 1
}

Write-Host ""
Write-Host "Local dev environment started." -ForegroundColor Green
Write-Host "  frontend: http://127.0.0.1:$FrontendPort"
Write-Host "  backend : $BackendUrl/swagger/index.html"
Write-Host ""
Write-Host "PID files:"
Write-Host "  $backendPidFile"
Write-Host "  $frontendPidFile"
Write-Host ""
Write-Host "Stop with:" -ForegroundColor Cyan
Write-Host "  .\\scripts\\stop-dev.ps1"
