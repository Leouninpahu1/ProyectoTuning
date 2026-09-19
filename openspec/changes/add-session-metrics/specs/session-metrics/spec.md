## ADDED Requirements

### Requirement: Conteo por estado

El sistema SHALL exponer el número de sesiones agrupadas por estado, sobre un
rango de fechas opcional.

#### Scenario: Conteo global

- **WHEN** un usuario autorizado consulta el conteo por estado sin filtro de fechas
- **THEN** la respuesta incluye el número de sesiones en `Created`, `Active`, `Completed`, `TimedOut` y `Cancelled`

#### Scenario: Conteo por rango

- **WHEN** se consulta indicando fecha inicial y final
- **THEN** solo se cuentan las sesiones creadas dentro de ese rango

### Requirement: Balance entre condiciones

El sistema SHALL exponer el número de sesiones por condición y la diferencia
entre ambas, para vigilar la desviación del balanceo 50/50.

#### Scenario: Balance equilibrado

- **WHEN** hay el mismo número de sesiones `Human` y `AI`
- **THEN** la diferencia reportada es cero

#### Scenario: Balance desviado

- **WHEN** una condición acumula más sesiones que la otra
- **THEN** la respuesta indica el conteo de cada una y la magnitud de la diferencia

#### Scenario: Sin sesiones

- **WHEN** no existe ninguna sesión en el rango consultado
- **THEN** la respuesta es `200 OK` con conteos en cero y no un error

### Requirement: Latencia de operaciones de sesión

El sistema SHALL exponer la latencia observada de las operaciones de sesión, de
modo que pueda contrastarse con los objetivos de rendimiento del proyecto.

#### Scenario: Latencia reportada

- **WHEN** un usuario autorizado consulta las latencias
- **THEN** la respuesta incluye la latencia observada de la creación y de la consulta de sesión

### Requirement: Acceso restringido a las métricas

El sistema SHALL restringir los endpoints de métricas a los roles `Researcher` y
`Administrator`, por tratarse de datos agregados de todos los participantes.

#### Scenario: Acceso sin privilegio

- **WHEN** un `Participant` consulta cualquier endpoint de métricas
- **THEN** la respuesta es `403 Forbidden`

#### Scenario: Acceso sin autenticar

- **WHEN** se consulta sin token válido
- **THEN** la respuesta es `401 Unauthorized`

### Requirement: Métricas derivadas sin tabla propia

El sistema SHALL calcular las métricas por consulta agregada sobre las tablas
existentes y SHALL NOT introducir una tabla de métricas materializada.

#### Scenario: Reflejo inmediato

- **WHEN** cambia el estado de una sesión
- **THEN** la siguiente consulta de métricas refleja ese cambio
