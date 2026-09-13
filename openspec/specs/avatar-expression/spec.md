# avatar-expression

## Purpose

Convertir una lectura emocional normalizada en un estado visual estable que el
cliente pueda renderizar, de forma determinista y con fallback explícito para
emociones desconocidas.

## Requirements

### Requirement: Derivación determinista

El sistema SHALL derivar una expresión a partir de una lectura emocional
existente, de forma determinista: la misma emoción y el mismo score producen
siempre la misma expresión.

El mapa vigente traduce `joy`, `sadness`, `anger` y `surprise` a `Joy`,
`Sadness`, `Anger` y `Surprise` respectivamente, sin distinguir mayúsculas.

#### Scenario: Emoción conocida

- **WHEN** se persiste una lectura con emoción `joy`
- **THEN** se deriva una expresión `Joy`
- **AND** no se marca como fallback

#### Scenario: Determinismo

- **WHEN** se derivan dos expresiones desde la misma emoción y el mismo score
- **THEN** ambas tienen el mismo nombre de expresión y la misma intensidad

### Requirement: Fallback a Neutral

El sistema SHALL usar `Neutral` para toda emoción no reconocida y SHALL marcar
esa expresión como fallback.

Una lectura cuya emoción es literalmente `neutral` produce `Neutral` sin marca de
fallback.

#### Scenario: Emoción desconocida

- **WHEN** la lectura trae una emoción fuera del mapa conocido
- **THEN** la expresión derivada es `Neutral`
- **AND** queda marcada con `IsFallback` verdadero

#### Scenario: Neutral genuino

- **WHEN** la lectura trae la emoción `neutral`
- **THEN** la expresión derivada es `Neutral`
- **AND** `IsFallback` es falso

### Requirement: Rango de intensidad

El sistema SHALL acotar la intensidad de la expresión al intervalo de 0 a 1,
tomándola del score de la lectura.

#### Scenario: Intensidad siempre en rango

- **WHEN** se deriva una expresión desde cualquier lectura
- **THEN** su intensidad está entre 0 y 1 inclusive

### Requirement: Consulta de la expresión actual

El sistema SHALL exponer `GET /api/sessions/{sessionId}/avatar/current`
devolviendo la expresión más reciente de la sesión, sin borrar el historial.

#### Scenario: Expresión vigente

- **WHEN** se consulta la expresión actual de una sesión con varias expresiones
- **THEN** se devuelve la de `CreatedAt` más reciente

#### Scenario: Sesión sin expresiones

- **WHEN** se consulta la expresión actual de una sesión que no tiene ninguna
- **THEN** el cliente recibe una respuesta que puede renderizar como `Neutral`

### Requirement: Historial y trazabilidad

El sistema SHALL persistir en `AvatarExpressions` los campos `Id`, `SessionId`,
`EmotionReadingId`, `ExpressionName`, `Intensity`, `ParametersJson`, `CreatedAt`
e `IsFallback`, conservando la relación entre cada emoción y su expresión.

Los parámetros entregados al cliente SHALL NOT contener contenido crudo del
proveedor de análisis.

#### Scenario: Trazabilidad hasta la lectura

- **WHEN** se consulta una expresión del historial
- **THEN** su `EmotionReadingId` identifica la lectura de la que se derivó

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
