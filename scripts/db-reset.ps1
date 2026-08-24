param([switch]$Seed = $true)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
Write-Host ">> Eliminando DBs locales..."
Remove-Item -Force "src/turning.API/turning.db" -ErrorAction SilentlyContinue
Remove-Item -Force "src/turning.API/turning.development.db" -ErrorAction SilentlyContinue
Write-Host ">> Aplicando migraciones..."
dotnet ef database update --project src/turning.Infrastructure --startup-project src/turning.API
Write-Host ">> Verificando..."
Get-ChildItem "src/turning.API/*.db" | Format-Table Name, Length
Write-Host ">> Listo. Ejecuta dotnet run --project src/turning.API para seed automatico."
