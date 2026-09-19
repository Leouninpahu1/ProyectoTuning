# Especificaciones del proyecto Turning

## Orden de implementación

Las especificaciones forman una secuencia de módulos. Todas usan el mismo contrato de sesión y SQL Server como fuente oficial de persistencia mediante EF Core.

| Orden | Especificación | Responsabilidad | Dependencias |
|---|---|---|---|
| 001 | Fundación del experimento | Alcance, arquitectura y vocabulario común | Ninguna |
| 002 | Gestión de sesiones | Ciclo de vida y trazabilidad de una sesión | 001 |
| 003 | Asignación | Decide condición Humano/IA | 001, 002 |
| 004 | Emociones | Persiste lecturas emocionales | 001, 002 |
| 005 | Avatar | Traduce emociones a expresiones | 001, 004 |
| 006 | Encuestas | Entrega y persiste respuestas | 001, 002 |
| 007 | Orquestación | Coordina conversación, IA, emociones y cierre | 002–006 |
| 008 | Resultados | Consulta y exporta resultados | 002–007 |
| 009 | Tiempo real web | Actualiza el cliente durante la sesión | 002, 007, 008 |

## Decisiones comunes

- Plataforma: .NET 10, ASP.NET Core, Blazor y Clean Architecture.
- Persistencia oficial: SQL Server con EF Core, migraciones y transacciones.
- SQLite es opcional únicamente para pruebas, prototipos o un buffer local de interacciones. No es fuente de verdad para sesiones, resultados, auditoría ni datos experimentales.
- Identificadores: `Guid` para entidades; `SessionCode` con formato `EXP-{8}`.
- Tiempo: UTC en persistencia y contratos ISO 8601.
- Condiciones: `Human` e `AI`. El cliente puede solicitar una preferencia, pero el backend decide y persiste la asignación.
- Semántica de condición: `Human` indica interlocutor humano; `AI` indica respuesta generada por IA. No representa tipo de avatar ni proveedor emocional.
- Ciclo de cierre: `POST /api/sessions/{id}/complete` para cierre normal; el scheduler usa `TimedOut`; un administrador usa `Cancelled`.
- Fallos externos: se persisten como `DegradedEvent` con sesión, operación, código, mensaje seguro, timestamp y posibilidad de reintento.
- Si una interacción se almacena temporalmente en SQLite, debe sincronizarse con SQL Server y marcarse como confirmada; una interacción no sincronizada no cuenta como dato experimental válido.
- API: `src/turning.Web` nunca accede directamente a base de datos, repositorios o proveedores externos.
- Eliminación: los registros experimentales son inmutables durante el ensayo; cualquier corrección debe quedar auditada.
- Alcance de la primera entrega: bootstrap de sesión, conversación persistida, asignación, emociones simuladas, avatar derivado, encuesta y consulta de resultados. Integraciones reales con OpenAI/Hume y WebSocket quedan detrás de puertos y adaptadores.

## Convenciones de contrato

- Sesión creada: `POST /api/sessions` → `201 Created`.
- Sesión activa: `POST /api/sessions/{id}/activate`.
- Estado: `GET /api/sessions/{id}`.
- Recursos hijos siempre incluyen `sessionId` y no pueden existir sin una sesión válida.
- Errores: `400` entrada inválida, `401/403` autenticación/autorización, `404` recurso inexistente, `409` conflicto de estado o concurrencia, `422` regla de negocio, `503` proveedor externo no disponible.
