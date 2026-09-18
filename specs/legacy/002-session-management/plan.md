# Implementation Plan: 002 — Gestión de Sesiones Experimentales

**Branch**: `002-session-management` | **Date**: 2026-08-24 | **Spec**: `specs/002-session-management/spec.md`
**Input**: Spec 002 v2 (RF-SES-001..010 simplificado) + Spec 001 normativa + Codebase actual (.NET10, Clean Arch, SQLite/TurningDbContext)

## Summary
Implementar el ciclo de vida persistente de `ExperimentSession` (Created→Active→Completed/TimedOut/Cancelled) con identidad inmutable, temporización dual (300s duración + 120s inactividad), concurrencia optimista (RowVersion/409), auditoría y recuperación tras reinicio. El slice cubre Domain→Application→Infrastructure→API sin invadir Assignment/Survey/Emotion, y deja base migrada lista para specs 003-009.

## Technical Context
**Language/Version**: C# 14 / .NET 10
**Primary Dependencies**: ASP.NET Core Web API, EF Core 10 + SQLite, Serilog, JWT Bearer (existente), xUnit 3 (tests)
**Storage**: SQLite local `turning.db` / `turning.development.db` vía `TurningDbContext`; EF Migrations; tablas `ExperimentSessions`, `SessionAuditEntries` (nueva), índices `SessionCode` único, `(OwnerUserId,CreatedAtUtc)`, `(Status,ActivatedAtUtc)`
**Testing**: `dotnet test` — Domain unit, Application unit (handlers), Infrastructure integration (EF InMemory/SQLite), API contract via WebApplicationFactory
**Target Platform**: Linux/Windows server, ASP.NET Core
**Project Type**: Clean Architecture web-service (5 proyectos: Domain/Application/Infrastructure/API/Web — Web solo consume API)
**Performance Goals**: POST create <200ms p95, GET state <50ms, list <100ms, recovery <30s, timer precisión ±1s
**Constraints**: Domain sin refs a infra; puertos en Application; API no expone PII; RowVersion obligatorio; todo timestamp UTC; admin RBAC
**Scale/Scope**: ~10 RFs, 1 aggregate + 1 audit entity, 5 endpoints REST + admin cancel, 1 BackgroundService scheduler, 8 CAs

## Constitution Check
- I. Clean Arch: Domain extiende enum/estado sin infra → PASS
- II. Vertical Slice: 002 no crea Assignment/Survey → PASS (delegación vía eventos)
- III. Contracts First: contratos REST definidos antes de código → PASS
- IV. Quality Gates: tests por capa + `dotnet build` → PASS
- V. Simplicidad: sin Hangfire/Quartz, scheduler in-process → PASS
- Frontend: Web solo vía API → PASS

## Project Structure
### Documentation (this feature)
```
specs/002-session-management/
├── plan.md
├── research.md   (Phase 0 — scheduler & concurrencia)
├── data-model.md (Phase 1 — entities & índices)
├── quickstart.md (Phase 1 — run & verify)
├── contracts/    (Phase 1 — OpenAPI)
└── tasks.md      (Phase 2 — breakdown)
```

### Source Code
```
src/
├── turning.Domain/
│   ├── Entities/ExperimentSession.cs (extender enum + fields + RowVersion + métodos)
│   └── Entities/SessionAuditEntry.cs (nueva)
├── turning.Application/
│   ├── Features/Sessions/Commands/{Create,Activate,Complete,Cancel}
│   ├── Features/Sessions/Queries/{Get,ListByParticipant}
│   ├── Interfaces/ISessionScheduler, IActivityTracker
│   └── DTOs/SessionDtos
├── turning.Infrastructure/
│   ├── Persistence/Migrations/ (2ª migration)
│   ├── Persistence/TurningDbContext.cs (+SessionAuditEntries config)
│   ├── Repositories/ExperimentSessionRepository.cs
│   └── Services/SessionSchedulerService.cs (BackgroundService)
├── turning.API/
│   └── Endpoints/SessionsEndpoints.cs
└── tests/
    ├── turning.Domain.Tests/SessionManagement/
    ├── turning.Application.Tests/Sessions/
    └── turning.Infrastructure.Tests/Sessions/
```

## Complexity Tracking
| Violation | Why Needed | Simpler Alternative Rejected |
|-----------|------------|------------------------------|
| Nueva tabla SessionAuditEntries | Auditoría RF-010 sin contaminar aggregate | Guardar json en ExperimentSession rompe query |
| BackgroundService | Timer persistente + recovery | Hangfire añade infra innecesaria para local |

## Phase 0 — Research (pendiente research.md)
- Q1: RowVersion vs `WHERE Status=Expected` en SQLite — validar concurrencia 409 reproducible
- Q2: BackgroundService + `ActivatedAtUtc/ExpiresAtUtc/LastActivityAtUtc` vs `IHostedService` con polling — recovery <30s
- Q3: Índice `(Status,ActivatedAtUtc)` cardinalidad — medir

## Phase 1 — Design (pendiente)
- data-model.md: ExperimentSession fields + SessionAuditEntry + índices + FK OwnerUserId
- contracts/: OpenAPI para 5 endpoints + error 409/422/403 payloads
- quickstart.md: `dotnet ef database update` + `dotnet run` + curl verify CAs

## Risks & Mitigations
- Timer drift → test con TimeProvider fake; startup recalcula ExpiresAt
- SQLite lock → transacciones cortas, retry 1x, RowVersion
- RBAC bypass → policy `Administrator` en /admin, owner check en GET

## Execution Order (tasks.md preview)
1. Domain: extender ExperimentSession + SessionAuditEntry + tests
2. Infra: migration + context + repo + scheduler + tests
3. Application: handlers/validators/DTOs + tests
4. API: endpoints + auth policies + contract tests
5. E2E: crear→activar→timeout→recovery + `dotnet build` verde
