## MODIFIED Requirements

### Requirement: El registro público concede el rol solicitado

El sistema SHALL conceder el rol `Participant` a todo usuario creado por el
registro público, con independencia del rol que solicite.

Los roles `Researcher` y `Administrator` SHALL concederse únicamente por una vía
autorizada, distinta del registro anónimo.

#### Scenario: Registro con rol privilegiado

- **WHEN** un usuario anónimo se registra solicitando el rol `Administrator`
- **THEN** el registro se completa
- **AND** el usuario creado tiene el rol `Participant`

#### Scenario: Registro sin rol

- **WHEN** un usuario anónimo se registra sin indicar rol
- **THEN** el registro se completa con rol `Participant`

#### Scenario: Concesión por vía autorizada

- **WHEN** un actor autorizado concede el rol `Researcher` a un usuario existente
- **THEN** el usuario pasa a tener ese rol
- **AND** la concesión queda registrada con el actor y el instante
