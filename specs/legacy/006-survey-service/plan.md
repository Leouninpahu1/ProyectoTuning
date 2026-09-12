# Implementation Plan: 006 — Encuestas del Experimento

**Branch**: `006-survey-service` | **Date**: 2026-08-24 | **Spec**: `specs/006-survey-service/spec.md`
**Input**: Spec 006 + 001/002

## Summary
Encuestas versionadas: `SurveyDefinition/Question` + `SurveyResponse/Answer` con 1 envío por sesión/definición, versión congelada, preguntas requeridas validadas, soporte Completed/TimedOut/Cancelled con CompletionStatus.

## Technical Context
**Language/Version**: C# 14 / .NET 10
**Primary Dependencies**: EF Core 10, FluentValidation
**Storage**: Tablas `SurveyDefinitions`, `SurveyQuestions`, `SurveyResponses`, `SurveyAnswers`; índice único (SessionId,SurveyDefinitionId) en Responses
**Testing**: Domain validation, Infra integration 409 segundo envío, API contract 400 incompleta
**Target Platform**: ASP.NET Core
**Project Type**: Clean Arch slice
**Performance Goals**: GET survey <50ms, POST submit <100ms
**Constraints**: No log respuestas; versión inmutable tras uso
**Scale/Scope**: 4 CAs, 4 entities

## Constitution Check — PASS (definiciones sin sesión ok, respuestas requieren sesión)

## Project Structure
```
src/turning.Domain/Entities/Survey*.cs
src/turning.Application/Features/Surveys/{Queries,Commands}
src/turning.Infrastructure/Persistence/Survey configs
src/turning.API/Endpoints/SurveyEndpoints.cs GET/POST /api/sessions/{id}/survey*
```

## Execution Order
1. Domain entities + validation
2. Infra migration + seed definición activa
3. Application handlers
4. API endpoints + tests
