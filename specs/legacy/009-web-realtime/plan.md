# Implementation Plan: 009 — Actualización en Tiempo Real del Cliente Web

**Branch**: `009-web-realtime` | **Date**: 2026-08-24 | **Spec**: `specs/009-web-realtime/spec.md`
**Input**: Spec 009 + 002/007/008

## Summary
Actualizar Blazor sin romper turning.Web→turning.API; v1 polling HTTP configurable, contrato eventos preparado para SignalR futuro. Eventos almacenados 30 días en ExperimentEvents, 410 si expirado.

## Technical Context
**Language/Version**: C# 14 / .NET 10 + Blazor Web App
**Primary Dependencies**: EF Core 10, Blazor, (futuro SignalR)
**Storage**: Tabla `ExperimentEvents` (EventId global ordenable, SessionId, Type, OccurredAtUtc, PayloadJson, ExpiresAtUtc) índice (SessionId,EventId)
**Testing**: Integration polling, contract 410 expiración, E2E reconexión sin duplicados
**Target Platform**: ASP.NET Core + Blazor WASM/Server
**Project Type**: Web slice (API + Web)
**Performance Goals**: GET events <50ms, polling intervalo configurable 1-3s
**Constraints**: No WebSocket directo a providers; eventos sin secretos; dueño autorizado
**Scale/Scope**: 5 CAs, 1 tabla, 7 tipos evento

## Constitution Check
- I. Web solo vía API → PASS
- II. Transporte intercambiable → PASS

## Project Structure
```
src/turning.Infrastructure/Persistence/ExperimentEvents config
src/turning.Application/Interfaces/IEventStore
src/turning.API/Endpoints/EventsEndpoints.cs GET /api/sessions/{id}/events?after=
src/turning.Web/Services/EventPollingService.cs + Components
```

## Execution Order
1. Infra migration ExperimentEvents + IEventStore
2. API GET events con paginación + 410
3. Application event publisher (reusa DegradedOperation)
4. Web polling + reconexión
5. Tests E2E
