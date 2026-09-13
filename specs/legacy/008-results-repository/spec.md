# 008 — Repositorio de resultados

## Objetivo

Ofrecer una vista consistente y autorizada de los datos de un experimento terminado, sin duplicar la fuente de verdad de cada módulo.

## Dependencias

Depende de 002–007. Lee sesiones, asignaciones, turnos, emociones, expresiones y encuestas.

## Contrato

GET /api/sessions/{sessionId}/results devuelve sesión, conversación, emotionReadings, avatarExpressions, survey y degradedEvents. degradedEvents contiene Id, código, operación, mensaje seguro, occurredAtUtc y retryable.

GET /api/results?from=&to=&condition=&page=1&pageSize=50 requiere rol Investigador o Administrador.

## Reglas

- Solo se devuelven sesiones autorizadas.
- Una sesión activa puede consultarse como resumen, pero el resultado completo exige estado terminal.
- Los resultados deben conservar referencias a las entidades originales.
- Las exportaciones no incluyen contraseñas, tokens ni PII innecesaria.
- El orden de conversación y eventos es cronológico.
- Los eventos degradados se conservan como parte del resultado y se retienen durante el mismo periodo que la sesión.

## Persistencia

Puede implementarse como consultas proyectadas; no se exige una tabla duplicada en la primera versión. Si se materializa ExperimentResults, debe conservar SessionId único y versión de generación.

## Criterios de aceptación

- CA-RES-001: Un resultado incluye todos los módulos disponibles para la sesión.
- CA-RES-002: Un usuario sin autorización recibe 403 o 404 según la política definida.
- CA-RES-003: Los turnos y lecturas se devuelven en orden estable.
- CA-RES-004: Una exportación no expone secretos.
