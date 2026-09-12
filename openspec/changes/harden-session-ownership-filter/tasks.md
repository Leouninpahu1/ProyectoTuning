## 1. Cobertura de las rutas anidadas

- [ ] 1.1 Montar la suite de aislamiento con `WebApplicationFactory`, dos usuarios y una sesión de cada uno
- [ ] 1.2 `POST` y `GET /api/sessions/{sessionId}/emotions` sobre sesión ajena devuelven `404` con `SESSION_NOT_FOUND`
- [ ] 1.3 `GET /api/sessions/{sessionId}/avatar/current` sobre sesión ajena devuelve `404`
- [ ] 1.4 `GET` y `POST /api/sessions/{sessionId}/survey*` sobre sesión ajena devuelven `404`
- [ ] 1.5 `GET /api/sessions/{sessionId}/events` sobre sesión ajena devuelve `404`
- [ ] 1.6 `GET /api/sessions/{sessionId}/results` sobre sesión ajena devuelve `404` y no filtra la conversación
- [ ] 1.7 En cada caso, comprobar que la respuesta es byte a byte la misma que para una sesión inexistente
- [ ] 1.8 Comprobar que el `POST` de emociones sobre sesión ajena no altera `EmotionSampleCount` ni `AvatarState` de esa sesión

## 2. Rol privilegiado

- [ ] 2.1 Un `Researcher` accede a las cinco rutas sobre una sesión ajena
- [ ] 2.2 Un `Administrator` también
- [ ] 2.3 Decidir y fijar en prueba si un rol privilegiado puede enviar la encuesta de otro participante, o solo consultarla

## 3. El filtro no puede desaparecer en silencio

- [ ] 3.1 Prueba que verifica que `SessionOwnershipFilter` está en los filtros globales de MVC
- [ ] 3.2 Prueba de que un `401` se devuelve cuando el token no resuelve un `Guid` de usuario

## 4. Independencia del nombre de parámetro

- [ ] 4.1 Decidir el mecanismo: convención sobre la plantilla de ruta, marcado explícito por endpoint, o comprobación al arrancar que enumere las rutas con sesión no cubiertas
- [ ] 4.2 Implementarlo en `SessionOwnershipFilter`
- [ ] 4.3 Prueba: una ruta anidada con parámetro de otro nombre queda cubierta o falla de forma visible
- [ ] 4.4 Revisar que `SessionsController`, que usa `{id:guid}` y valida en el servicio, sigue correcto bajo el mecanismo nuevo

## 5. Documentación

- [ ] 5.1 Anotar en `EmotionService`, `AvatarService`, `EventService`, `ResultsService` y `SurveyAppService` que el propietario se verifica en `SessionOwnershipFilter`, para que nadie lo dé por ausente ni lo duplique
- [ ] 5.2 Verificar que el XML-doc de cada controller describe el comportamiento real

## 6. Cierre

- [ ] 6.1 `dotnet build` y `dotnet test` verdes contra SQL Server LocalDB
- [ ] 6.2 Actualizar `diags/diagnostico-aislamiento-propietario.md` con la cobertura conseguida
