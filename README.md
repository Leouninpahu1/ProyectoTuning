# Turning

API .NET para el experimento Human/AI (registro → sesión experimental → conversación → encuesta → resultados).

## Requisitos
- .NET SDK 10 (o el indicado en los `.csproj` del repo)
- SQL Server LocalDB (viene con Visual Studio / SQL Server Express) **o** SQLite como fallback solo para tests
- PowerShell 7+ para correr los scripts de `scripts/`

## Instalación y arranque

```powershell
# 1. Restaurar y compilar
dotnet build turning.sln

# 2. Aplicar la migración contra LocalDB
dotnet ef database update `
  --project src/turning.Infrastructure `
  --startup-project src/turning.API `
  --connection "Server=(localdb)\MSSQLLocalDB;Database=Turning;Trusted_Connection=True;TrustServerCertificate=True"

# 3. Levantar la API
dotnet run --project src/turning.API
```

Con la API corriendo, Swagger queda disponible en `https://localhost:<puerto>/swagger`
y el health check en `GET /api/health`.

### Fallback SQLite (solo para tests, sin LocalDB)
Los tests de integración (`tests/turning.Infrastructure.Tests`) ya usan
`Microsoft.Data.Sqlite` en memoria, por lo que **no** necesitas LocalDB para
correr `dotnet test`. LocalDB solo es obligatorio para levantar la API completa.

## Build y test

```powershell
dotnet build turning.sln --no-restore
dotnet test turning.sln --no-restore
```

Para correr todo el smoke test de release (build + test + migración + health check)
de una sola vez:

```powershell
./scripts/smoke.ps1
```

Para dejar la base de datos en un estado limpio antes de una demo:

```powershell
./scripts/db-reset.ps1
```

## Flujo E2E de referencia
1. `POST /api/auth/register`
2. `POST /api/auth/login`
3. `POST /api/sessions`
4. `POST /api/sessions/{id}/activate`
5. `POST /api/experiment-sessions/{id}/conversation-turns`
6. `POST /api/sessions/{id}/complete`
7. `GET /api/sessions/{id}/survey` → `POST /api/sessions/{id}/survey/responses`
8. `GET /api/sessions/{id}/results`

Colección lista para importar en Postman/Insomnia: `docs/api/collection.json`.

## Checklist de release
Ver `docs/checklist.md` antes de cada demo o entrega semanal.

## Contribuir
- Sigue la estructura de `specs/` para nuevas features (spec-driven).
- Corre `./scripts/smoke.ps1` antes de abrir un PR.
- Actualiza `docs/checklist.md` con la evidencia de tu entrega.
