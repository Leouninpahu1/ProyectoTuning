# Implementation Plan: 007 — Orquestación del Experimento

**Branch**: `007-experiment-orchestration` | **Date**: 2026-08-24 | **Spec**: `specs/007-experiment-orchestration/spec.md`
**Input**: Spec 007 + 002-006

## Summary
Coordinar sesión→turnos→IA→emociones→avatar→encuesta vía Application sin acceso directo a EF ni providers. ConversationTurn con OriginatingTurnId y secuencia única;  POST /turns asigna Sender=Participant, orquestador crea Interlocutor.

## Technical Context
**Language/Version**: C# 14 / .NET 10
**Primary Dependencies**: EF Core 10, HttpClient (ITextGenerationPort), Polly (opcional)
**Storage**: Tabla `ConversationTurns` ya existe; añadir `OriginatingTurnId` + índice; añade `DegradedEvents` vía `ExperimentEvents`
**Testing**: Unit orquestador, integration IA mock + degradación, contract 400/422/409, E2E Human vs AI
**Target Platform**: ASP.NET Core
**Project Type**: Clean Arch slice (Application-centric)
**Performance Goals**: POST turn <150ms sin IA, <2s con IA mock
**Constraints**: Puertos en Application, adapters en Infra; terminal no acepta turnos
**Scale/Scope**: 6 CAs, 1 aggregate extension

## Constitution Check
- I. Sin EF en Application → PASS (repo abstraído)
- II. Slice vertical orquesta otros → PASS
- III. Contracts ITextGenerationPort/IEmotionAnalysisPort → PASS

## Project Structure
```
src/turning.Application/Features/Experiments/{TurnCommands}
src/turning.Application/Ports/ITextGenerationPort, IEmotionAnalysisPort, IExperimentEventPublisher
src/turning.Infrastructure/AI/*Adapter + EventPublisher
src/turning.API/Endpoints/ConversationEndpoints.cs
```

## Execution Order
1. Domain: extend ConversationTurn + validation
2. Infra: migration OriginatingTurnId + adapters mock
3. Application: Turn handler + IA branch + degradación
4. API: POST/GET /turns + POST /complete
5. Tests E2E
