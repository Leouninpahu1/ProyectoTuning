# Plan Individual — Juan Diego Aguirre Torres

**Rol:** Backend y casos de uso de sesiones / conversación
**Ownership primario (R):** Backend, API de sesiones y turnos
**Apoyo:** Integración API (con Hector), contrato AI (con Gerson)
**Supervisión:** Líder técnico (A)

## Objetivo del MVP personal
Entregar el vertical slice de sesión ejecutable contra SQL Server, sin lógica duplicada en controllers y con autorización por propietario.

## Alcance incluido
- `POST /api/sessions`, `POST /api/sessions/{id}/activate|complete|cancel`, `GET /api/sessions/{id}`, `GET /api/sessions/participant/{id}`
- `POST/GET /api/experiment-sessions/{id}/conversation-turns`
- Validación, manejo de errores HTTP coherente (400/401/403/404/409)
- Autorización: propietario vs Researcher/Administrator
- Integración con `IAssignmentService` y `ITextGenerationPort` (sin implementar modelo)

## Fuera de MVP personal (POST-MVP)
- Entrenamiento AI, WebSocket tiempo real, panel analítico avanzado

## Entregables verificables
| # | Entregable | Evidencia |
|---|---|---|
| JD-01 | Endpoints de sesión con DTOs documentados en Swagger | `src/turning.API/Controllers/SessionsController.cs` + Swagger |
| JD-02 | Activación explícita + expiración (SessionScheduler) | `SessionSchedulerService`, test 409 doble activate |
| JD-03 | Turnos con `OriginatingTurnId` y bloqueo terminal | `ConversationTurnService`, test `IsTerminal` |
| JD-04 | Pruebas de aislamiento por propietario | `dotnet test` + colección Hector |
| JD-05 | Sin acceso directo a `TurningDbContext` desde controllers | Code review Clean Arch |

## Plan 72 horas (24–27 ago)
- Día 1: Revisar contratos `Create/Activate/Complete/Cancel` y corregir `SessionOptions` (300s/120s)
- Día 2: Corregir autorización `participant/{id}` (solo propietario/Researcher/Admin) y errores 409
- Día 3: PR con pruebas de propietario + integración con `BalancedAssignmentService`; demo `register→create→activate` contra SQL Server

## Plan semanal
| Semana | Objetivo | Tarea clave | Demo |
|---|---|---|---|
| 1 (24–30 ago) | Cerrar camino crítico | Contratos + migración `InitialSqlServer` validada | Login → crear sesión |
| 2 (31ago–6sep) | API base segura | Aislamiento por propietario, secretos por env | Dos usuarios ven solo sus sesiones |
| 3 (7–13 sep) | Vertical slice | Sesión Human completa sin IA avanzada | Turnos persistidos |
| 4 (14–20 sep) | Integración experimental | AI baseline via `ITextGenerationPort`, encuesta | Human/AI comparable |
| 5 (21–27 sep) | Estabilización | Logging, manejo errores, seed | Persona externa ejecuta flujo |
| Cierre 28–30 sep | Release | Congelar, tag, diagrama ER | Go/no-go |

## Dependencias
- **Depende de:** Gerson (SQL Server disponible, H3 2 sep), Hector (colección pruebas)
- **Bloquea a:** Sebastian (frontend consume estos endpoints), Gerson (AI necesita sesión activa)

## Definition of Done personal
- [ ] Endpoints probados contra SQL Server (no solo SQLite)
- [ ] `dotnet test` verde para casos de uso de sesión
- [ ] Swagger/colección actualizada, sin `DbContext` en controllers
- [ ] Demo `crear→activar→conversar→completar` reproducible

## Riesgo y mitigación
- **Riesgo:** Fuga de datos entre usuarios → mitigación: test de autorización por endpoint, bloquear release si falla.

## Comando de validación
```powershell
dotnet ef database update --project src/turning.Infrastructure --startup-project src/turning.API --connection "Server=(localdb)\MSSQLLocalDB;Database=Turning;Trusted_Connection=True;TrustServerCertificate=True"
dotnet test
```
