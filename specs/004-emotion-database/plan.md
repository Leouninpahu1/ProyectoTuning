# Implementation Plan: 004 — Registro de Lecturas Emocionales

**Branch**: `004-emotion-database` | **Date**: 2026-08-24 | **Spec**: `specs/004-emotion-database/spec.md`
**Input**: Spec 004 + 001 + 002

## Summary
Normalizar y persistir `EmotionReading` asociada a sesión (y opcional turno), desacoplado de Hume AI. Score 0-1, inmutable, sin exponer raw provider, soporte evento degradado.

## Technical Context
**Language/Version**: C# 14 / .NET 10
**Primary Dependencies**: EF Core 10, Serilog
**Storage**: Tabla `EmotionReadings` (SessionId, TurnId?, Source, Emotion, Score, CapturedAtUtc, Provider, IsDegraded) índices (SessionId,CapturedAt) y (TurnId)
**Testing**: Domain validation, Infra integration orden cronológico, API contract 400/409
**Target Platform**: ASP.NET Core
**Project Type**: Clean Arch slice
**Performance Goals**: POST <100ms, query orden <50ms
**Constraints**: IEmotionAnalysisPort en Application, adapter en Infra; no análisis directo aquí
**Scale/Scope**: 4 CAs, 1 port, 1 entity

## Constitution Check — PASS (port/adapter isolation)

## Project Structure
```
src/turning.Domain/Entities/EmotionReading.cs
src/turning.Application/Ports/IEmotionAnalysisPort + Features/Emotions
src/turning.Infrastructure/Persistence/EmotionReadingConfig + Repositories
src/turning.API/Endpoints/EmotionEndpoints.cs POST /api/sessions/{id}/emotions
```

## Phase 0 — Research
- Q1: Source enum valores (video/audio/text/simulated) validar contra Hume mock

## Phase 1 — Design
- data-model: EmotionReading + increment EmotionSampleCount post-save
- contracts: EmotionAnalysisRequest/Result
- quickstart: curl POST emotion + GET ordered

## Execution Order
1. Domain entity + validation
2. Infra migration + repo
3. Application port + handler
4. API endpoint + tests
