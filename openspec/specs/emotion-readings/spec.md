# emotion-readings

## Purpose

Normalizar y persistir lecturas emocionales asociadas a una sesión, sin acoplar
el dominio a Hume AI ni a ningún proveedor concreto. El proveedor se conecta
mediante `IEmotionAnalysisPort`.

Cada lectura persistida deriva además una expresión de avatar (ver
`avatar-expression`).

## Requirements

### Requirement: Registro de lectura emocional

El sistema SHALL exponer `POST /api/sessions/{sessionId}/emotions` para registrar
una lectura emocional sobre una sesión existente.

La emoción puede llegar explícita en la solicitud o resolverse consultando el
puerto de análisis.

#### Scenario: Emoción explícita

- **WHEN** la solicitud incluye una emoción
- **THEN** se persiste una lectura con esa emoción y proveedor `direct`
- **AND** la respuesta es `201 Created` con la lectura y la expresión de avatar derivada

#### Scenario: Emoción resuelta por el proveedor

- **WHEN** la solicitud no incluye emoción
- **THEN** el sistema consulta `IEmotionAnalysisPort`
- **AND** persiste la emoción devuelta con su proveedor

#### Scenario: Sesión inexistente

- **WHEN** se registra una lectura sobre una sesión que no existe
- **THEN** la respuesta es `404 Not Found` con código `SESSION_NOT_FOUND`
- **AND** no se persiste ninguna lectura

### Requirement: Rango del score

El sistema SHALL exigir que el score explícito esté entre 0 y 1 inclusive, y
SHALL acotar a ese rango el score devuelto por el proveedor.

#### Scenario: Score fuera de rango

- **WHEN** la solicitud indica un score menor que 0 o mayor que 1
- **THEN** la respuesta es `400 Bad Request` con código `SCORE_RANGE`
- **AND** no se persiste ninguna lectura

#### Scenario: Score del proveedor acotado

- **WHEN** el proveedor devuelve un score fuera del rango
- **THEN** el valor persistido se acota al intervalo de 0 a 1

### Requirement: Degradación del proveedor

El sistema SHALL conservar la sesión intacta cuando el proveedor de análisis
falla, SHALL persistir una lectura marcada como degradada y SHALL registrar un
evento `DegradedOperation` en `ExperimentEvents` con la operación y el proveedor.

#### Scenario: El proveedor falla

- **WHEN** `IEmotionAnalysisPort` lanza una excepción
- **THEN** se persiste una lectura con emoción `neutral`, proveedor `fallback` e `IsDegraded` verdadero
- **AND** se registra un evento `DegradedOperation` para la sesión
- **AND** el estado de la sesión no cambia

### Requirement: Contador de muestras

El sistema SHALL incrementar `EmotionSampleCount` de la sesión al persistir
correctamente una lectura.

#### Scenario: Conteo tras registrar

- **WHEN** se persiste una lectura emocional
- **THEN** `EmotionSampleCount` de la sesión aumenta en uno

### Requirement: Consulta ordenada

El sistema SHALL exponer `GET /api/sessions/{sessionId}/emotions` devolviendo las
lecturas de la sesión ordenadas por `CapturedAtUtc` ascendente.

#### Scenario: Listado cronológico

- **WHEN** se consultan las lecturas de una sesión
- **THEN** vienen ordenadas de la más antigua a la más reciente

### Requirement: Modelo e inmutabilidad de la lectura

El sistema SHALL persistir en `EmotionReading` los campos `Id`, `SessionId`,
`ConversationTurnId` opcional, `Source`, `Emotion`, `Score`, `CapturedAtUtc`,
`Provider`, `ProviderReference` opcional e `IsDegraded`.

Una lectura persistida SHALL NOT modificarse. Los datos crudos del proveedor
SHALL NOT exponerse al cliente.

#### Scenario: Sin datos crudos del proveedor

- **WHEN** el cliente recibe una lectura
- **THEN** la respuesta contiene emoción, score, fuente, proveedor y timestamp
- **AND** no contiene la carga original devuelta por el proveedor

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
