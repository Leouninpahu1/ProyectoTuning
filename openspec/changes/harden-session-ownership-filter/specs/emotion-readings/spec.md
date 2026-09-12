## MODIFIED Requirements

### Requirement: Aislamiento por propietario

El sistema SHALL responder `404 Not Found` con cuerpo
`{"error":"SESSION_NOT_FOUND"}` cuando la sesión indicada en la ruta no existe o
pertenece a otro usuario, de forma indistinguible entre ambos casos.

La regla se aplica en `SessionOwnershipFilter`, un filtro de autorización
registrado globalmente que intercepta las rutas anidadas bajo una sesión antes de
llegar al controller, y consulta
`IExperimentSessionService.IsSessionAccessibleAsync`.

La protección SHALL NOT depender de que el parámetro de ruta se llame
exactamente `sessionId`: una ruta anidada bajo una sesión que quede fuera del
alcance del filtro SHALL fallar de forma visible en tiempo de arranque o de
prueba, nunca quedar desprotegida en silencio.

Los roles `Researcher` y `Administrator` quedan exentos. Un endpoint puede
excluirse explícitamente con `SkipSessionOwnershipAttribute`.

#### Scenario: Sesión ajena

- **WHEN** un usuario opera sobre una sesión de otro usuario
- **THEN** la respuesta es `404 Not Found` con `SESSION_NOT_FOUND`
- **AND** la respuesta es idéntica a la de una sesión inexistente
- **AND** la acción del controller no llega a ejecutarse

#### Scenario: Rol privilegiado

- **WHEN** un `Researcher` o `Administrator` opera sobre una sesión ajena
- **THEN** el filtro no bloquea la petición

#### Scenario: Token sin identificador utilizable

- **WHEN** el token no permite resolver un `Guid` de usuario
- **THEN** la respuesta es `401 Unauthorized`

#### Scenario: Ruta anidada con otro nombre de parámetro

- **WHEN** una ruta anidada bajo una sesión declara su parámetro con un nombre distinto de `sessionId`
- **THEN** el aislamiento se aplica igualmente, o la discrepancia se manifiesta como fallo visible
- **AND** la ruta no queda accesible sin verificación de propietario
