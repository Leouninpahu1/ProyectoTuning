# 002 — Gestión de sesiones experimentales

## Objetivo

Implementar el ciclo de vida persistente de `ExperimentSession`, con identidad, condición, temporización, concurrencia y trazabilidad suficientes para que los módulos posteriores puedan depender de una sesión consistente.

## Dependencias y límites

- Depende de 001.
- No implementa el algoritmo de asignación (003), emociones (004), encuestas (006), resultados (008) ni tiempo real (009).
- Puede invocar puertos de notificación, pero no depende directamente de proveedores externos.

## Modelo de sesión

```text
ExperimentSession
- Id: Guid
- OwnerUserId: Guid
- SessionCode: string, único, EXP-{8}
- Condition: Human | AI, inmutable
- Status: Created | Active | Completed | TimedOut | Cancelled
- CreatedAtUtc: DateTime
- ActivatedAtUtc: DateTime?
- LastActivityAtUtc: DateTime?
- ExpiresAtUtc: DateTime?
- CompletedAtUtc: DateTime?
- CancelledAtUtc: DateTime?
- CancellationReason: string?
- ConversationTurnCount: int
- EmotionSampleCount: int
- RowVersion: byte[] o mecanismo equivalente de concurrencia
```

Los estados terminales son `Completed`, `TimedOut` y `Cancelled`. Un estado terminal no puede modificarse mediante operaciones normales.

## Transiciones

| Estado actual | Operación | Estado nuevo | Regla |
|---|---|---|---|
| — | Crear | Created | condición asignada y owner válido |
| Created | Activar | Active | solo una activación válida |
| Active | Completar | Completed | cierre normal |
| Active | Expirar | TimedOut | duración o inactividad superada |
| Created/Active | Cancelar | Cancelled | motivo obligatorio y actor autorizado |

## Requisitos funcionales

- **RF-SES-001**: `POST /api/sessions` debe crear una sesión con `201`, `SessionCode` único y condición devuelta por el backend.
- **RF-SES-002**: `POST /api/sessions/{id}/activate` debe permitir únicamente `Created → Active` y guardar `ActivatedAtUtc`.
- **RF-SES-003**: La duración máxima inicial será configurable y por defecto de 300 segundos.
- **RF-SES-004**: La inactividad máxima será configurable y por defecto de 120 segundos; al superarse se usa `TimedOut`.
- **RF-SES-004a**: Cada sesión `Active` debe actualizar `LastActivityAtUtc` al registrar actividad válida. `ExpiresAtUtc` se calcula al activar y ambos timestamps permiten recuperar el scheduler tras reinicio.
- **RF-SES-005**: El scheduler debe recuperar sesiones `Active` después de reiniciar la aplicación usando timestamps persistidos.
- **RF-SES-006**: `GET /api/sessions/{id}` debe devolver estado, condición, timestamps y contadores.
- **RF-SES-007**: `GET /api/participants/{id}/sessions?page=1&pageSize=50` debe listar sesiones autorizadas, ordenadas por creación descendente.
- **RF-SES-008**: `POST /api/sessions/{id}/cancel` debe exigir motivo y autorización de administrador.
- **RF-SES-008a**: `POST /api/sessions/{id}/complete` debe permitir el cierre normal de una sesión `Active`, guardar `CompletedAtUtc` y producir el estado `Completed`.
- **RF-SES-009**: Una transición concurrente debe producir un solo cambio exitoso; el conflicto devuelve `409`.
- **RF-SES-010**: Cada transición debe registrar actor, timestamp UTC y motivo en una bitácora de auditoría.

## Persistencia

SQL Server/EF Core debe incluir `ExperimentSessions` y `SessionAuditEntries`, dentro de migraciones transaccionales. Deben existir índices para `SessionCode`, `(OwnerUserId, CreatedAtUtc)` y `(Status, ActivatedAtUtc)`. Las sesiones no deben quedar sin usuario válido.

SQLite solo puede utilizarse como almacenamiento temporal de interacciones en modo local. Antes de considerar una sesión o resultado válido, sus registros deben confirmarse en SQL Server. El scheduler, la auditoría y las consultas oficiales siempre leen SQL Server.

`SessionAuditEntries` contiene `Id`, `SessionId`, `PreviousStatus`, `NewStatus`, `ActorType`, `ActorId` opcional, `Reason`, `OccurredAtUtc` y `MetadataJson` seguro.

## Contratos

```http
POST /api/sessions
{ "preferredCondition": "AI" }
```

La preferencia es opcional y no obliga al asignador. Respuesta: `sessionId`, `sessionCode`, `condition`, `status`, `createdAtUtc`.

```http
POST /api/sessions/{id}/activate
GET  /api/sessions/{id}
POST /api/sessions/{id}/complete
POST /api/sessions/{id}/cancel
{ "reason": "ParticipantRequest" }
```

## Criterios de aceptación

- **CA-SES-001**: Crear devuelve `Created` y una condición válida.
- **CA-SES-002**: Activar dos veces devuelve `409` en la segunda solicitud.
- **CA-SES-003**: Una sesión activa expirada termina en `TimedOut` y conserva su auditoría.
- **CA-SES-004**: Una sesión activa se recupera correctamente tras reinicio.
- **CA-SES-005**: Dos activaciones concurrentes producen una respuesta exitosa y una `409`.
- **CA-SES-006**: Una sesión cancelada no acepta nuevos turnos ni cambios de condición.
- **CA-SES-007**: El cierre normal `complete` solo acepta sesiones `Active`, registra auditoría y deja `CompletedAtUtc`.
- **CA-SES-008**: Tras reinicio, el scheduler puede decidir `TimedOut` por `ExpiresAtUtc` o `LastActivityAtUtc` sin depender de memoria del proceso. `Completed` solo se obtiene mediante cierre normal.
