# realtime-events

## Purpose

Permitir que el cliente web refleje lo que ocurre durante una sesión sin romper
la frontera `turning.Web → turning.API`, mediante un flujo de eventos
consultable por polling y reanudable desde el último evento conocido.

El contrato se diseña para admitir SignalR más adelante sin cambiar el dominio.

## Requirements

### Requirement: Consulta incremental de eventos

El sistema SHALL exponer `GET /api/sessions/{sessionId}/events?after={eventId}`
devolviendo los eventos de la sesión posteriores al indicado, ordenados
cronológicamente y acotados a un máximo de 100 por página.

#### Scenario: Primera consulta

- **WHEN** se consultan los eventos de una sesión sin indicar `after`
- **THEN** se devuelven sus eventos desde el más antiguo, ordenados por `OccurredAtUtc`

#### Scenario: Continuación desde el último evento

- **WHEN** se consulta indicando `after` con el último `eventId` recibido
- **THEN** solo se devuelven eventos posteriores a ese
- **AND** el evento indicado en `after` no se repite

#### Scenario: Sin eventos nuevos

- **WHEN** no hay eventos posteriores al indicado
- **THEN** la respuesta es `200 OK` con una lista vacía

#### Scenario: Sesión inexistente

- **WHEN** la sesión no existe
- **THEN** la respuesta es `404 Not Found` con código `SESSION_NOT_FOUND`

### Requirement: Orden total y desempate estable

El sistema SHALL ordenar por `OccurredAtUtc` y SHALL desempatar por identificador
de evento, de modo que dos eventos con el mismo instante tengan un orden estable.

#### Scenario: Eventos simultáneos

- **WHEN** dos eventos comparten `OccurredAtUtc`
- **THEN** su orden relativo es estable entre consultas
- **AND** la continuación con `after` no omite ni repite ninguno de los dos

### Requirement: Expiración de eventos

El sistema SHALL conservar los eventos durante un periodo de retención y SHALL
responder `410 Gone` cuando se solicita continuar desde un evento que ya expiró,
sin impedir que el cliente recupere el estado actual por REST.

#### Scenario: Evento expirado

- **WHEN** se indica en `after` un evento que ya no existe y la sesión sí tiene eventos
- **THEN** la respuesta es `410 Gone` con código `EVENT_EXPIRED`

#### Scenario: Recuperación tras expiración

- **WHEN** el cliente recibe `410 Gone`
- **THEN** puede recuperar el estado vigente mediante `GET /api/sessions/{id}`

### Requirement: Tipos de evento

El sistema SHALL emitir eventos de los tipos `SessionStateChanged`,
`ConversationTurnAdded`, `EmotionReadingAdded`, `AvatarExpressionChanged`,
`SurveyAvailable`, `SessionEnded` y `DegradedOperation`.

Cada evento SHALL incluir identificador, sesión, tipo, `occurredAtUtc` y una
carga específica.

#### Scenario: Evento tipado

- **WHEN** el cliente recibe un evento
- **THEN** incluye su identificador, tipo, instante y carga

### Requirement: Idempotencia en el cliente

El sistema SHALL entregar un identificador único por evento, de modo que el
cliente pueda descartar duplicados tras una reconexión.

#### Scenario: Reconexión sin duplicados

- **WHEN** el cliente reconecta y vuelve a consultar desde su último `eventId`
- **THEN** no recibe eventos que ya había procesado

#### Scenario: La desconexión no altera la sesión

- **WHEN** el cliente pierde la conexión
- **THEN** el estado de la sesión no cambia

### Requirement: Contenido seguro de los eventos

El sistema SHALL persistir los eventos en `ExperimentEvents` con identificador
ordenable, `SessionId`, `Type`, `OccurredAtUtc`, `PayloadJson` y `ExpiresAtUtc`.

La carga SHALL NOT contener secretos ni datos crudos de proveedores externos.

#### Scenario: Carga sin secretos

- **WHEN** se emite cualquier evento
- **THEN** su carga no contiene credenciales, tokens ni respuestas crudas de proveedores

### Requirement: Aislamiento por propietario

El sistema SHALL responder `404 Not Found` con cuerpo
`{"error":"SESSION_NOT_FOUND"}` cuando la sesión indicada en la ruta no existe o
pertenece a otro usuario, de forma indistinguible entre ambos casos.

La regla se aplica en `SessionOwnershipFilter`, un filtro de autorización
registrado globalmente que intercepta toda ruta con parámetro `sessionId` antes
de llegar al controller, y consulta
`IExperimentSessionService.IsSessionAccessibleAsync`.

Los roles `Researcher` y `Administrator` quedan exentos. Un endpoint puede
excluirse explícitamente con `SkipSessionOwnershipAttribute`.

#### Scenario: Sesión ajena

- **WHEN** un usuario opera sobre una sesión de otro usuario
- **THEN** la respuesta es `404 Not Found` con `SESSION_NOT_FOUND`
- **AND** la respuesta es idéntica a la de una sesión inexistente
- **AND** la acción del controller no llega a ejecutarse

#### Scenario: Rol privilegiado

- **WHEN** un `Researcher` o `Administrator` opera sobre una sesión ajena
- **THEN** el filtro no bloquea la petición

#### Scenario: Token sin identificador utilizable

- **WHEN** el token no permite resolver un `Guid` de usuario
- **THEN** la respuesta es `401 Unauthorized`
