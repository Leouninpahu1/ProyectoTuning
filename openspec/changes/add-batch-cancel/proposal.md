## Why

RF-SES-07 contempla cancelar sesiones en lote y la tarea T014 de la spec 002 lo
dimensiona en hasta 100 por operación. Hoy solo existe la cancelación individual.

Cuando un ensayo se interrumpe —falla el laboratorio, se cae un proveedor, se
detecta un defecto que invalida las sesiones en curso— hay que cerrar decenas de
sesiones una por una. Además de tedioso, deja el conjunto en estado inconsistente
si el proceso se interrumpe a la mitad.

## What Changes

- Nuevo endpoint de cancelación en lote, restringido a `Administrator`, con un
  motivo común obligatorio.
- El lote se acota a 100 sesiones por operación.
- Las sesiones se seleccionan por lista explícita de identificadores o por
  criterio de estado y rango de fechas.
- La operación es parcialmente tolerante: informa por sesión si se canceló o por
  qué no, en lugar de fallar entera por una sesión ya terminada.
- Cada cancelación del lote deja su propia entrada de auditoría, igual que la
  individual.

## Capabilities

### New Capabilities

Ninguna.

### Modified Capabilities

- `session-lifecycle`: aparece la cancelación en lote junto a la individual.

## Impact

**Código afectado:**

- `src/turning.Application/Features/ExperimentSessions/ExperimentSessionService.cs`: caso de uso de cancelación en lote
- `src/turning.API/Controllers/SessionsController.cs`: endpoint nuevo

**Decisión de diseño pendiente:** si el lote es atómico o parcialmente tolerante.
Esta propuesta opta por tolerante, porque el caso real es cerrar lo que se pueda;
un lote atómico fallaría entero por una sola sesión ya cancelada. Conviene
confirmarlo antes de implementar.

**Riesgo:** es una operación destructiva sobre datos experimentales. El criterio
de selección debe ser explícito y la respuesta debe detallar exactamente qué se
canceló.

**Prioridad:** la más baja de los cinco cambios. No bloquea el MVP.
