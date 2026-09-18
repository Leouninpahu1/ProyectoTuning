# 005 — Expresión del avatar

## Objetivo

Convertir una emoción normalizada en un estado visual estable que el cliente pueda renderizar.

## Dependencias

Depende de 001, 002 y 004. No analiza audio/video ni accede directamente a un proveedor externo.

## Modelo

AvatarExpression contiene Id, SessionId, EmotionReadingId, ExpressionName, Intensity, ParametersJson, CreatedAtUtc e IsFallback.

## Reglas

- La expresión se deriva de una lectura válida.
- Intensity debe estar entre 0 y 1.
- Toda emoción desconocida usa Neutral como fallback y queda marcada.
- La traducción debe ser determinista para la misma emoción e intensidad.
- El cliente recibe parámetros seguros y no el contenido crudo de la integración.

## Contrato

IAvatarExpressionService.MapAsync(EmotionReading reading) devuelve una expresión. Endpoint: GET /api/sessions/{sessionId}/avatar/current.

## Persistencia

Tabla AvatarExpressions, índice por SessionId y CreatedAtUtc. La expresión actual puede consultarse sin borrar el historial.

## Criterios de aceptación

- CA-AVA-001: Cada lectura válida produce una expresión o un fallback explícito.
- CA-AVA-002: La intensidad siempre está en rango.
- CA-AVA-003: El historial conserva la relación entre emoción y expresión.
- CA-AVA-004: El cliente puede renderizar Neutral aunque falle la traducción.

