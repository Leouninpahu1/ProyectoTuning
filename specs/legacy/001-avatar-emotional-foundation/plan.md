# Implementation Plan: 001 — Fundación del Experimento de Avatar Emocional

**Branch**: `001-avatar-emotional-foundation` | **Date**: 2026-08-24 | **Spec**: `specs/001-avatar-emotional-foundation/spec.md`
**Input**: Spec 001 normativa (FR-001..009) + Codebase .NET10 Clean Arch + DB dual-provider

## Summary
Consolidar la fundación transversal: vocabulario `Human/AI`, estados `Created/Active/Completed/TimedOut/Cancelled`, agregado raíz `ExperimentSession`, y persistencia oficial SQL Server (SQLite solo buffer local) con todos los límites FR-001..009 verificables para que 002-009 no redefinan infraestructura.

## Technical Context
**Language/Version**: C# 14 / .NET 10
**Primary Dependencies**: ASP.NET Core, EF Core 10 (SqlServer + Sqlite), Serilog, JWT Bearer, xUnit 3
**Storage**: `TurningDbContext` dual-provider: `UseSqlServer` si connection string contiene `Server=` (oficial), `UseSqlite` fallback (dev/test). Migraciones `InitialCreate`..`AddRemainingDomains` ya aplicadas. Tablas: `ExperimentSessions, ConditionAssignments, ConversationTurns, EmotionReadings, AvatarExpressions, SurveyDefinitions/Questions/Responses/Answers, ExperimentEvents, SessionAuditEntries`
**Testing**: Domain unit (invariantes), Infra integration (EF provider), API contract (CA-001..004)
**Target Platform**: Windows/Linux ASP.NET Core
**Project Type**: Clean Arch web-service (5 proyectos)
**Performance Goals**: N/A para fundación; contratos <50ms
**Constraints**: Domain sin infra, puertos en Application, API solo vía `turning.API`, SQLite no autoritativo (CA-003)
**Scale/Scope**: 9 FRs, 4 CAs, 9 entidades comunes

## Constitution Check
- I. Clean Arch: Domain aislado, puertos en Application → PASS
- II. Vertical Slice: 001 no implementa algoritmo 50/50 ni métricas → PASS
- III. Contracts First: puertos `ITextGenerationPort/IEmotionAnalysisPort` en Application → PASS
- IV. Quality Gates: tests por capa → PASS
- V. Simplicidad: dual-provider sin duplicar DbContext → PASS

## Project Structure
```
specs/001-avatar-emotional-foundation/
├── plan.md | research.md | data-model.md | quickstart.md | contracts/ | tasks.md
src/
├── turning.Domain/Entities/{ExperimentSession, ConditionAssignment, ConversationTurn, EmotionReading, AvatarExpression, Survey*, ExperimentEvent}
├── turning.Application/Interfaces/{IExperimentSessionRepository, IAssignmentService, ITextGenerationPort, IEmotionAnalysisPort}
├── turning.Infrastructure/Persistence/TurningDbContext (+ dual-provider) + Migrations
└── turning.API/Program.cs (Migrate + Seed)
```

## Phase 0 — Research
- Q1: SQL Server LocalDB vs SQLite para CI — LocalDB no garantizado en CI, fallback Sqlite resuelto via `UseSqlServer` condicional.
- Q2: DegradedEvent retención 30 días — `ExperimentEvent.ExpiresAtUtc` + `DegradedOperation` tipo.

## Phase 1 — Design
- data-model.md: ER de 9 entidades con FK SessionId, RowVersion, índices descritos arriba.
- contracts/: `ExperimentSession` vocabulario, `DegradedEvent` shape.

## Execution Order
1. Infra: SqlServer package + dual-provider + appsettings.json oficial
2. Domain: verificar CA-001 naming (alias Bootstrapped=Created)
3. Arch: actualizar ARCHITECTURE.md dual-provider
4. Tests: CA-001..004 contract verification
