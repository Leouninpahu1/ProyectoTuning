# 004 — Registro de lecturas emocionales

## Objetivo

Normalizar y persistir lecturas emocionales asociadas a una sesión, sin acoplar el dominio a Hume AI u otro proveedor.

## Dependencias

Depende de 001 y 002. El proveedor externo se conecta mediante IEmotionAnalysisPort; esta especificación no exige una integración concreta.

## Modelo

EmotionReading contiene Id, SessionId, ConversationTurnId opcional, Source (video/audio/text/simulated), Emotion, Score entre 0 y 1, CapturedAtUtc, Provider, ProviderReference opcional e IsDegraded.

## Reglas

- La lectura debe referenciar una sesión existente.
- Score debe estar entre 0 y 1.
- Los datos crudos del proveedor no se exponen al cliente por defecto.
- Si el proveedor falla, la sesión continúa y se puede guardar un evento degradado sin inventar una emoción.
- Las lecturas son inmutables después de persistirse.

## Contrato

    public interface IEmotionAnalysisPort
    {
        Task<EmotionAnalysisResult> AnalyzeAsync(
            EmotionAnalysisRequest request,
            CancellationToken cancellationToken = default);
    }

Endpoint: POST /api/sessions/{sessionId}/emotions.

## Persistencia

Tabla EmotionReadings, índices por SessionId y CapturedAtUtc y por ConversationTurnId cuando exista. La sesión incrementa EmotionSampleCount solo después de guardar correctamente.

## Criterios de aceptación

- CA-EMO-001: No se guarda una lectura para una sesión inexistente.
- CA-EMO-002: Se rechaza un score fuera de rango.
- CA-EMO-003: Un fallo externo no cambia el estado de la sesión.
- CA-EMO-004: Una lectura persistida puede recuperarse ordenada por timestamp.

