# condition-assignment

## Purpose

Asignar exactamente una condición experimental (`Human` o `AI`) a cada sesión
nueva, de forma balanceada, reproducible y auditable, sin que el cliente pueda
forzar el resultado.

Implementada por `BalancedAssignmentService`, se invoca desde la creación de
sesión y no expone endpoint propio.

## Requirements

### Requirement: Asignación única por sesión

El sistema SHALL asignar exactamente una condición por sesión, antes de que la
sesión pueda activarse, y SHALL conservarla inmutable durante toda su vida.

#### Scenario: Cada sesión recibe una condición

- **WHEN** se crea una sesión
- **THEN** existe exactamente una fila en `ConditionAssignments` para esa sesión
- **AND** su condición es `Human` o `AI`

#### Scenario: Reasignación rechazada

- **WHEN** se solicita asignar condición a una sesión que ya tiene una
- **THEN** se devuelve la asignación existente
- **AND** no se crea una segunda asignación

#### Scenario: La condición no cambia tras activar

- **WHEN** una sesión se activa, se cierra o se cancela
- **THEN** su condición permanece igual a la asignada en la creación

### Requirement: Balanceo entre condiciones

El sistema SHALL elegir la condición mediante balanceo por conteo de sesiones por
condición, resolviendo el empate de forma determinista.

#### Scenario: Se compensa el desbalance

- **WHEN** una condición acumula menos sesiones que la otra
- **THEN** la siguiente asignación elige la condición con menor conteo

#### Scenario: Empate determinista

- **WHEN** ambas condiciones tienen el mismo conteo
- **THEN** la elección sigue una regla determinista y reproducible

### Requirement: La preferencia del cliente no es vinculante

El sistema SHALL tratar `preferredCondition` como una señal opcional y SHALL
decidir la condición en el backend.

#### Scenario: Preferencia ignorada por el balanceo

- **WHEN** el cliente solicita una condición y el balanceo indica la contraria
- **THEN** la sesión se crea con la condición decidida por el backend

#### Scenario: Sin preferencia

- **WHEN** el cliente no envía preferencia
- **THEN** la asignación se realiza igualmente por balanceo

### Requirement: Atomicidad con la creación de sesión

El sistema SHALL ejecutar el conteo y la asignación dentro de la misma
transacción que la creación de la sesión, de modo que un fallo del asignador no
deje una sesión huérfana.

#### Scenario: Fallo del asignador

- **WHEN** la asignación falla
- **THEN** no se persiste ninguna sesión
- **AND** se devuelve un error trazable

#### Scenario: Solicitudes concurrentes

- **WHEN** dos creaciones de sesión concurren sobre el mismo conteo
- **THEN** cada sesión recibe su propia asignación
- **AND** no se generan dos asignaciones para una misma sesión

### Requirement: Trazabilidad de la decisión

El sistema SHALL persistir en `ConditionAssignment` los campos `Id`, `SessionId`,
`Condition`, `Strategy`, `Reason` y `CreatedAtUtc`, con índice único sobre
`SessionId`.

#### Scenario: La decisión queda explicada

- **WHEN** se consulta la asignación de una sesión
- **THEN** incluye la estrategia usada y el motivo de la decisión
