## Why

El registro público concede el rol que el solicitante pida, incluidos
`Researcher` y `Administrator`. Cualquiera con acceso a la API puede crearse un
usuario privilegiado y, con él, leer las sesiones de todos los participantes y
cancelarlas. Eso anula el aislamiento por propietario y el control por rol de
todas las demás capacidades.

La permisividad fue deliberada y está documentada en `AuthService`: hoy no existe
otra vía de crear usuarios `Researcher` para pruebas. Cerrarla exige antes
habilitar esa vía, por eso el cambio no se ha aplicado.

## What Changes

- El registro público SHALL conceder únicamente el rol `Participant`. Un
  `role` privilegiado en el cuerpo deja de tener efecto.
- Se habilita una vía autorizada de conceder `Researcher` y `Administrator`. La
  forma concreta se decide en `design.md` con el equipo: seed inicial de
  administrador, endpoint restringido a `Administrator`, o comando de
  configuración fuera de la API.
- **BREAKING** para las pruebas y scripts que hoy crean `Researcher` por
  registro público. Hay que migrarlos a la vía nueva antes de cerrar la actual.
- El test `RegisterAsync_WithPrivilegedRole_ShouldGrantIt_PendienteDeRestriccion`
  se reescribe para afirmar lo contrario y pierde el sufijo.

## Capabilities

### New Capabilities

Ninguna.

### Modified Capabilities

- `authentication`: el registro público deja de conceder roles privilegiados y aparece una vía autorizada de asignarlos.

## Impact

**Código afectado:**

- `src/turning.Application/Features/Auth/AuthService.cs`: validación del rol en el registro
- `src/turning.Application/Features/Auth/RegisterRequest.cs`: `Role` deja de ser obligatorio, o desaparece
- `tests/turning.Application.Tests`: el test que hoy documenta la escalada

**Decisión pendiente de equipo.** Este cambio no debe aplicarse sin acordar la vía
de alta de usuarios privilegiados: cerrar el registro sin habilitarla dejaría el
proyecto sin forma de crear un `Researcher`, y bloquearía las demos.

**Dependencia:** `SessionOwnershipFilter` protege las rutas anidadas de sesión,
pero un atacante lo elude registrándose como `Researcher`, porque el filtro exime
a los roles privilegiados. Mientras esta escalada siga abierta, ese aislamiento
no es una garantía real.

**Seguridad:** es el hueco de mayor impacto del proyecto. Mientras siga abierto,
ninguna garantía de autorización es real.
