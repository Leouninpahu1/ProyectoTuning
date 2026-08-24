# Implementation Plan: 003 — Asignación de Condición Experimental

**Branch**: `003-assignment-service` | **Date**: 2026-08-24 | **Spec**: `specs/003-assignment-service/spec.md`
**Input**: Spec 003 (asignación Human/AI) + Spec 001 limites + Spec 002 sesiones

## Summary
Asignar exactamente una condición por sesión de forma reproducible y auditable, sin que el cliente fuerce el resultado. Estrategia inicial: balanceo por conteo de sesiones por condición con empate determinista, transacción serializable y fila de contador con concurrencia optimista.

## Technical Context
**Language/Version**: C# 14 / .NET 10
**Primary Dependencies**: EF Core 10, Serilog, xUnit 3
**Storage**: Tabla `ConditionAssignments` (SessionId único, Condition, Strategy, Reason) + `AssignmentCounters` (opcional para serialización); transacción con `ExperimentSessions`
**Testing**: Domain unit (estrategia), Application unit (IAssignmentService), Infra integration (concurrencia 409), API contract
**Target Platform**: ASP.NET Core
**Project Type**: Clean Arch slice (Domain/Application/Infrastructure/API)
**Performance Goals**: Assign <50ms p95, retry 1x, 409 determinista en conflicto
**Constraints**: No crea sesión; no expone algoritmo al cliente; no acceso directo a DB desde API
**Scale/Scope**: ~4 CAs, 1 service, 1 port, 2 tablas

## Constitution Check
- I. Domain sin infra → PASS
- II. Slice vertical sin crear sesión → PASS
- III. Contrato IAssignmentService antes de código → PASS
- IV. Tests por capa → PASS
- V. Simplicidad: sin tabla distribuida → PASS

## Project Structure
```
specs/003-assignment-service/
├── plan.md | research.md | data-model.md | quickstart.md | contracts/ | tasks.md
src/
├── turning.Domain/Entities/ConditionAssignment.cs + AssignmentStrategy enum
├── turning.Application/Interfaces/IAssignmentService + DTOs
├── turning.Infrastructure/Services/BalancedAssignmentService.cs + Persistence config
├── turning.API/ (sin endpoint propio; usado vía POST /api/sessions internamente)
└── tests/...
```

## Complexity Tracking
| Violation | Why | Alternative |
|-----------|-----|-------------|
| AssignmentCounters tabla | Serializa conteo en SQLite sin lock pesimista | Lock en memoria no sirve multi-instancia futura |

## Phase 0 — Research
- Q1: SQLite serializable vs `UPDATE counters SET count = count+1 WHERE condition` con RowVersion — elegir RowVersion.

## Phase 1 — Design
- data-model: ConditionAssignments + AssignmentCounters migra
- contracts: IAssignmentService signature
- quickstart: `dotnet test --filter Assignment`

## Execution Order
1. Domain: ConditionAssignment + Strategy
2. Infra: migration + BalancedAssignmentService
3. Application: integration con CreateSessionCommand (transacción)
4. Tests contrato concurrentes
