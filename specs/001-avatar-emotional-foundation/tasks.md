# Tasks: 001 — Fundación

**Input**: spec.md + plan.md | **Branch**: 001-avatar-emotional-foundation

## Phase 1: Setup
- [x] T001 Verificar entidades 001 existen en Domain (9 entidades)
- [x] T002 Agregar provider `Microsoft.EntityFrameworkCore.SqlServer` a Infrastructure

## Phase 2: Foundational
- [x] T003 Configurar `TurningDbContext` dual-provider (UseSqlServer vs UseSqlite)
- [x] T004 Actualizar `appsettings.json` a SQL Server oficial LocalDB
- [ ] T005 Actualizar `ARCHITECTURE.md` con dual-provider y CA-003
- [ ] T006 Validar `dotnet build` y `dotnet ef migrations list` con ambos providers
- [ ] T007 Test contrato CA-001 naming (Human/AI/Created/Active...) + CA-002 FK SessionId

## Phase 3: Polish
- [ ] T008 Verificar `DegradedEvent` persiste en `ExperimentEvents` tipo DegradedOperation (FR-009)
- [ ] T009 Quickstart: `dotnet run` con SQL Server fallback a SQLite dev
