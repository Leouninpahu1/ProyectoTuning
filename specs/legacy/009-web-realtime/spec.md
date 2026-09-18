# 009 — Actualización en tiempo real del cliente web

## Objetivo

Actualizar la interfaz Blazor durante una sesión sin romper la frontera turning.Web → turning.API.

## Dependencias

Depende de 002, 007 y 008. La lógica de dominio no depende del transporte elegido.

## Decisión de transporte

La primera versión usa polling HTTP configurable para reducir complejidad. El contrato de eventos se diseña para permitir SignalR posteriormente. WebSocket directo desde el cliente a proveedores externos queda fuera de alcance.

## Eventos

- SessionStateChanged
- ConversationTurnAdded
- EmotionReadingAdded
- AvatarExpressionChanged
- SurveyAvailable
- SessionEnded
- DegradedOperation

Cada evento contiene eventId, sessionId, type, occurredAtUtc y un payload específico.

Los eventos se almacenan en ExperimentEvents con EventId global ordenable, SessionId, Type, OccurredAtUtc, PayloadJson y ExpiresAtUtc. Un DegradedEvent es la proyección tipada de un evento DegradedOperation y no requiere otra tabla. La primera versión conserva eventos durante 30 días. Si el eventId solicitado ya expiró, la API devuelve 410 y el cliente debe recuperar el estado actual mediante REST.

## Reglas

- Los eventos se entregan solo a usuarios autorizados para la sesión.
- eventId permite ignorar duplicados.
- El cliente puede reconectar y consultar el estado actual mediante REST.
- La pérdida de conexión no modifica la sesión.
- Los eventos no contienen secretos ni datos crudos de proveedores.

## Contratos

GET /api/sessions/{sessionId}/events?after={eventId} devuelve eventos paginados. El cliente debe poder continuar desde el último eventId.

## Criterios de aceptación

- CA-RT-001: Un nuevo turno aparece en el cliente después de la siguiente actualización.
- CA-RT-002: Una reconexión no duplica eventos.
- CA-RT-003: Un usuario no autorizado no recibe eventos.
- CA-RT-004: Si el transporte falla, el cliente recupera estado mediante GET /api/sessions/{id}.
- CA-RT-005: Un eventId expirado devuelve 410 y no bloquea la recuperación del estado actual.
