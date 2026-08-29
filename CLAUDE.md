# Contexto del Proyecto — Epistech / ProyectoTuning

> Este archivo es memoria de proyecto para Claude Code (terminal). Se generó a partir
> del Project "Epistech" en claude.ai (docs `01_Juan_Diego_Plan_Trabajo.md` y las specs
> `RF-SES-01` a `RF-SES-10`) y de la estructura real de este repo. Mantenerlo actualizado
> manualmente — no hay sincronización automática con el Project.

## Rol de quien trabaja en este repo
- **Nombre:** Juan Diego Aguirre Torres
- **Rol:** Backend y casos de uso de sesiones / conversación
- **Ownership primario (R):** Backend, API de sesiones y turnos
- **Apoyo:** Integración API (con Hector), contrato AI (con Gerson)
- **Supervisión:** Líder técnico (A)

## Qué es el proyecto
Sistema multimodal para la evaluación de interacciones empáticas humano–IA en entornos
virtuales (test de Turing empático). Es un desarrollo universitario en .NET (solución
`turning.sln`) con arquitectura limpia:

- `src/turning.API` — controllers / endpoints HTTP
- `src/turning.Application` — casos de uso, interfaces, DTOs
- `src/turning.Domain` — entidades de dominio
- `src/turning.Infrastructure` — persistencia, servicios (scheduler, repos)
- `src/turning.Web` — cliente/frontend interno
- `specs/00X-*` — specs por feature (spec.md / plan.md / tasks.md)
- `tests/` — pruebas por capa (Domain, Application, Infrastructure)

## Mi alcance (MVP personal — sesiones)
Vertical slice de sesión ejecutable contra SQL Server, sin lógica duplicada en
controllers y con autorización por propietario.

**Endpoints en mi ownership:**
- `POST /api/sessions`
- `POST /api/sessions/{id}/activate|complete|cancel`
- `GET /api/sessions/{id}`
- `GET /api/sessions/participant/{id}`
- `POST/GET /api/experiment-sessions/{id}/conversation-turns`

**Archivos clave ya existentes en el repo (verificados):**
- `src/turning.API/Controllers/SessionsController.cs`
- `src/turning.API/Controllers/ExperimentSessionsController.cs`
- `src/turning.Application/Features/ExperimentSessions/ExperimentSessionService.cs`
- `src/turning.Application/Features/ExperimentSessions/IExperimentSessionService.cs`
- `src/turning.Application/Features/ExperimentSessions/SessionOptions.cs`
- `src/turning.Application/Features/ExperimentSessions/CreateExperimentSessionRequest.cs`
- `src/turning.Application/Features/ExperimentSessions/ExperimentSessionSnapshot.cs`
- `src/turning.Application/Interfaces/IExperimentSessionRepository.cs`
- `src/turning.Infrastructure/Services/SessionSchedulerService.cs`
- `src/turning.Infrastructure/Repositories/ExperimentSessionRepository.cs`
- `src/turning.Domain/Entities/ExperimentSession.cs`
- `src/turning.Domain/Entities/SessionAuditEntry.cs`
- `specs/002-session-management/{spec.md,plan.md,tasks.md}`

**Fuera de mi MVP personal (post-MVP):** entrenamiento AI, WebSocket en tiempo real
(RF-WEB-*), panel analítico avanzado.

**Reglas de arquitectura:** no acceder a `TurningDbContext` directamente desde
controllers; toda la lógica de negocio va en `turning.Application`, no en los
controllers.

## Requerimientos RF-SES (fuente: Project Epistech)

