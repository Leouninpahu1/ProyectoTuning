## Why

El aislamiento por propietario de las rutas anidadas de sesión existe y funciona:
`SessionOwnershipFilter`, registrado globalmente en `Program.cs`, intercepta toda
ruta con parámetro `sessionId` y responde `404` uniforme antes de llegar al
controller.

El problema es cómo se sostiene esa garantía:

1. **Ninguna prueba la cubre.** Es la única barrera entre un participante y los
   datos de otro, y nada impide que una refactorización la desactive en silencio.
   El filtro se introdujo precisamente para cerrar una fuga real; sin pruebas,
   esa fuga puede reabrirse sin que nadie lo note.
2. **Depende del nombre del parámetro de ruta.** El filtro solo actúa si la ruta
   declara exactamente `sessionId`. Un endpoint nuevo que use `id`,
   `experimentSessionId` o cualquier otra variante queda desprotegido, sin error
   ni advertencia. `SessionsController` ya usa `{id:guid}` y se salva solo porque
   valida en el servicio.
3. **Los servicios confían en el filtro sin saberlo.** `EmotionService`,
   `AvatarService`, `EventService`, `ResultsService` y `SurveyAppService`
   resuelven la sesión por `FindAsync` sin comparar propietario. Leídos en
   aislamiento parecen vulnerables, y nada en ellos indica que su protección vive
   en otra capa.

## What Changes

- Pruebas de aislamiento por endpoint para las cinco rutas anidadas: sesión
  ajena devuelve `404` indistinguible, y el rol privilegiado pasa.
- Prueba que verifica que el filtro sigue registrado globalmente, para que su
  desregistro rompa la suite en vez de pasar inadvertido.
- El aislamiento deja de depender del nombre del parámetro: se detecta cualquier
  ruta anidada bajo una sesión, o bien se falla de forma visible cuando una ruta
  con sesión no queda cubierta.
- Documentar en los cinco servicios que su protección proviene del filtro, para
  que nadie la dé por ausente ni la duplique.

## Capabilities

### New Capabilities

Ninguna.

### Modified Capabilities

- `emotion-readings`: el aislamiento deja de depender del nombre del parámetro de ruta.
- `avatar-expression`: igual.
- `survey`: igual.
- `realtime-events`: igual.
- `experiment-results`: igual.

## Impact

**Código afectado:**

- `src/turning.API/Filters/SessionOwnershipFilter.cs`: detección de la ruta
- `src/turning.API/Program.cs`: registro del filtro
- `tests/`: suite de aislamiento nueva; hoy no existe ninguna

**Sin cambio de contrato.** Las respuestas visibles siguen siendo las mismas: este
cambio protege una garantía existente, no la modifica.

**Dependencia:** mientras el registro público conceda `Researcher` y
`Administrator` (ver `restrict-role-assignment`), cualquiera puede eludir este
filtro registrándose con rol privilegiado. El blindaje es real solo si ambos
cambios se aplican.

**Origen:** el filtro se introdujo en `ed08541`. El levantamiento del 2026-09-12
detectó que no tiene cobertura de pruebas.
