# Implementation Plan: 005 — Expresión del Avatar

**Branch**: `005-avatar-expression` | **Date**: 2026-08-24 | **Spec**: `specs/005-avatar-expression/spec.md`
**Input**: Spec 005 + 001/002/004

## Summary
Convertir `EmotionReading` → `AvatarExpression` determinista (Neutral fallback), Intensity 0-1, ParametersJson seguro, historial persistido, GET current.

## Technical Context
**Language/Version**: C# 14 / .NET 10
**Primary Dependencies**: System.Text.Json, EF Core 10
**Storage**: Tabla `AvatarExpressions` (SessionId, ReadingId, ExpressionName, Intensity, ParametersJson, IsFallback) índice (SessionId,CreatedAt)
**Testing**: Domain mapping determinista, Infra integration, API GET current
**Target Platform**: ASP.NET Core + Blazor (render Neutral)
**Project Type**: Clean Arch slice
**Performance Goals**: Map <10ms, GET <50ms
**Constraints**: No análisis; no raw provider al cliente; determinismo
**Scale/Scope**: 4 CAs, 1 service domain

## Constitution Check — PASS

## Project Structure
```
src/turning.Domain/Services/AvatarExpressionMapper.cs
src/turning.Application/Interfaces/IAvatarExpressionService
src/turning.Infrastructure/Services/AvatarExpressionService.cs
src/turning.API/Endpoints/AvatarEndpoints.cs GET /api/sessions/{id}/avatar/current
```

## Execution Order
1. Domain mapper + tests determinismo
2. Infra migration + service
3. API endpoint + tests
