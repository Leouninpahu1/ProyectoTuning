# session-lifecycle

## Purpose

Ciclo de vida persistente de `ExperimentSession`: creación, activación, cierre,
expiración y cancelación, con identidad estable, temporización recuperable tras
reinicio, concurrencia optimista, auditoría de transiciones y aislamiento por
propietario.

Es la capacidad base del sistema: toda entidad de ejecución cuelga de una sesión.

## Requirements

### Requirement: Creación de sesión

El sistema SHALL crear una sesión asociada al usuario autenticado, con un
`SessionCode` único derivado de su identificador, la condición decidida por el
backend y estado inicial `Created`.

La preferencia de condición enviada por el cliente es opcional y no obliga al
asignador.

#### Scenario: Creación exitosa

- **WHEN** un usuario autenticado envía `POST /api/sessions`
- **THEN** la respuesta es `201 Created`
- **AND** el cuerpo incluye `sessionId`, `sessionCode`, `condition`, `status` y `createdAtUtc`
- **AND** `status` es `Created`
- **AND** `sessionCode` tiene el formato `EXP-` seguido de 8 caracteres hexadecimales en mayúscula

#### Scenario: El cliente no puede imponer la condición

- **WHEN** el cliente envía `preferredCondition` en el cuerpo
- **THEN** el backend decide la condición mediante la capacidad `condition-assignment`
- **AND** la condición persistida puede diferir de la preferida

#### Scenario: Sin autenticación

- **WHEN** se envía `POST /api/sessions` sin token válido
- **THEN** la respuesta es `401 Unauthorized`

### Requirement: Activación

El sistema SHALL permitir únicamente la transición `Created → Active`, registrar
`ActivatedAtUtc` y calcular `ExpiresAtUtc` a partir de la duración configurada.

#### Scenario: Activación válida

- **WHEN** el propietario envía `POST /api/sessions/{id}/activate` sobre una sesión `Created`
- **THEN** el estado pasa a `Active`
- **AND** se persisten `ActivatedAtUtc` y `ExpiresAtUtc`

#### Scenario: Doble activación

- **WHEN** se activa una sesión que ya está `Active`
- **THEN** la respuesta es `409 Conflict`
- **AND** el estado de la sesión no cambia

#### Scenario: Activación sobre estado terminal

- **WHEN** se activa una sesión en estado `Completed`, `TimedOut` o `Cancelled`
- **THEN** la respuesta es `409 Conflict`

### Requirement: Cierre normal

El sistema SHALL permitir cerrar una sesión `Active` mediante
`POST /api/sessions/{id}/complete`, dejando estado `Completed` y `CompletedAtUtc`.

`Completed` solo se alcanza por cierre normal: nunca por expiración ni por
cancelación.

#### Scenario: Cierre de sesión activa

- **WHEN** el propietario envía `POST /api/sessions/{id}/complete` sobre una sesión `Active`
- **THEN** el estado pasa a `Completed`
- **AND** se persiste `CompletedAtUtc`
- **AND** la encuesta queda disponible para esa sesión

#### Scenario: Cierre de sesión no activa

- **WHEN** se intenta cerrar una sesión en `Created` o en estado terminal
- **THEN** la respuesta es `409 Conflict`

### Requirement: Temporización dual

El sistema SHALL aplicar dos límites configurables a cada sesión `Active`: una
duración máxima de sesión y un máximo de inactividad. Ambos por defecto son 300 y
120 segundos respectivamente, definidos en `SessionOptions`.

Al superarse cualquiera de los dos, la sesión pasa a `TimedOut`.

#### Scenario: Expiración por duración

- **WHEN** el instante actual supera `ExpiresAtUtc` de una sesión `Active`
- **THEN** el scheduler transiciona la sesión a `TimedOut`
- **AND** la transición queda auditada

#### Scenario: Expiración por inactividad

- **WHEN** transcurre el máximo de inactividad desde `LastActivityAtUtc`
- **THEN** el scheduler transiciona la sesión a `TimedOut`

#### Scenario: La actividad posterga la inactividad

- **WHEN** se registra actividad válida sobre una sesión `Active`
- **THEN** `LastActivityAtUtc` se actualiza al instante de esa actividad

### Requirement: Recuperación tras reinicio

El sistema SHALL reprogramar los temporizadores de las sesiones `Active` al
arrancar, usando exclusivamente los timestamps persistidos y sin depender de
estado en memoria del proceso anterior.

#### Scenario: Reinicio con sesiones activas

- **WHEN** la aplicación arranca y existen sesiones en estado `Active`
- **THEN** el scheduler recalcula el tiempo restante de cada una desde `ExpiresAtUtc` y `LastActivityAtUtc`
- **AND** reprograma sus temporizadores

#### Scenario: Sesión que expiró durante la caída

- **WHEN** al arrancar una sesión `Active` ya superó `ExpiresAtUtc`
- **THEN** el scheduler la transiciona a `TimedOut`

### Requirement: Consulta de estado

El sistema SHALL exponer `GET /api/sessions/{id}` devolviendo estado, condición,
timestamps y contadores de la sesión, sin exponer `OwnerUserId`, `RowVersion` ni
`IsDeleted`.

#### Scenario: Consulta de sesión propia

- **WHEN** el propietario consulta `GET /api/sessions/{id}`
- **THEN** la respuesta es `200 OK` con el snapshot público de la sesión

#### Scenario: Sesión inexistente

