# Tasks: 002 — Gestión de Sesiones Experimentales

**Input**: `spec.md` (RF-SES-001..010) + `plan.md` | **Prerequisites**: plan.md ✓ | **Branch**: `specs-all-plans`

## Phase 1: Setup
- [ ] T001 Verificar `TurningDbContext` actual y migration `InitialCreate` aplicada
- [ ] T002 Configurar `appsettings.json` (300s duración, 120s inactividad) y validar Serilog

## Phase 2: Foundational (blocking)
- [ ] T003 [P] Domain: extender `ExperimentSessionStatus` con `Cancelled,TimedOut` + fields `ActivatedAtUtc,ExpiresAtUtc,LastActivityAtUtc,CompletedAtUtc,CancelledAtUtc,CancellationReason,RowVersion`
- [ ] T004 [P] Domain: crear `SessionAuditEntry` + tests state machine
- [ ] T005 Infra: migration `AddSessionManagement` + `TurningDbContext` config + índices
- [ ] T006 Infra: `ISessionScheduler` + `SessionSchedulerService : BackgroundService` (poll 1s, recovery <30s)

## Phase 3: US1 — Crear/Activar/Consultar (P1)
- [ ] T007 [P] Test: contract POST /api/sessions →201 + GET /api/sessions/{id} <50ms
- [ ] T008 Application: `CreateSessionCommand` (integra IAssignmentService mock) + `ActivateSessionCommand` + validators
- [ ] T009 API: `SessionsEndpoints` POST/GET/PATCH activate + 409/404 handling
- [ ] T010 Test: activación doble →409, concurrencia RowVersion

## Phase 4: US2 — Expiración/Recuperación (P2)
- [ ] T011 Test: scheduler expira Active→TimedOut ±1s
- [ ] T012 Impl: `ExpiresAtUtc` calc + `LastActivityAtUtc` update + recovery on startup
- [ ] T013 Test: reinicio simula TimeProvider fake → timers reprogramados

## Phase 5: US3 — Cancelación/Listado/Métricas (P3)
- [ ] T014 Test: POST /admin/cancel + batch ≤100 + GET /participants/{id}/sessions paginado
- [ ] T015 Impl: cancel/complete endpoints + RBAC Administrator + batch criteria
- [ ] T016 Impl: metrics GET /api/metrics/sessions/* (agregación básica)

## Phase 6: Polish
- [ ] T017 `dotnet build` + `dotnet test` verdes + `scripts/db-reset.ps1` verifica
- [ ] T018 quickstart.md validación manual curl

**Parallel**: T003,T004 | T007 | **Checkpoint**: Foundation (T005,T006) bloquea US
