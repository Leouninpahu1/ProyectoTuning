## Why

RF-SES-10 del plan de trabajo pide validar precondiciones antes de activar una
sesión: base de datos disponible, servicios dependientes en pie, APIs externas
alcanzables y límites de concurrencia respetados. Hoy `activate` solo comprueba
que la sesión esté en `Created`.

El efecto práctico es que una sesión puede activarse y arrancar su temporizador
de 300 segundos con el proveedor de IA caído. El participante consume su sesión
completa contra un sistema que no puede responderle, y ese ensayo se pierde: no
hay forma de repetirlo sin contaminar el balanceo.

## What Changes

- La activación ejecuta una comprobación de precondiciones antes de transicionar
  a `Active`.
- Si alguna precondición falla, la sesión permanece en `Created` y la respuesta
  indica cuál falló, sin filtrar detalles internos del proveedor.
- La comprobación tiene un tiempo límite propio, para no dejar la activación
  colgada de un proveedor que no responde.
- Las comprobaciones contra servicios externos se reintentan con espera creciente
  antes de darse por fallidas.
- El conjunto de precondiciones es configurable: un despliegue sin IA puede
  desactivar esa comprobación.

## Capabilities

### New Capabilities

Ninguna.

### Modified Capabilities

- `session-lifecycle`: la activación queda condicionada a una comprobación previa de precondiciones.

## Impact

**Código afectado:**

- `src/turning.Application/Features/ExperimentSessions/ExperimentSessionService.cs`: la activación invoca la comprobación
- `src/turning.Application/`: interfaz de comprobación de precondiciones, con implementaciones en Infrastructure
- `src/turning.API/Controllers/SessionsController.cs`: nuevo código de respuesta cuando la comprobación falla

**Contrato:** `POST /api/sessions/{id}/activate` puede responder `503` cuando un
proveedor externo no está disponible. Afecta a Sebastian y a Hector.

**Riesgo:** una comprobación mal calibrada bloquea activaciones legítimas. Por eso
el conjunto debe ser configurable y debe poder desactivarse por completo en
desarrollo.

**Dependencia:** conviene aplicarlo después de `harden-session-ownership-filter`,
que es más urgente y toca los mismos servicios.
