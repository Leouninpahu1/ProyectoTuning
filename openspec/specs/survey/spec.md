# survey

## Purpose

Entregar una encuesta versionada cuando una sesión termina y persistir la
respuesta asociada a esa sesión y a su participante.

La definición de encuesta es reutilizable y existe sin sesión; la respuesta
siempre pertenece a una sesión.

## Requirements

### Requirement: Disponibilidad ligada al cierre

El sistema SHALL entregar la encuesta únicamente cuando la sesión ha terminado,
sea por cierre normal, expiración o cancelación.

Una sesión `Created` o `Active` no tiene encuesta disponible.

#### Scenario: Sesión completada

- **WHEN** se consulta `GET /api/sessions/{sessionId}/survey` sobre una sesión `Completed`
- **THEN** la respuesta es `200 OK` con la definición vigente y sus preguntas

#### Scenario: Sesión expirada o cancelada

- **WHEN** la sesión está en `TimedOut` o `Cancelled`
- **THEN** la encuesta vigente también se entrega y admite envío

#### Scenario: Sesión aún en curso

- **WHEN** la sesión está en `Created` o `Active`
- **THEN** la respuesta es `400 Bad Request` con código `SURVEY_NOT_AVAILABLE`

#### Scenario: Sesión inexistente

- **WHEN** la sesión no existe
- **THEN** la respuesta es `404 Not Found` con código `SESSION_NOT_FOUND`

### Requirement: Definición vigente y versionada

El sistema SHALL entregar la definición activa con sus preguntas ordenadas por el
campo `Order`, incluyendo código, versión, texto, tipo y obligatoriedad de cada
pregunta.

Si no existe ninguna definición activa, el sistema SHALL crear una definición por
defecto y entregarla.

#### Scenario: Preguntas ordenadas

- **WHEN** se entrega la definición de encuesta
- **THEN** sus preguntas vienen ordenadas de forma ascendente por `Order`

#### Scenario: Sin definición activa

- **WHEN** no existe ninguna definición marcada como activa
- **THEN** el sistema crea una definición por defecto y la entrega

### Requirement: Envío único por sesión y definición

El sistema SHALL admitir a lo sumo un envío por combinación de sesión y
definición de encuesta. El primer envío inicia la respuesta; el segundo la marca
como enviada; un envío posterior se rechaza.

#### Scenario: Inicio de la respuesta

- **WHEN** se envía por primera vez `POST /api/sessions/{sessionId}/survey/responses`
- **THEN** se crea una respuesta con `StartedAtUtc` y sin `SubmittedAtUtc`
- **AND** se devuelve su identificador

#### Scenario: Envío definitivo

- **WHEN** se envía sobre una respuesta ya iniciada y no enviada
- **THEN** se registra `SubmittedAtUtc`

#### Scenario: Segundo envío

- **WHEN** se envía sobre una respuesta que ya tiene `SubmittedAtUtc`
- **THEN** la respuesta es `409 Conflict` con código `ALREADY_SUBMITTED`

#### Scenario: Definición inexistente

- **WHEN** la solicitud referencia una definición que no existe
- **THEN** la respuesta es `400 Bad Request` con código `SURVEY_NOT_FOUND`

### Requirement: Vínculo con sesión y participante

El sistema SHALL asociar cada respuesta a la sesión indicada y al propietario de
esa sesión, y SHALL NOT permitir reasociarla a otra sesión.

#### Scenario: Propietario heredado de la sesión

- **WHEN** se crea una respuesta de encuesta
- **THEN** su `OwnerUserId` es el propietario de la sesión

### Requirement: Confidencialidad de las respuestas

El sistema SHALL NOT escribir el contenido de las respuestas de encuesta en los
registros de log.

#### Scenario: Envío no registrado en logs

- **WHEN** se envía una respuesta de encuesta
- **THEN** los logs no contienen el contenido de las respuestas

### Requirement: Modelo de encuesta

El sistema SHALL persistir `SurveyDefinition` (`Id`, `Code`, `Version`, `Name`,
`IsActive`), `SurveyQuestion` (`Id`, `SurveyDefinitionId`, `Code`, `Text`,
`Type`, `Required`, `Order`), `SurveyResponse` (`Id`, `SessionId`,
`SurveyDefinitionId`, `OwnerUserId`, `StartedAtUtc`, `SubmittedAtUtc` opcional) y
`SurveyAnswer` como respuesta tipada a una pregunta.

#### Scenario: Versión congelada

- **WHEN** la definición de encuesta cambia después de que una respuesta la usó
- **THEN** la respuesta conserva la referencia a la definición y versión con la que se respondió

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
