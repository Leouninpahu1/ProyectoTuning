# 006 — Encuestas del experimento

## Objetivo

Entregar una encuesta versionada al finalizar una sesión y persistir respuestas asociadas al participante y a la sesión.

## Dependencias

Depende de 001 y 002. El cierre de sesión notifica al servicio; la encuesta no decide el estado de la sesión.

## Modelo

- SurveyDefinition: Id, Code, Version, Name, IsActive.
- SurveyQuestion: Id, SurveyDefinitionId, Code, Text, Type, Required, Order.
- SurveyResponse: Id, SessionId, SurveyDefinitionId, OwnerUserId, StartedAtUtc, SubmittedAtUtc opcional.
- SurveyAnswer: respuesta tipada a una pregunta.

## Reglas

- Solo una respuesta enviada por sesión y definición.
- La versión usada queda congelada aunque la encuesta cambie después.
- Preguntas obligatorias deben responderse antes de enviar.
- Una encuesta no puede asociarse a otra sesión.
- Las sesiones Completed, TimedOut y Cancelled pueden iniciar y enviar la encuesta; la respuesta incluye CompletionStatus para distinguir el motivo de cierre. Esta regla es obligatoria para la primera versión y no queda a decisión de cada módulo.
- Las respuestas no se registran en logs.

## Contratos

GET /api/sessions/{sessionId}/survey entrega definición y preguntas. POST /api/sessions/{sessionId}/survey/responses inicia o envía respuestas.

## Criterios de aceptación

- CA-SUR-001: Una sesión completada devuelve su encuesta vigente.
- CA-SUR-001a: Una sesión TimedOut o Cancelled también devuelve la encuesta vigente y permite enviarla.
- CA-SUR-002: No se puede enviar una respuesta incompleta.
- CA-SUR-003: Un segundo envío devuelve 409.
- CA-SUR-004: Las respuestas quedan consultables por sesión autorizada.
