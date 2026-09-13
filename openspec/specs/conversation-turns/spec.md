# conversation-turns

## Purpose

Registrar y consultar la conversación de una sesión experimental: turnos
ordenados por secuencia, con generación automática de respuesta cuando la
condición es `AI`, y aislamiento por propietario.

## Requirements

### Requirement: Registro de turno

El sistema SHALL registrar un turno de conversación sobre una sesión propia no
terminal, asignando el siguiente número de secuencia.

La ruta es `POST /api/experiment-sessions/{sessionId}/conversation-turns`.

#### Scenario: Turno válido

- **WHEN** el propietario envía un mensaje no vacío sobre una sesión no terminal
- **THEN** el turno se persiste con `SequenceNumber` igual al anterior más uno
- **AND** se incrementa `ConversationTurnCount` de la sesión

#### Scenario: Mensaje vacío

- **WHEN** el mensaje está vacío o solo contiene espacios
- **THEN** la respuesta es `400 Bad Request` con código `CONVERSATION_EMPTY_MESSAGE`

#### Scenario: Mensaje demasiado largo

- **WHEN** el mensaje supera los 4000 caracteres
- **THEN** la respuesta es `400 Bad Request` con código `CONVERSATION_MESSAGE_TOO_LONG`

#### Scenario: Sesión terminal

- **WHEN** se intenta registrar un turno sobre una sesión `Completed`, `TimedOut` o `Cancelled`
- **THEN** la operación se rechaza con código `SESSION_TERMINAL`

### Requirement: Consulta de la conversación

El sistema SHALL exponer `GET /api/experiment-sessions/{sessionId}/conversation-turns`
devolviendo los turnos de una sesión propia ordenados por número de secuencia
ascendente.

#### Scenario: Listado ordenado

- **WHEN** el propietario consulta la conversación
- **THEN** la respuesta es `200 OK`
- **AND** los turnos vienen del más antiguo al más reciente por `SequenceNumber`

#### Scenario: Conversación vacía

- **WHEN** la sesión no tiene turnos
- **THEN** la respuesta es `200 OK` con una lista vacía

### Requirement: Aislamiento por propietario en la conversación

El sistema SHALL resolver el usuario autenticado desde el token y SHALL responder
`404 Not Found` con código `SESSION_NOT_FOUND` cuando la sesión no existe o
pertenece a otro usuario.

#### Scenario: Conversación de sesión ajena

- **WHEN** un usuario consulta o escribe en la conversación de una sesión ajena
- **THEN** la respuesta es `404 Not Found` con código `SESSION_NOT_FOUND`
- **AND** no se persiste ningún turno

#### Scenario: Token sin identificador de usuario

- **WHEN** el token no permite resolver el usuario autenticado
- **THEN** la respuesta es `401 Unauthorized`

### Requirement: Respuesta generada en condición AI

El sistema SHALL solicitar una respuesta a `ITextGenerationPort` cuando la
condición de la sesión es `AI` y el turno registrado proviene del participante, y
SHALL persistirla como un turno adicional del interlocutor que referencia al
turno que lo originó mediante `OriginatingTurnId`.

En condición `Human` no se genera respuesta automática.

#### Scenario: Sesión AI responde

- **WHEN** el participante registra un turno en una sesión de condición `AI`
- **THEN** se persiste un segundo turno con emisor `Interlocutor`
- **AND** ese turno lleva `OriginatingTurnId` apuntando al turno del participante
- **AND** ocupa el siguiente número de secuencia

#### Scenario: Sesión Human no responde

- **WHEN** el participante registra un turno en una sesión de condición `Human`
- **THEN** solo se persiste el turno del participante

#### Scenario: Fallo del generador

- **WHEN** `ITextGenerationPort` lanza una excepción
- **THEN** el turno del participante permanece persistido
- **AND** el estado de la sesión no cambia
- **AND** la operación devuelve el turno del participante

#### Scenario: Respuesta vacía del generador

- **WHEN** el generador devuelve texto vacío
- **THEN** no se persiste turno de interlocutor

### Requirement: Modelo del turno

El sistema SHALL persistir en `ConversationTurn` los campos `Id`, `SessionId`,
`SequenceNumber`, `Sender`, `Message`, `CreatedAtUtc` y `OriginatingTurnId`
opcional, donde `Sender` es `Participant` o `Interlocutor`.

#### Scenario: Emisor no reconocido

- **WHEN** la solicitud indica un emisor distinto de `Participant` o `Interlocutor`
- **THEN** la operación se rechaza con `400 Bad Request`

#### Scenario: Secuencia sin huecos

- **WHEN** se registran varios turnos consecutivos en una sesión
- **THEN** sus números de secuencia son consecutivos y empiezan en 1
