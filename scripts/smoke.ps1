<#
.SYNOPSIS
    Smoke test de release para Turning. Falla (exit 1) en el primer paso que rompa.
.DESCRIPTION
    1) Restaura y compila la solucion.
    2) Corre toda la bateria de tests (Domain/Application/Infrastructure).
    3) Aplica la migracion de EF Core contra LocalDB.
    4) Levanta la API en background y valida GET /api/health.
.NOTES
    Ejecutar desde la raiz del repo:  ./scripts/smoke.ps1
    Requiere: .NET SDK, SQL Server LocalDB (o ajustar -ConnectionString para SQLite).
#>

param(
    [string]$Solution = "turning.sln",
    [string]$ApiProject = "src/turning.API",
    [string]$InfraProject = "src/turning.Infrastructure",
    [string]$ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=Turning;Trusted_Connection=True;TrustServerCertificate=True",
    [string]$HealthUrl = "http://localhost:5080/api/health",
    [int]$HealthTimeoutSeconds = 30
)

$ErrorActionPreference = "Stop"
$exitCode = 0

# Fijamos el ambiente de forma explicita para todo el script, para no depender
# de lo que haya quedado configurado en la sesion de PowerShell (evita el error
# "Connection string keyword 'server' is not supported", que pasa si el ambiente
# queda en Development -> SQLite mientras se le pasa una cadena de SQL Server).
$env:ASPNETCORE_ENVIRONMENT = "Production"

function Step($name, [scriptblock]$action) {
    Write-Host "==> $name" -ForegroundColor Cyan
    & $action
    if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne $null) {
        Write-Host "FALLO: $name (exit $LASTEXITCODE)" -ForegroundColor Red
        exit 1
    }
    Write-Host "OK: $name" -ForegroundColor Green
}

Step "dotnet build $Solution" { dotnet build $Solution --no-restore }
Step "dotnet test $Solution"  { dotnet test $Solution --no-restore }
Step "dotnet ef database update" {
    dotnet ef database update `
        --project $InfraProject `
        --startup-project $ApiProject `
        --connection $ConnectionString
}

Write-Host "==> Levantando API en background para validar /api/health" -ForegroundColor Cyan
$apiProcess = Start-Process -PassThru -NoNewWindow dotnet -ArgumentList "run --project $ApiProject --urls http://localhost:5080"

try {
    $healthy = $false
    $deadline = (Get-Date).AddSeconds($HealthTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-RestMethod -Uri $HealthUrl -TimeoutSec 3
            if ($response.isHealthy) {
                $healthy = $true
                break
            }
        } catch {
            Start-Sleep -Seconds 2
        }
    }

    if (-not $healthy) {
        Write-Host "FALLO: $HealthUrl no respondio healthy en $HealthTimeoutSeconds s" -ForegroundColor Red
        $exitCode = 1
    } else {
        Write-Host "OK: health check verde ($HealthUrl)" -ForegroundColor Green
    }
} finally {
    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
    }
}

if ($exitCode -eq 0) {
    Write-Host "`nSMOKE TEST COMPLETO: build + test + migracion + health OK" -ForegroundColor Green
} else {
    Write-Host "`nSMOKE TEST FALLO" -ForegroundColor Red
}

exit $exitCode
