## ADDED Requirements

### Requirement: Solicitud de restablecimiento por correo

El sistema SHALL exponer `POST /api/auth/forgot-password` de acceso anónimo, que recibe un correo. Si el correo pertenece a una cuenta, SHALL enviar a esa dirección un correo con un enlace de restablecimiento hacia la Web, válido durante 30 minutos. La respuesta SHALL ser `202 Accepted` con el mismo cuerpo exista o no la cuenta.

#### Scenario: Correo registrado

- **WHEN** se solicita el restablecimiento con el correo de una cuenta existente
- **THEN** la respuesta es `202 Accepted`
- **AND** se envía a ese correo un mensaje con el enlace de restablecimiento y su vigencia

#### Scenario: Correo no registrado

- **WHEN** se solicita el restablecimiento con un correo que no pertenece a ninguna cuenta
- **THEN** la respuesta es `202 Accepted` con el mismo cuerpo que para un correo registrado
- **AND** no se envía ningún correo

#### Scenario: Correo vacío o mal formado

- **WHEN** se solicita el restablecimiento sin correo o con un correo mal formado
- **THEN** la respuesta es `400 Bad Request`

#### Scenario: Solicitudes repetidas

- **WHEN** se piden varios restablecimientos para el mismo correo en menos de 60 segundos
- **THEN** todas las respuestas son `202 Accepted`
- **AND** solo se envía un correo

### Requirement: Restablecimiento con token de un solo uso

El sistema SHALL exponer `POST /api/auth/reset-password` de acceso anónimo, que recibe el token del enlace y una contraseña nueva de al menos 8 caracteres. Con un token válido SHALL reemplazar la contraseña, sin iniciar sesión. El token SHALL dejar de servir al usarse, al expirar o si la contraseña cambia por otra vía. Todo token no válido SHALL dar la misma respuesta.

#### Scenario: Token válido

- **WHEN** se envía un token vigente y una contraseña nueva válida
- **THEN** la contraseña queda reemplazada
- **AND** el inicio de sesión funciona con la contraseña nueva y falla con la anterior
- **AND** la respuesta no incluye un token de sesión

#### Scenario: Token reutilizado

- **WHEN** se vuelve a usar un token que ya cambió la contraseña
- **THEN** la respuesta es `400 Bad Request` con código `AUTH_RESET_TOKEN_INVALID`
- **AND** la contraseña no cambia

#### Scenario: Token expirado o alterado

- **WHEN** se envía un token con más de 30 minutos de emitido, o un token modificado o inventado
- **THEN** la respuesta es `400 Bad Request` con código `AUTH_RESET_TOKEN_INVALID`, idéntica en todos los casos

#### Scenario: Contraseña nueva demasiado corta

- **WHEN** se envía un token válido con una contraseña de menos de 8 caracteres
- **THEN** la respuesta es `400 Bad Request` con código `AUTH_INVALID_REQUEST`
- **AND** el token sigue sirviendo para un intento correcto

### Requirement: Envío seguro del correo de restablecimiento

El sistema SHALL enviar el correo desde una cuenta dedicada al proyecto y configurada por entorno, nunca desde la cuenta personal de un integrante, y sin credenciales en el repositorio ni en `appsettings*.json`. El enlace y el token SHALL viajar solo dentro del correo: no aparecen en respuestas de la API ni en los logs. Un fallo del envío SHALL NOT cambiar la respuesta de la solicitud.

#### Scenario: Fallo del servidor de correo

- **WHEN** el servidor de correo no está configurado, no responde o rechaza el envío
- **THEN** la solicitud responde `202 Accepted` igual que siempre
- **AND** se registra una advertencia que no contiene el enlace ni el token

#### Scenario: El token no se filtra

- **WHEN** se procesa cualquier solicitud de restablecimiento
- **THEN** ni la respuesta de la API ni los logs contienen el enlace ni el token

### Requirement: Opciones de restablecimiento en la Web

La Web SHALL ofrecer en la pantalla de inicio de sesión un enlace «¿Olvidaste tu contraseña?» hacia una página anónima donde se pide el enlace por correo, y una página anónima de restablecimiento que recibe el token desde el enlace. Ambas SHALL mostrar mensajes que no revelen si un correo está registrado.

#### Scenario: Pedir el enlace

- **WHEN** un usuario anónimo escribe un correo y pulsa «Enviar enlace»
- **THEN** la página muestra la misma confirmación genérica exista o no la cuenta, indicando revisar la bandeja de entrada y el correo no deseado

#### Scenario: Cambiar la contraseña desde el enlace

- **WHEN** el usuario abre el enlace del correo, escribe la contraseña nueva dos veces iguales y pulsa «Cambiar contraseña»
- **THEN** la página confirma el cambio y ofrece ir al inicio de sesión

#### Scenario: Enlace no válido

- **WHEN** el usuario abre un enlace expirado, ya usado o alterado y envía el formulario
- **THEN** la página informa que el enlace no es válido y ofrece pedir uno nuevo

#### Scenario: Confirmación distinta

- **WHEN** la contraseña nueva y su confirmación no coinciden, o tienen menos de 8 caracteres
- **THEN** la página no envía la solicitud y muestra el error de validación