| Req | Nombre | Puntos clave |
|---|---|---|
| RF-SES-01 | Crear y configurar nueva sesión | GUID v4, asignación balanceada 50/50 vía `AssignmentService` (ventana móvil de 20 sesiones), duración inicial 300s, estado `CREATED` |
| RF-SES-02 | Activar sesión experimental | `CREATED → ACTIVE`, inicia temporizador de 300s, notifica a componentes suscritos |
| RF-SES-03 | Finalizar sesión por temporizador | `ACTIVE → COMPLETED` a los 300s ±1s, dispara `SurveyService` |
| RF-SES-04 | Consultar estado de sesión | `GET /api/sessions/{id}`, respuesta < 50ms, 404 si no existe |
| RF-SES-05 | Recuperar sesiones tras reinicio | Al startup, recalcular tiempo restante de sesiones `ACTIVE` y reprogramar timers (< 30s) |
| RF-SES-06 | Listado de sesiones por participante | `GET /api/participants/{participantId}/sessions`, paginado (50/página), solo Researcher/Admin |
| RF-SES-07 | Cancelación manual | `POST /api/admin/sessions/{id}/cancel` (+ batch-cancel), motivo obligatorio, solo admin, no aplica sobre `COMPLETED` |
| RF-SES-08 | Timeouts de inactividad | Timeout configurable (default 120s), transición a `COMPLETED_TIMEOUT`, no aplica a sesiones < 60s |
| RF-SES-09 | Métricas y dashboard | Endpoints `/api/metrics/sessions/*`, balance de condición, latencia, actualización < 15s |
| RF-SES-10 | Validación de pre-condiciones para activación | Checklist (DB, servicios, APIs externas, límites concurrentes) antes de `ACTIVE`, timeout de validación 10s, reintentos con backoff exponencial |

**Nota:** `SessionOptions` en el repo debe reflejar 300s de duración de sesión y 120s de
timeout de inactividad, según Día 1 del plan (ver abajo).

## Plan de 72 horas (24–27 ago 2026)
1. **Día 1:** revisar contratos `Create/Activate/Complete/Cancel` y corregir
   `SessionOptions` (300s / 120s).
2. **Día 2:** corregir autorización de `GET /api/sessions/participant/{id}`
   (solo propietario o Researcher/Admin) y códigos 409.
3. **Día 3:** PR con pruebas de aislamiento por propietario + integración con
   `BalancedAssignmentService`; demo `register → create → activate` contra SQL Server.

## Plan semanal (resumen)
| Semana | Objetivo | Demo |
|---|---|---|
| 1 (24–30 ago) | Cerrar camino crítico, migración `InitialSqlServer` validada | Login → crear sesión |
| 2 (31 ago–6 sep) | Aislamiento por propietario, secretos por env | Dos usuarios ven solo sus sesiones |
| 3 (7–13 sep) | Vertical slice sesión Human sin IA avanzada | Turnos persistidos |
| 4 (14–20 sep) | AI baseline vía `ITextGenerationPort`, encuesta | Human/AI comparable |
| 5 (21–27 sep) | Logging, manejo de errores, seed | Persona externa ejecuta flujo |
| Cierre (28–30 sep) | Congelar, tag, diagrama ER | Go/no-go |

## Dependencias
- **Depende de:** Gerson (SQL Server disponible ~2 sep), Hector (colección de pruebas)
- **Bloquea a:** Sebastian (frontend consume estos endpoints), Gerson (AI necesita
  sesión activa)

## Definition of Done (mío)
- [ ] Endpoints probados contra SQL Server (no solo SQLite)
- [ ] `dotnet test` verde para casos de uso de sesión
- [ ] Swagger/colección actualizada, sin `DbContext` en controllers
- [ ] Demo `crear → activar → conversar → completar` reproducible

## Riesgo principal
Fuga de datos entre usuarios → mitigación: test de autorización por endpoint;
bloquear release si falla.

## Comandos de validación
```powershell
dotnet ef database update --project src/turning.Infrastructure --startup-project src/turning.API --connection "Server=(localdb)\MSSQLLocalDB;Database=Turning;Trusted_Connection=True;TrustServerCertificate=True"
dotnet test
```

## Convenciones para Claude Code en este repo
- Responder siempre en español neutro (Bogotá), tono directo, sin frases de cortesía.
- No inventar datos: si algo no está en este archivo, en `specs/002-session-management/`
  o en el código, decirlo abiertamente en vez de asumir.
- Antes de tocar `SessionsController` o `ExperimentSessionsController`, revisar
  `ExperimentSessionService` y `SessionOptions` para no duplicar lógica de negocio en
  el controller.
- Cambios de contrato (DTOs en `CreateExperimentSessionRequest` / `ExperimentSessionSnapshot`)
  deben reflejarse en Swagger y, si aplica, notificarse a Sebastian (frontend) y Hector
  (colección de pruebas).
