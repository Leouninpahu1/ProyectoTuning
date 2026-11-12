# Endpoints principales

Base local: `http://localhost:5080/api`. El contrato completo está en Swagger (`http://localhost:5080/swagger`) y las respuestas reales en [ejemplos-respuestas.md](ejemplos-respuestas.md).

Salvo `register`, `login`, `forgot-password`, `reset-password` y `health`, todo exige `Authorization: Bearer <accessToken>`.

## Flujo E2E

| Paso | Petición | Quién | Resultado |
|---|---|---|---|
| 1 | `POST /auth/register` | Pública | Cuenta `Participant` y token |
| 2 | `POST /auth/login` | Pública | Token |
| 3 | `POST /sessions` | Participante | Sesión `Created`, condición oculta |
| 4 | `POST /sessions/join` | Segunda cuenta, solo en Human | Queda como interlocutor |
| 5 | `POST /sessions/{id}/activate` | Dueño | `Active`, corre el temporizador de 300 s |
| 6 | `POST /experiment-sessions/{id}/conversation-turns` | Dueño e interlocutor | Turno guardado; en AI se genera la respuesta |
| 7 | `GET /experiment-sessions/{id}/conversation-turns` | Dueño e interlocutor | Conversación, con sondeo por `afterSequence` |
| 8 | `POST /sessions/{id}/complete` | Dueño | `Completed`, encuesta disponible |
| 9 | `GET /sessions/{id}/survey` | Dueño | Preguntas de la encuesta |
| 10 | `POST /sessions/{id}/survey/responses` | Dueño | Respuestas guardadas, envío único |
| 11 | `GET /sessions/{id}/results` | Dueño | Resultado de la sesión |

## Reglas transversales

**Roles:** `Participant`, `Researcher` y `Administrator`. El registro público solo crea `Participant`; los otros roles los concede un administrador.

**Acceso a una sesión:**

| Nivel | Puede |
|---|---|
| Dueño | Todo el ciclo de su sesión, salvo cancelar |
| Interlocutor | Consultar la sesión, conversar y leer eventos |
| Researcher / Administrator | Leer cualquier sesión. No escriben en el experimento: activar, completar, conversar, emociones y encuesta responden `404` |
| Cualquier otra cuenta | `404 SESSION_NOT_FOUND`, igual que si la sesión no existiera |

**Cegamiento:** el backend asigna Human o AI por balance entre las sesiones existentes y no atiende preferencias. El dueño ve `condition: "Hidden"`, sin datos del interlocutor; en sus turnos, `source: "pending"`, y sin proveedor ni referencias internas. Researcher y Administrator ven los datos reales.

**Formato de error:** `{ "error": "CODIGO" }`, a veces con `code` y `message`. Los `401` por token y los `403` por el rol que exige la ruta no llevan cuerpo.

**Fechas:** todas son UTC, aunque algunas lecturas se devuelven sin la `Z` final.

## Autenticación: `/auth`

| Petición | Acceso | Respuestas |
|---|---|---|
| `POST /auth/register` | Pública | `200` · `400 AUTH_INVALID_REQUEST`, contraseña de menos de 8 caracteres · `400 AUTH_ROLE_UNKNOWN` · `403 AUTH_ROLE_FORBIDDEN`, rol privilegiado · `409 AUTH_USER_ALREADY_EXISTS` |
| `POST /auth/login` | Pública | `200` · `401 AUTH_INVALID_CREDENTIALS` |
| `GET /auth/me` | Token | `200` · `401`, también con un token anterior a un cambio de rol |
| `POST /auth/forgot-password` | Pública | `202`, exista o no la cuenta · `400 AUTH_INVALID_REQUEST`, correo mal formado |
| `POST /auth/reset-password` | Token del correo | `204` · `400 AUTH_RESET_TOKEN_INVALID`, incluido un token ya usado · `400 AUTH_INVALID_REQUEST` |
| `GET /auth/users?q=&page=&pageSize=` | Administrator | `200` · `400 AUTH_INVALID_PAGE`, `pageSize` hasta 50 · `403` |
| `PUT /auth/users/{userId}/role` | Administrator | `200`; el usuario debe iniciar sesión de nuevo · `400 AUTH_ROLE_UNKNOWN` · `400 AUTH_ROLE_SELF_CHANGE`, quitarse a sí mismo el rol · `403` |

## Sesiones: `/sessions`

Estados: `Created`, `Active`, `Completed`, `TimedOut` y `Cancelled`.

| Petición | Acceso | Respuestas |
|---|---|---|
| `POST /sessions` | Token | `201` · `400 SESSION_INVALID_CONDITION` |
| `GET /sessions/{id}` | Dueño, interlocutor, privilegiado | `200` · `404` |
| `POST /sessions/{id}/activate` | Dueño | `200` · `404` · `409 SESSION_CONFLICT`, si no está `Created` |
| `POST /sessions/{id}/complete` | Dueño | `200` · `404` · `409 SESSION_CONFLICT`, si no está `Active` |
| `POST /sessions/{id}/cancel` | Administrator | `200` · `400 SESSION_INVALID_REASON`, `reason` es obligatorio · `403` · `404` · `409 SESSION_CONFLICT`, si no está `Created` ni `Active` |
| `GET /sessions/participant/{participantId}?page=&pageSize=` | El propio participante o privilegiado | `200` · `400 SESSION_INVALID_PAGE`, `pageSize` hasta 50 · `403` |

`preferredCondition` en `POST /sessions` se valida, pero no decide la condición. Cancelar una sesión también deja disponible su encuesta.

