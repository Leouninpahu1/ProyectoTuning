# 007 — Orquestación del experimento

## Objetivo

Coordinar sesión, conversación, asignación, emociones, avatar y encuesta mediante casos de uso de Application.

## Dependencias

Depende de 002, 003, 004, 005 y 006. No contiene acceso directo a EF Core ni a proveedores externos.

## Flujo

1. Crear sesión y obtener asignación.
2. Activar sesión cuando las precondiciones estén listas.
3. Registrar cada turno con secuencia única.
4. Si la condición es AI, solicitar respuesta mediante ITextGenerationPort.
5. Analizar señales mediante IEmotionAnalysisPort cuando existan.
6. Crear la expresión del avatar.
7. Cerrar la sesión y solicitar la encuesta.

## Conversación

ConversationTurn contiene Id, SessionId, SequenceNumber, Sender (Participant o Interlocutor), Message, CreatedAtUtc y OriginatingTurnId opcional. OriginatingTurnId relaciona una respuesta de IA con el turno que la provocó.

Endpoint: POST /api/sessions/{sessionId}/turns con { "message": "..." }. El servidor asigna Sender=Participant; solo el orquestador interno puede crear Sender=Interlocutor. La respuesta HTTP 201 devuelve Id, SessionId, SequenceNumber, Sender, Message, OriginatingTurnId y CreatedAtUtc. GET /api/sessions/{sessionId}/turns devuelve la conversación ordenada.

Un mensaje vacío se rechaza con 400, un mensaje mayor a 4000 caracteres con 422 y un turno sobre sesión terminal con 409.

## Reglas

- Todas las operaciones reciben sessionId y validan ownership/autorización.
- Un fallo de IA o emociones no elimina turnos ni sesión.
- Las respuestas generadas se guardan como turnos de Interlocutor con referencia al turno que las originó.
- Las operaciones críticas usan transacción y control de concurrencia.
- Una sesión terminal no acepta nuevos turnos.
- El cierre normal usa `POST /api/sessions/{id}/complete`; el scheduler usa `TimedOut` y cancelación usa `Cancelled`.
- Cualquier fallo degradado se registra como `DegradedEvent` y no se oculta en la respuesta de operación.

## Puertos

ITextGenerationPort, IEmotionAnalysisPort, IExperimentEventPublisher e ISessionRepository viven en Application. Sus implementaciones viven en Infrastructure.

## Criterios de aceptación

- CA-ORC-001: Una sesión Human registra los turnos enviados sin generar respuesta automática.
- CA-ORC-002: Una sesión AI puede registrar respuesta generada y su trazabilidad.
- CA-ORC-003: Un fallo externo conserva los datos ya persistidos y marca degradación.
- CA-ORC-004: No se aceptan turnos después de completar, expirar o cancelar.
- CA-ORC-005: Una respuesta AI contiene OriginatingTurnId y conserva la secuencia de conversación.
- CA-ORC-006: POST /complete cierra normalmente la sesión y deja disponible la encuesta.
