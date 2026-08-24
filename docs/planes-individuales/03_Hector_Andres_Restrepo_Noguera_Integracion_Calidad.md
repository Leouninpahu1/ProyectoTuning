# Plan Individual — Hector Andres Restrepo Noguera

**Rol:** Integración, calidad, pruebas E2E e infraestructura
**Ownership primario (R):** Infraestructura, pruebas, integración, documentación de ejecución
**Apoyo:** Backend (con Juan), AI (con Gerson)
**Supervisión:** Líder técnico (A)

## Objetivo del MVP personal
Garantizar que build, tests, migración, arranque y camino feliz sean reproducibles y que la autorización no filtre datos.

## Alcance incluido
- Colección de requests (Postman/REST), smoke test, checklist release, README reproducible
- Pruebas de autorización por endpoint, pruebas E2E Human/AI, health/readiness
- Documentación de comandos de validación, logs y seed

## Fuera del MVP personal
- Escalamiento horizontal, despliegue cloud HA

## Entregables verificables
| # | Entregable | Evidencia |
|---|---|---|
| HE-01 | Colección API versionada (register→create→activate→turns→complete→survey→results) | `docs/api/collection.json` |
| HE-02 | Smoke test script `scripts/smoke.ps1` + `scripts/db-reset.ps1` | `scripts/` |
| HE-03 | Checklist release (build/test/migrate/seed/demo) | `docs/checklist.md` |
| HE-04 | README reproducible (comandos + SQL Server) | `README.md` actualizado |
| HE-05 | Pruebas E2E de aislamiento (usuario A no ve sesión de B) | Test report |

## Plan 72 horas (24–27 ago)
- Día 1: Crear `docs/api/collection.json` con 12 requests del E2E MVP; probar contra Swagger
- Día 2: Script `scripts/smoke.ps1` que hace `dotnet build && dotnet test && dotnet ef database update` y valida `GET /api/health`
- Día 3: Documentar en `README` pasos para levantar SQL Server LocalDB y fallback SQLite test

## Plan semanal
| Semana | Objetivo | Tarea clave | Demo |
|---|---|---|---|
| 1 (24–30 ago) | Cierre técnico | Migración aplicada, colección lista | Migración ok |
| 2 (31ago–6sep) | API base | Test aislamiento, health DB | Health/readiness verde |
| 3 (7–13 sep) | Vertical slice | E2E Human sin IA avanzada | Sesión persistida inicio-fin |
| 4 (14–20 sep) | Integración | E2E AI+emoción+encuesta, logs | Human/AI comparable |
| 5 (21–27 sep) | Estabilización | Seed idempotente, logging estructurado | Externa ejecuta flujo |
| Cierre 28–30 sep | Entrega | Release tag, script SQL, diagrama ER | Go/no-go |

## Dependencias
- **Depende de:** Gerson (DB), Juan (endpoints)
- **Bloquea a:** Líder (Go/no-go), Sebastian (UI necesita colección)

## Definition of Done personal
- [ ] `dotnet build && dotnet test && dotnet ef database update` documentados y verdes
- [ ] Camino feliz reproducible sin intervención manual oculta
- [ ] Autorización probada por endpoint (403/404 correcto)

## Riesgo y mitigación
- **Riesgo:** SQL Server no disponible en CI → contingencia: SQLite solo para tests, instancia alternativa documentada.

## Comandos de validación (evidencia H3/H4)
```powershell
$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet build turning.sln --no-restore
dotnet test turning.sln --no-restore
dotnet ef database update --project src/turning.Infrastructure --startup-project src/turning.API --connection "Server=(localdb)\MSSQLLocalDB;Database=Turning;Trusted_Connection=True;TrustServerCertificate=True"
```
