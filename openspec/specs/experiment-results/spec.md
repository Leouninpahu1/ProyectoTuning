# experiment-results

## Purpose

Ofrecer una vista consolidada y autorizada de los datos de una sesión —
conversación, emociones, expresiones, encuesta y eventos degradados — y un
listado transversal para el equipo de investigación.

Se implementa como proyección sobre las tablas de cada módulo, sin duplicar la
fuente de verdad.

## Requirements

### Requirement: Resultado consolidado de una sesión

El sistema SHALL exponer `GET /api/sessions/{sessionId}/results` devolviendo el
snapshot de la sesión junto con su conversación, lecturas emocionales,
expresiones de avatar, respuestas de encuesta y eventos degradados.

#### Scenario: Resultado completo

- **WHEN** se consulta el resultado de una sesión existente
- **THEN** la respuesta es `200 OK`
- **AND** incluye la sesión, la conversación, las lecturas emocionales, las expresiones de avatar, la encuesta y los eventos degradados

#### Scenario: Módulo sin datos

- **WHEN** la sesión no tiene lecturas emocionales ni encuesta
- **THEN** esas secciones vienen vacías y el resto del resultado se entrega igual

#### Scenario: Sesión inexistente

- **WHEN** se consulta el resultado de una sesión que no existe
- **THEN** la respuesta es `404 Not Found`

### Requirement: Orden estable de las colecciones

El sistema SHALL devolver la conversación ordenada por `SequenceNumber`, las
lecturas emocionales por `CapturedAtUtc`, las expresiones de avatar por
`CreatedAt` y los eventos degradados por `OccurredAtUtc`, todos ascendentes.

#### Scenario: Orden cronológico reproducible

- **WHEN** se consulta el mismo resultado dos veces sin cambios intermedios
- **THEN** las colecciones vienen en el mismo orden en ambas consultas

### Requirement: Eventos degradados en el resultado

El sistema SHALL incluir en el resultado los eventos de tipo `DegradedOperation`
de la sesión, con identificador, código, operación, mensaje seguro,
`occurredAtUtc` y si admite reintento.

#### Scenario: Degradación visible

- **WHEN** durante la sesión falló un proveedor externo
- **THEN** el resultado incluye el evento degradado correspondiente

### Requirement: Listado transversal restringido

El sistema SHALL exponer `GET /api/results` con filtros por rango de fechas y
condición, y paginación, restringido a los roles `Researcher` y `Administrator`.

A diferencia del resultado individual, esta consulta no se limita a las sesiones
propias.

#### Scenario: Consulta con rol privilegiado

- **WHEN** un `Researcher` o `Administrator` consulta el listado
- **THEN** la respuesta es `200 OK` con la página solicitada

#### Scenario: Consulta sin rol privilegiado

- **WHEN** un usuario sin esos roles consulta el listado
- **THEN** la respuesta es `403 Forbidden`

#### Scenario: Sin autenticación

- **WHEN** se consulta sin token válido
- **THEN** la respuesta es `401 Unauthorized`

### Requirement: Contratos públicos sin datos internos

El sistema SHALL devolver contratos públicos y SHALL NOT exponer `OwnerUserId`,
`RowVersion`, `IsDeleted`, contraseñas, tokens ni enums numéricos. Los enums se
entregan como texto.

#### Scenario: Exportación sin secretos

- **WHEN** se obtiene cualquier resultado o listado
- **THEN** la respuesta no contiene credenciales, tokens ni campos internos de persistencia

### Requirement: Sin duplicación de la fuente de verdad

El sistema SHALL construir el resultado como proyección de las tablas de cada
módulo. Si en el futuro se materializa una tabla de resultados, SHALL conservar
`SessionId` único y una versión de generación.

#### Scenario: Lectura derivada

- **WHEN** cambia un dato en el módulo de origen
- **THEN** la siguiente consulta de resultado refleja ese cambio sin requerir regeneración

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
