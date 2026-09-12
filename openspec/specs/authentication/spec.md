# authentication

## Purpose

Registrar usuarios, autenticarlos con JWT y exponer la identidad del usuario en
curso. Es la base del aislamiento por propietario y del control por rol que
aplican el resto de capacidades.

Esta capacidad no tenía spec previa: se redactó desde el código durante la
migración a OpenSpec del 2026-09-12.

## Requirements

### Requirement: Registro de usuario

El sistema SHALL exponer `POST /api/auth/register` de acceso anónimo, exigiendo
nombre completo, correo, contraseña y rol, y SHALL devolver un token de sesión
utilizable de inmediato.

#### Scenario: Registro exitoso

- **WHEN** se registra un correo que no existe, con datos válidos
- **THEN** el usuario queda persistido
- **AND** la respuesta incluye un token JWT y los datos del usuario

#### Scenario: Correo ya registrado

- **WHEN** se registra un correo que ya existe
- **THEN** la respuesta es `409 Conflict`

#### Scenario: Datos inválidos

- **WHEN** falta algún campo obligatorio o el rol viene vacío
- **THEN** la respuesta es `400 Bad Request` indicando los roles válidos

### Requirement: Roles reconocidos

El sistema SHALL reconocer exactamente tres roles: `Participant`, que accede solo
a sus propias sesiones; `Researcher`, que accede a sesiones ajenas para análisis;
y `Administrator`, que además puede cancelar sesiones de cualquier participante.

Un rol no reconocido SHALL rechazarse.

#### Scenario: Rol desconocido

- **WHEN** el registro indica un rol que no es `Participant`, `Researcher` ni `Administrator`
- **THEN** la respuesta es `400 Bad Request`
- **AND** el mensaje enumera los roles válidos

### Requirement: El registro público concede el rol solicitado

El sistema SHALL asignar al usuario el rol que pide en el registro, incluidos
`Researcher` y `Administrator`.

Esta permisividad es una decisión temporal y deliberada, documentada en
`AuthService`: hoy no existe otra vía de crear usuarios privilegiados para
pruebas. Anula cualquier control por rol mientras siga vigente y está pendiente
de acordar con el equipo antes de restringirse.

#### Scenario: Registro con rol privilegiado

- **WHEN** un usuario anónimo se registra solicitando el rol `Administrator`
- **THEN** el registro se completa y el usuario obtiene ese rol

### Requirement: Inicio de sesión

El sistema SHALL exponer `POST /api/auth/login` de acceso anónimo, devolviendo un
token JWT cuando las credenciales son correctas.

#### Scenario: Credenciales válidas

- **WHEN** se envían correo y contraseña correctos
- **THEN** la respuesta es `200 OK` con un token JWT

#### Scenario: Credenciales inválidas

- **WHEN** el correo no existe o la contraseña no coincide
- **THEN** la respuesta es `401 Unauthorized`
- **AND** el mensaje no revela cuál de los dos datos falló

### Requirement: Identidad del usuario en curso

El sistema SHALL exponer `GET /api/auth/me` para un usuario autenticado,
resolviendo su identificador desde la claim `NameIdentifier` o `sub` del token.

#### Scenario: Token válido

- **WHEN** un usuario autenticado consulta su identidad
- **THEN** la respuesta es `200 OK` con sus datos y su rol

#### Scenario: Token sin identificador utilizable

- **WHEN** el token no contiene un identificador de usuario interpretable como `Guid`
- **THEN** la respuesta es `401 Unauthorized`

### Requirement: Clave de firma fuera del código

El sistema SHALL tomar `Jwt:SigningKey` de la configuración por entorno —
`dotnet user-secrets` o la variable `Jwt__SigningKey` — y SHALL NOT incluirla en
`appsettings*.json` ni en el código.

Fuera de `Development` la API SHALL NOT arrancar sin esa clave. En `Development`
SHALL usar una clave efímera y advertirlo.

El sistema SHALL rechazar las claves revocadas listadas en
`JwtOptions.RevokedSigningKeys`.

#### Scenario: Arranque sin clave en producción

- **WHEN** la aplicación arranca fuera de `Development` sin `Jwt:SigningKey` configurada
- **THEN** la aplicación no arranca

#### Scenario: Arranque sin clave en desarrollo

- **WHEN** la aplicación arranca en `Development` sin la clave configurada
- **THEN** usa una clave efímera
- **AND** registra una advertencia

#### Scenario: Clave revocada

- **WHEN** se configura una clave incluida en `RevokedSigningKeys`
- **THEN** la configuración se rechaza

### Requirement: Contraseñas no recuperables

El sistema SHALL persistir las contraseñas con un algoritmo de hash y SHALL NOT
devolverlas en ninguna respuesta ni escribirlas en logs.

#### Scenario: La contraseña nunca sale

- **WHEN** se consulta cualquier endpoint de autenticación
- **THEN** la respuesta no contiene la contraseña ni su hash
