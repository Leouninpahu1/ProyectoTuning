## ADDED Requirements

### Requirement: Cancelación en lote

El sistema SHALL permitir cancelar varias sesiones en una sola operación,
restringida al rol `Administrator`, exigiendo un motivo común no vacío.

El lote SHALL acotarse a un máximo de 100 sesiones por operación.

#### Scenario: Lote válido

- **WHEN** un administrador cancela un lote de sesiones `Created` o `Active` con un motivo
- **THEN** cada una pasa a `Cancelled` con ese motivo y su `CancelledAtUtc`
- **AND** la respuesta detalla el resultado por sesión

#### Scenario: Lote sin motivo

- **WHEN** se envía un lote sin motivo o con motivo vacío
- **THEN** la respuesta es `400 Bad Request`
- **AND** no se cancela ninguna sesión

#### Scenario: Lote excedido

- **WHEN** el lote supera las 100 sesiones
- **THEN** la respuesta es `400 Bad Request`
- **AND** no se cancela ninguna sesión

#### Scenario: Sin privilegio

- **WHEN** un usuario que no es `Administrator` envía un lote
- **THEN** la respuesta es `403 Forbidden`

### Requirement: Tolerancia parcial del lote

El sistema SHALL cancelar las sesiones del lote que admitan cancelación e
informar del motivo por el que las demás no se cancelaron, sin abortar la
operación completa.

#### Scenario: Lote con sesiones ya terminadas

- **WHEN** el lote incluye sesiones en estado terminal junto a sesiones cancelables
- **THEN** las cancelables pasan a `Cancelled`
- **AND** la respuesta indica, para cada sesión no cancelada, por qué no lo fue

#### Scenario: Lote con identificadores inexistentes

- **WHEN** el lote incluye identificadores que no corresponden a ninguna sesión
- **THEN** el resto del lote se procesa igualmente
- **AND** la respuesta los señala como no encontrados

### Requirement: Selección de las sesiones del lote

El sistema SHALL admitir seleccionar las sesiones por lista explícita de
identificadores o por criterio de estado y rango de fechas de creación.

#### Scenario: Selección por criterio

- **WHEN** se cancela por criterio de estado y rango de fechas
- **THEN** solo se cancelan las sesiones que cumplen ambos
- **AND** el conteo afectado no supera el límite del lote

### Requirement: Auditoría de la cancelación en lote

El sistema SHALL registrar una entrada de auditoría independiente por cada sesión
cancelada dentro del lote, con el actor, el motivo y el instante.

#### Scenario: Rastro por sesión

- **WHEN** se cancelan diez sesiones en un lote
- **THEN** se persisten diez entradas de auditoría, una por sesión
