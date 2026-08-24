# Implementation Plan: 008 — Repositorio de Resultados

**Branch**: `008-results-repository` | **Date**: 2026-08-24 | **Spec**: `specs/008-results-repository/spec.md`
**Input**: Spec 008 + 002-007

## Summary
Vista consistente autorizada de experimento terminado: sesión + conversación + emociones + expresiones + encuesta + degradedEvents, sin duplicar fuente de verdad. Proyecto como query, con opción materializar ExperimentResults luego.

## Technical Context
**Language/Version**: C# 14 / .NET 10
**Primary Dependencies**: EF Core 10 (proyecciones), AutoMapper opcional
**Storage**: Sin tabla nueva v1 (queries); opcional `ExperimentResults` SessionId único
**Testing**: Integration proyección orden cronológico, contract 403/404, exportación sin secretos
**Target Platform**: ASP.NET Core
**Project Type**: Query slice
**Performance Goals**: GET result <150ms, list page <200ms
**Constraints**: Solo terminal completo; PII filtrada; autorización Investigator/Admin
**Scale/Scope**: 4 CAs

## Constitution Check — PASS

## Project Structure
```
src/turning.Application/Features/Results/Queries/{GetResult,ListResults}
src/turning.API/Endpoints/ResultsEndpoints.cs GET /api/sessions/{id}/results + GET /api/results
```

## Execution Order
1. Application queries + DTOs
2. API endpoints + auth
3. Tests orden + autorización
