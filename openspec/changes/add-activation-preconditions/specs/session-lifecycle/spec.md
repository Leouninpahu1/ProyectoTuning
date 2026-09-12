## ADDED Requirements

### Requirement: Comprobación de precondiciones antes de activar

El sistema SHALL verificar un conjunto configurable de precondiciones antes de
transicionar una sesión de `Created` a `Active`, y SHALL NOT activarla si alguna
falla.

El conjunto cubre la disponibilidad de la base de datos, la de los servicios
dependientes, la de las APIs externas requeridas por la condición de la sesión y
el respeto de los límites de concurrencia configurados.

#### Scenario: Precondiciones satisfechas

- **WHEN** se activa una sesión `Created` y todas las precondiciones se cumplen
- **THEN** la sesión pasa a `Active`

#### Scenario: Proveedor externo no disponible

- **WHEN** la comprobación detecta que un proveedor externo requerido no responde
- **THEN** la sesión permanece en `Created`
- **AND** la respuesta es `503 Service Unavailable`
- **AND** no se calcula `ExpiresAtUtc` ni se inicia el temporizador

#### Scenario: Límite de concurrencia alcanzado

- **WHEN** el número de sesiones `Active` alcanza el límite configurado
- **THEN** la activación se rechaza
- **AND** la sesión permanece en `Created`

#### Scenario: El detalle interno no se filtra

- **WHEN** una precondición falla
- **THEN** la respuesta identifica la precondición que falló
- **AND** no incluye trazas, credenciales ni mensajes crudos del proveedor

### Requirement: Tiempo límite de la comprobación

El sistema SHALL acotar la duración total de la comprobación de precondiciones,
tratando el agotamiento de ese tiempo como fallo.

#### Scenario: La comprobación no responde

- **WHEN** la comprobación supera su tiempo límite
- **THEN** la activación se rechaza
- **AND** la sesión permanece en `Created`

### Requirement: Reintentos con espera creciente

El sistema SHALL reintentar las comprobaciones contra servicios externos con
esperas crecientes antes de darlas por fallidas, sin exceder el tiempo límite
total.

#### Scenario: Fallo transitorio

- **WHEN** una comprobación externa falla una vez y responde en el reintento
- **THEN** la precondición se considera satisfecha
- **AND** la sesión se activa

### Requirement: Conjunto de precondiciones configurable

El sistema SHALL permitir habilitar o deshabilitar cada precondición por
configuración, de modo que un despliegue sin proveedores externos pueda activar
sesiones.

#### Scenario: Comprobación deshabilitada

- **WHEN** la comprobación de un proveedor externo está deshabilitada por configuración
- **THEN** la activación no la evalúa
- **AND** la sesión se activa si el resto de precondiciones se cumplen
