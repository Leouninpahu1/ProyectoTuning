param()
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
dotnet ef migrations list --project src/turning.Infrastructure --startup-project src/turning.API
Write-Host "`n--- Tablas ---"
sqlite3 "src/turning.API/turning.development.db" ".tables" 2>$null
if (-not $?) {
  Write-Host "(sqlite3 CLI no instalado, usando dotnet)"
  dotnet ef dbcontext info --project src/turning.Infrastructure --startup-project src/turning.API
}