- **WHEN** se consulta un identificador que no corresponde a ninguna sesión
- **THEN** la respuesta es `404 Not Found`

### Requirement: Aislamiento por propietario

El sistema SHALL responder `404 Not Found` cuando un usuario opera sobre una
sesión ajena, de modo indistinguible de una sesión inexistente, para no revelar
la existencia de identificadores de terceros.

Los roles `Researcher` y `Administrator` quedan exentos.

La regla se concentra en `ExperimentSessionService.GetAccessibleSessionAsync` y
cubre la consulta, la activación y el cierre.

#### Scenario: Consulta de sesión ajena

- **WHEN** un usuario consulta la sesión de otro usuario
- **THEN** la respuesta es `404 Not Found`
- **AND** la respuesta es idéntica a la de un identificador inexistente

#### Scenario: Operación sobre sesión ajena

- **WHEN** un usuario intenta activar o cerrar la sesión de otro usuario
- **THEN** la respuesta es `404 Not Found`
- **AND** el estado de la sesión ajena no cambia

#### Scenario: Rol privilegiado

- **WHEN** un usuario con rol `Researcher` o `Administrator` consulta una sesión ajena
- **THEN** la respuesta es `200 OK`

### Requirement: Listado por participante

El sistema SHALL exponer `GET /api/sessions/participant/{participantId}` con
resultados paginados y ordenados por fecha de creación descendente, accesible al
propio participante o a un rol privilegiado.

#### Scenario: Listado propio

- **WHEN** un participante lista sus propias sesiones
- **THEN** la respuesta es `200 OK` con sus sesiones ordenadas de más reciente a más antigua

#### Scenario: Listado ajeno sin privilegio

- **WHEN** un participante lista las sesiones de otro participante
- **THEN** no recibe sesiones ajenas

#### Scenario: Listado ajeno con privilegio

- **WHEN** un `Researcher` o `Administrator` lista las sesiones de un participante
- **THEN** la respuesta es `200 OK` con las sesiones de ese participante

### Requirement: Cancelación

El sistema SHALL permitir cancelar una sesión `Created` o `Active` mediante
`POST /api/sessions/{id}/cancel`, exigiendo un motivo no vacío y autorización de
administrador, dejando estado `Cancelled`, `CancelledAtUtc` y
`CancellationReason`.

Una sesión en estado terminal no puede cancelarse.

#### Scenario: Cancelación válida

- **WHEN** un administrador cancela una sesión `Active` indicando un motivo
- **THEN** el estado pasa a `Cancelled`
- **AND** se persisten `CancelledAtUtc` y `CancellationReason`

#### Scenario: Cancelación sin motivo

- **WHEN** se envía una cancelación sin motivo o con motivo vacío
- **THEN** la respuesta es `400 Bad Request`

#### Scenario: Cancelación de sesión ya terminada

- **WHEN** se intenta cancelar una sesión `Completed`, `TimedOut` o `Cancelled`
- **THEN** la respuesta es `409 Conflict`

### Requirement: Concurrencia optimista

El sistema SHALL usar un token de concurrencia `RowVersion` sobre
`ExperimentSession`, de modo que dos transiciones simultáneas produzcan
exactamente un cambio exitoso.

#### Scenario: Activaciones concurrentes

- **WHEN** dos solicitudes intentan activar la misma sesión `Created` a la vez
- **THEN** una responde con éxito
- **AND** la otra responde `409 Conflict`
- **AND** la sesión queda activada una sola vez

### Requirement: Auditoría de transiciones

El sistema SHALL registrar cada cambio de estado en `SessionAuditEntries` con
`SessionId`, `PreviousStatus`, `NewStatus`, `ActorType`, `ActorId` opcional,
`Reason`, `OccurredAtUtc` y `MetadataJson` sin datos sensibles.

#### Scenario: Toda transición deja rastro

- **WHEN** una sesión cambia de estado por cualquier vía
- **THEN** se persiste una entrada de auditoría con el estado previo, el nuevo, el actor y el instante UTC

#### Scenario: La auditoría sobrevive al cierre

- **WHEN** una sesión alcanza un estado terminal
- **THEN** sus entradas de auditoría permanecen consultables

### Requirement: Modelo de estados

El sistema SHALL usar los estados `Created`, `Active`, `Completed`, `TimedOut` y
`Cancelled`. `Completed`, `TimedOut` y `Cancelled` son terminales y no admiten
operaciones normales.

El identificador `Bootstrapped` existe en el código como alias numérico de
`Created` y no constituye un estado adicional.

#### Scenario: Estado terminal inmutable

- **WHEN** se intenta cualquier transición sobre una sesión en estado terminal
- **THEN** la operación se rechaza y el estado no cambia

### Requirement: Persistencia e índices

El sistema SHALL persistir `ExperimentSessions` y `SessionAuditEntries` en SQL
Server mediante migraciones, con índice único sobre `SessionCode` e índices sobre
`(OwnerUserId, CreatedAtUtc)` y `(Status, ActivatedAtUtc)`. Ninguna sesión puede
quedar sin usuario propietario válido.

#### Scenario: Código de sesión único

- **WHEN** se intenta persistir dos sesiones con el mismo `SessionCode`
- **THEN** la base de datos rechaza la segunda

#### Scenario: Sesión sin propietario

- **WHEN** se intenta crear una sesión con propietario vacío
- **THEN** la operación se rechaza antes de persistir
