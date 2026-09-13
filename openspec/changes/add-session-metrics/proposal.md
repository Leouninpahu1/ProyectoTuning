## Why

RF-SES-09 del plan de trabajo pide métricas de sesiones y un panel que las
consuma: balance entre condiciones, latencia y volumen por estado. No existe
ningún endpoint de métricas en el código: la tarea T016 de la spec 002 quedó sin
hacer.

Sin esas métricas nadie detecta que el balanceo 50/50 se está desviando hasta que
el ensayo ya terminó, que es cuando deja de poder corregirse.

## What Changes

- Nueva capacidad `session-metrics` con endpoints de solo lectura bajo
  `/api/metrics/sessions/`.
- Conteo de sesiones por estado y por condición, con el desbalance actual entre
  `Human` y `AI`.
- Latencias observadas de las operaciones de sesión.
- Acceso restringido a `Researcher` y `Administrator`: son datos agregados de
  todos los participantes.
- Las métricas se calculan por consulta agregada sobre las tablas existentes, sin
  tabla nueva ni proceso de materialización.

## Capabilities

### New Capabilities

- `session-metrics`: métricas agregadas del ciclo de vida de sesiones para seguimiento del ensayo.

### Modified Capabilities

Ninguna.

## Impact

**Código nuevo:**

- `src/turning.Application/Features/Metrics/`: consultas agregadas y DTOs
- `src/turning.API/Controllers/MetricsController.cs`

**Rendimiento:** las agregaciones se apoyan en el índice `(Status, ActivatedAtUtc)`
que ya existe. Conviene medir antes de añadir cualquier caché.

**Fuera de alcance:** el panel visual que las consume. Esta capacidad solo expone
los datos.