## Emparejamiento: `/sessions`

| Petición | Acceso | Respuestas |
|---|---|---|
| `POST /sessions/join` | Token, con el código `EXP-XXXXXXXX` | `200` · `400 SESSION_INVALID_CODE` · `400 SESSION_SELF_PAIRING` · `404` · `409 SESSION_CONFLICT`, no es Human, terminó o ya tiene interlocutor |
| `POST /sessions/{sessionId}/interlocutor` | Privilegiado | `200` · `400 SESSION_INVALID_INTERLOCUTOR` · `403` · `404` · `409 SESSION_CONFLICT` |
| `DELETE /sessions/{sessionId}/interlocutor` | Privilegiado | `200` · `403` · `404` |
| `GET /sessions/awaiting-interlocutor?page=&pageSize=` | Privilegiado | `200` · `400 SESSION_INVALID_PAGE` · `403` |

## Arranque rápido: `/experiment-sessions`

| Petición | Acceso | Respuestas |
|---|---|---|
| `POST /experiment-sessions/bootstrap` | Token | `200`; igual que `POST /sessions`, sin `Location` |
| `GET /experiment-sessions/latest` | Token | `200`, última sesión propia · `404` si no tiene ninguna |

## Conversación

| Petición | Acceso | Respuestas |
|---|---|---|
| `GET /experiment-sessions/{sessionId}/conversation-turns?afterSequence=&limit=` | Dueño, interlocutor, privilegiado | `200` · `400 CONVERSATION_INVALID_CURSOR` · `400 CONVERSATION_INVALID_LIMIT` · `404` |
| `POST /experiment-sessions/{sessionId}/conversation-turns` | Dueño e interlocutor | `200` · `400` (abajo) · `404` · `409 SESSION_NOT_STARTED` · `409 SESSION_TERMINAL` |

Errores `400` del `POST`: `CONVERSATION_EMPTY_MESSAGE`, `CONVERSATION_MESSAGE_TOO_LONG` (más de 4000 caracteres), `CONVERSATION_SENDER_NOT_ALLOWED` y `CONVERSATION_INVALID_SENDER`.

- El emisor lo decide el servidor según quien llama. `sender` es opcional y, si no coincide, se rechaza.
- El primer turno del dueño activa la sesión si seguía `Created`. El interlocutor no puede abrirla: recibe `SESSION_NOT_STARTED`.
- `limit` solo aplica junto con `afterSequence`, con un máximo de 200.
- Un Researcher o Administrator que intente escribir recibe `404 RESOURCE_NOT_FOUND`.

## Emociones y avatar

| Petición | Acceso | Respuestas |
|---|---|---|
| `POST /sessions/{sessionId}/emotions` | Dueño | `201` · `400 SCORE_RANGE`, `score` entre 0 y 1 · `400 EMOTION_TOO_LONG` · `400 SOURCE_TOO_LONG` · `404` · `409 SESSION_TERMINAL` |
| `GET /sessions/{sessionId}/emotions` | Dueño, privilegiado | `200` · `404` |
| `GET /sessions/{sessionId}/avatar/current` | Dueño, privilegiado | `200` · `404` |

Cada mensaje del participante también genera una lectura de emoción por análisis de texto.

## Eventos

| Petición | Acceso | Respuestas |
|---|---|---|
| `GET /sessions/{sessionId}/events?after=` | Dueño, interlocutor, privilegiado | `200`, hasta 100 eventos posteriores al cursor · `404` · `410 EVENT_EXPIRED` |

Dueño e interlocutor no reciben los eventos internos de la IA, como `AiReplyGenerated`.

El interlocutor sí recibe aquí las lecturas de emoción del participante (`EmotionReadingAdded`), aunque `GET /emotions` le responde `404`.

## Encuesta

| Petición | Acceso | Respuestas |
|---|---|---|
| `GET /sessions/{sessionId}/survey` | Dueño, privilegiado | `200` · `400 SURVEY_NOT_AVAILABLE`, sesión sin terminar · `404` |
| `POST /sessions/{sessionId}/survey/responses` | Dueño | `201` · `400` (abajo) · `404` · `409 ALREADY_SUBMITTED` |

`answers` usa como clave el `code` de cada pregunta: `perceived_interlocutor`, `empathy` y `understanding` son obligatorias, con escala de 1 a 5 en las dos últimas; `comments` es opcional.

Errores `400` habituales: `SURVEY_NOT_AVAILABLE`, `SURVEY_NOT_FOUND`, `SURVEY_REQUIRED_ANSWER`, `SURVEY_INVALID_SCALE`, `SURVEY_INVALID_CHOICE` y `SURVEY_UNKNOWN_QUESTION`.

## Resultados

| Petición | Acceso | Respuestas |
|---|---|---|
| `GET /sessions/{sessionId}/results` | Dueño, privilegiado | `200` · `404` |
| `GET /results?from=&to=&condition=&page=&pageSize=` | Privilegiado | `200` · `400 INVALID_PAGINATION`, `pageSize` hasta 100 · `400 INVALID_CONDITION` · `403` |
| `GET /surveys/results?page=&pageSize=&participantId=` | Privilegiado | `200`, solo encuestas enviadas · `400 INVALID_SURVEY_RESULTS_QUERY`, `pageSize` hasta 100 · `403` |

## Salud: `/health`

| Petición | Acceso | Respuestas |
|---|---|---|
| `GET /health` | Pública | `200` |
| `GET /health/status?message=` | Pública | `200` |
