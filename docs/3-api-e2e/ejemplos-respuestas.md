# Ejemplos de respuestas

Respuestas reales de la API en Development, sin claves de proveedor de IA, capturadas el 2026-11-05 contra SQL Server LocalDB. Solo se enmascararon los tokens y los correos. Las reglas de cada endpoint están en [endpoints-principales.md](endpoints-principales.md).

## Registro y acceso

### Registro

`POST /api/auth/register` → **200**

Petición:

```json
{
  "fullName": "Participante Ejemplo",
  "email": "owner@turning.local",
  "password": "TurningDemo2026!"
}
```

```json
{
  "accessToken": "<token>",
  "expiresAtUtc": "2026-11-05T09:53:12.8779346Z",
  "user": {
    "id": "c80f7a6e-70b2-4d5e-bd88-a9814c01c26b",
    "fullName": "Participante Ejemplo",
    "email": "owner@turning.local",
    "role": "Participant"
  }
}
```

### Usuario autenticado

`GET /api/auth/me` → **200**

```json
{
  "id": "c80f7a6e-70b2-4d5e-bd88-a9814c01c26b",
  "fullName": "Participante Ejemplo",
  "email": "owner@turning.local",
  "role": "Participant"
}
```

## Sesión

### Sesión vista por su dueño: la condición queda oculta

`GET /api/sessions/{id}` → **200**

```json
{
  "participationRole": "Owner",
  "id": "7e9bda33-f84d-46dd-ae04-30caf4df0896",
  "sessionCode": "EXP-7E9BDA33",
  "condition": "Hidden",
  "status": "Created",
  "avatarState": "Neutral",
  "interlocutorUserId": null,
  "hasInterlocutor": false,
  "interlocutorJoinedAtUtc": null,
  "isAwaitingInterlocutor": false,
  "conversationTurnCount": 0,
  "emotionSampleCount": 0,
  "lastDetectedEmotion": null,
  "createdAtUtc": "2026-11-05T05:53:13.3346681",
  "activatedAtUtc": null,
  "expiresAtUtc": null,
  "lastActivityAtUtc": null,
  "completedAtUtc": null,
  "cancelledAtUtc": null,
  "cancellationReason": null,
  "conversationStage": "ready-for-first-turn",
  "emotionStage": "ready-for-first-signal",
  "avatarStage": "Neutral"
}
```

### La misma sesión vista por un Researcher

`GET /api/sessions/{id}` → **200**

```json
{
  "participationRole": "Observer",
  "id": "7e9bda33-f84d-46dd-ae04-30caf4df0896",
  "sessionCode": "EXP-7E9BDA33",
  "condition": "Human",
  "status": "Created",
  "avatarState": "Neutral",
  "interlocutorUserId": null,
  "hasInterlocutor": false,
  "interlocutorJoinedAtUtc": null,
  "isAwaitingInterlocutor": true,
  "conversationTurnCount": 0,
  "emotionSampleCount": 0,
  "lastDetectedEmotion": null,
  "createdAtUtc": "2026-11-05T05:53:13.3346681",
  "activatedAtUtc": null,
  "expiresAtUtc": null,
  "lastActivityAtUtc": null,
  "completedAtUtc": null,
  "cancelledAtUtc": null,
  "cancellationReason": null,
  "conversationStage": "ready-for-first-turn",
  "emotionStage": "ready-for-first-signal",
  "avatarStage": "Neutral"
}
```

### Unión como interlocutor

`POST /api/sessions/join` → **200**

Petición:

```json
{
  "sessionCode": "EXP-7E9BDA33"
}
```

```json
{
  "participationRole": "Interlocutor",
  "id": "7e9bda33-f84d-46dd-ae04-30caf4df0896",
  "sessionCode": "EXP-7E9BDA33",
  "condition": "Human",
  "status": "Created",
  "avatarState": "Neutral",
  "interlocutorUserId": "35bf7f11-652c-4b80-b115-b369bae10bec",
  "hasInterlocutor": true,
  "interlocutorJoinedAtUtc": "2026-11-05T05:53:13.5761871Z",
  "isAwaitingInterlocutor": false,
  "conversationTurnCount": 0,
  "emotionSampleCount": 0,
  "lastDetectedEmotion": null,
  "createdAtUtc": "2026-11-05T05:53:13.3346681",
  "activatedAtUtc": null,
  "expiresAtUtc": null,
  "lastActivityAtUtc": null,
  "completedAtUtc": null,
  "cancelledAtUtc": null,
  "cancellationReason": null,
  "conversationStage": "ready-for-first-turn",
  "emotionStage": "ready-for-first-signal",
  "avatarStage": "Neutral"
}
```

## Conversación

### Turno del dueño en condición AI: no revela el origen

`POST /api/experiment-sessions/{id}/conversation-turns` → **200**

Petición:

```json
{
  "message": "Me alegra poder conversar."
}
```

```json
{
  "id": "51be1e1c-66d2-4fc7-bdb0-3a7f2edb474d",
  "sessionId": "2f533a57-4247-4fd4-abaa-afeea388543d",
  "sequenceNumber": 1,
  "sender": "Participant",
  "message": "Me alegra poder conversar.",
  "createdAtUtc": "2026-11-05T05:53:14.3857179Z",
  "originatingTurnId": null,
  "interlocutorTurn": null,
  "source": "pending",
  "ai": null,
  "emotion": {
    "emotionId": "joy",
    "confidence": 1,
    "intensity": 0.8,
    "polarity": 1,
    "source": "text_analysis",
    "fallbackUsed": false,
    "modelVersion": "lexicon-es-v1",
    "latencyMs": 0,
    "scores": {
      "joy": 1
    }
  },
  "degraded": false,
  "degradedReason": null,
  "eventId": null
}
```

### Turno del interlocutor en condición Human

`POST /api/experiment-sessions/{id}/conversation-turns` → **200**

Petición:

```json
{
  "message": "Aquí estoy, te escucho."
}
```

```json
{
  "id": "af691930-5578-4d3e-a057-6dd39b00e6fd",
  "sessionId": "7e9bda33-f84d-46dd-ae04-30caf4df0896",
  "sequenceNumber": 2,
  "sender": "Interlocutor",
  "message": "Aquí estoy, te escucho.",
  "createdAtUtc": "2026-11-05T05:53:13.7741243Z",
  "originatingTurnId": null,
  "interlocutorTurn": null,
  "source": "human",
  "ai": null,
  "emotion": null,
  "degraded": false,
  "degradedReason": null,
  "eventId": null
}
```

### Historial de la condición AI visto por el dueño

`GET /api/experiment-sessions/{id}/conversation-turns` → **200**

```json
[
  {
    "id": "51be1e1c-66d2-4fc7-bdb0-3a7f2edb474d",
    "sessionId": "2f533a57-4247-4fd4-abaa-afeea388543d",
    "sequenceNumber": 1,
    "sender": "Participant",
    "message": "Me alegra poder conversar.",
    "createdAtUtc": "2026-11-05T05:53:14.3857179",
    "originatingTurnId": null
  },
  {
    "id": "ab5f8ef2-7e8c-4546-8cf0-a1b8204f908f",
    "sessionId": "2f533a57-4247-4fd4-abaa-afeea388543d",
    "sequenceNumber": 2,
    "sender": "Interlocutor",
    "message": "Entiendo: \"Me alegra poder conversar.\". Sigamos con la siguiente intervencion del experimento.",
    "createdAtUtc": "2026-11-05T05:53:14.38906",
    "originatingTurnId": null
  }
]
```

## Eventos, emociones y avatar

### Eventos de la sesión

`GET /api/sessions/{id}/events` → **200**

Respuesta, con los primeros 2 de 10 elementos:

```json
[
  {
    "id": "01a1aa44-bbb5-73c3-89f4-903edd4bfd8c",
    "type": "SessionStateChanged",
    "payloadJson": "{\"previousStatus\":\"Created\",\"status\":\"Active\"}",
    "occurredAtUtc": "2026-11-05T05:53:13.6530216",
    "expiresAtUtc": "2026-12-05T05:53:13.6530216",
    "sessionId": "7e9bda33-f84d-46dd-ae04-30caf4df0896"
  },
  {
    "id": "01a1aa44-bc0a-7303-b4d4-051d7c8f2e47",
    "type": "ConversationTurnAdded",
    "payloadJson": "{\"turnId\":\"fecb746a-56f1-40f8-8c96-b9e58d55e9d2\",\"sequenceNumber\":1,\"sender\":\"Participant\"}",
    "occurredAtUtc": "2026-11-05T05:53:13.7389199",
    "expiresAtUtc": "2026-12-05T05:53:13.7389199",
    "sessionId": "7e9bda33-f84d-46dd-ae04-30caf4df0896"
  }
]
```

### Registro de una emoción

`POST /api/sessions/{id}/emotions` → **201**

Petición:

```json
{
  "emotion": "joy",
  "score": 0.7,
  "source": "self-report"
}
```

```json
{
  "emo": {
    "id": "7b91a458-0db8-49bc-b83d-5c0a27aab6be",
    "emotion": "joy",
    "score": 0.7,
    "source": "self-report",
    "provider": "direct",
    "capturedAtUtc": "2026-11-05T05:53:13.9910573Z"
  },
  "ava": {
    "expressionName": "Joy",
    "intensity": 0.7,
    "isFallback": false,
    "parametersJson": "{}"
  }
}
```

### Expresión actual del avatar

`GET /api/sessions/{id}/avatar/current` → **200**

```json
{
  "expressionName": "Joy",
  "intensity": 0.7,
  "isFallback": false,
  "parametersJson": "{}"
}
```

## Encuesta y resultados

### Definición de la encuesta

`GET /api/sessions/{id}/survey` → **200**

```json
{
  "id": "d60ffe70-3575-4ebd-8800-4cd0baf39cc8",
  "code": "default-v1",
  "version": "1.0",
  "questions": [
    {
      "id": "e9274f68-0070-4ff6-8077-c5d9ecb8b3de",
      "code": "perceived_interlocutor",
      "text": "¿Con quién crees que conversaste?",
      "type": "choice",
      "required": true,
      "order": 1
    },
    {
      "id": "0cacd214-3e5e-4173-8850-d21c0b7037d4",
      "code": "empathy",
      "text": "Sentí empatía durante la conversación (1: nada, 5: mucha).",
      "type": "scale",
      "required": true,
      "order": 2
    },
    {
      "id": "fa128b93-5743-4f20-a3e0-a9a6e30aa59c",
      "code": "understanding",
      "text": "Me sentí comprendido/a (1: nada, 5: mucho).",
      "type": "scale",
      "required": true,
      "order": 3
    },
    {
      "id": "6fc7fd65-d8d9-4a9c-b92a-0145d9f4b054",
      "code": "comments",
      "text": "¿Quieres añadir algún comentario?",
      "type": "text",
      "required": false,
      "order": 4
    }
  ]
}
```

### Envío de la encuesta

`POST /api/sessions/{id}/survey/responses` → **201**

Petición:

```json
{
  "surveyDefinitionId": "d60ffe70-3575-4ebd-8800-4cd0baf39cc8",
  "answers": {
    "perceived_interlocutor": "Human",
    "empathy": "4",
    "understanding": "4",
    "comments": "Conversación fluida."
  }
}
```

```json
{
  "responseId": "204fa4ed-6206-4059-aaa3-d332d38f74f5"
}
```

### Resultado de la sesión visto por su dueño

`GET /api/sessions/{id}/results` → **200**

```json
{
  "session": {
    "participationRole": "Owner",
    "id": "7e9bda33-f84d-46dd-ae04-30caf4df0896",
    "sessionCode": "EXP-7E9BDA33",
    "condition": "Hidden",
    "status": "Completed",
    "avatarState": "Joy",
    "interlocutorUserId": null,
    "hasInterlocutor": false,
    "interlocutorJoinedAtUtc": null,
    "isAwaitingInterlocutor": false,
    "conversationTurnCount": 2,
    "emotionSampleCount": 2,
    "lastDetectedEmotion": "joy",
    "createdAtUtc": "2026-11-05T05:53:13.3346681",
    "activatedAtUtc": "2026-11-05T05:53:13.6529242",
    "expiresAtUtc": "2026-11-05T05:58:13.6529242",
    "lastActivityAtUtc": "2026-11-05T05:53:13.9911357",
    "completedAtUtc": "2026-11-05T05:53:14.0721501",
    "cancelledAtUtc": null,
    "cancellationReason": null,
    "conversationStage": "in-progress",
    "emotionStage": "monitoring",
    "avatarStage": "Joy"
  },
  "conversation": [
    {
      "id": "fecb746a-56f1-40f8-8c96-b9e58d55e9d2",
      "sequenceNumber": 1,
      "sender": "Participant",
      "message": "Hola, quisiera conversar.",
      "originatingTurnId": null,
      "createdAtUtc": "2026-11-05T05:53:13.7385986"
    },
    {
      "id": "af691930-5578-4d3e-a057-6dd39b00e6fd",
      "sequenceNumber": 2,
      "sender": "Interlocutor",
      "message": "Aquí estoy, te escucho.",
      "originatingTurnId": null,
      "createdAtUtc": "2026-11-05T05:53:13.7741243"
    }
  ],
  "emotionReadings": [
    {
      "id": "150d38c2-9f84-4813-b9b9-4261ac1f0206",
      "conversationTurnId": "fecb746a-56f1-40f8-8c96-b9e58d55e9d2",
      "source": "fallback",
      "emotion": "neutral",
      "score": 0.3,
      "provider": "analysis",
      "isDegraded": true,
      "capturedAtUtc": "2026-11-05T05:53:13.7392278"
    },
    {
      "id": "7b91a458-0db8-49bc-b83d-5c0a27aab6be",
      "conversationTurnId": null,
      "source": "self-report",
      "emotion": "joy",
      "score": 0.7,
      "provider": "analysis",
      "isDegraded": false,
      "capturedAtUtc": "2026-11-05T05:53:13.9910573"
    }
  ],
  "avatarExpressions": [
    {
      "id": "01905aba-7814-4f64-b82f-418dbfedcb90",
      "emotionReadingId": "150d38c2-9f84-4813-b9b9-4261ac1f0206",
      "expressionName": "Neutral",
      "intensity": 0.3,
      "parametersJson": "{}",
      "isFallback": true,
      "createdAtUtc": "2026-11-05T05:53:13.7392291"
    },
    {
      "id": "0206c1d1-1651-4454-9764-3a7868b8320e",
      "emotionReadingId": "7b91a458-0db8-49bc-b83d-5c0a27aab6be",
      "expressionName": "Joy",
      "intensity": 0.7,
      "parametersJson": "{}",
      "isFallback": false,
      "createdAtUtc": "2026-11-05T05:53:13.9910616"
    }
  ],
  "survey": [
    {
      "id": "204fa4ed-6206-4059-aaa3-d332d38f74f5",
      "surveyDefinitionId": "d60ffe70-3575-4ebd-8800-4cd0baf39cc8",
      "startedAtUtc": "2026-11-05T05:53:14.2982109",
      "submittedAtUtc": "2026-11-05T05:53:14.2984793",
      "answers": [
        {
          "questionId": "0cacd214-3e5e-4173-8850-d21c0b7037d4",
          "value": "4"
        },
        {
          "questionId": "6fc7fd65-d8d9-4a9c-b92a-0145d9f4b054",
          "value": "Conversación fluida."
        },
        {
          "questionId": "e9274f68-0070-4ff6-8077-c5d9ecb8b3de",
          "value": "Human"
        },
        {
          "questionId": "fa128b93-5743-4f20-a3e0-a9a6e30aa59c",
          "value": "4"
        }
      ]
    }
  ],
  "degradedEvents": []
}
```

### Consulta transversal de un Researcher

`GET /api/results?condition=Human&page=1&pageSize=1` → **200**

```json
{
  "items": [
    {
      "participationRole": "Observer",
      "id": "8a2d2094-a3fc-470f-aa2c-31e65803c94a",
      "sessionCode": "EXP-8A2D2094",
      "condition": "Human",
      "status": "Cancelled",
      "avatarState": "Neutral",
      "interlocutorUserId": null,
      "hasInterlocutor": false,
      "interlocutorJoinedAtUtc": null,
      "isAwaitingInterlocutor": false,
      "conversationTurnCount": 0,
      "emotionSampleCount": 0,
      "lastDetectedEmotion": null,
      "createdAtUtc": "2026-11-05T05:53:14.4832067",
      "activatedAtUtc": null,
      "expiresAtUtc": null,
      "lastActivityAtUtc": null,
      "completedAtUtc": null,
      "cancelledAtUtc": "2026-11-05T05:53:14.5461696",
      "cancellationReason": "Sesión de prueba",
      "conversationStage": "ready-for-first-turn",
      "emotionStage": "ready-for-first-signal",
      "avatarStage": "Neutral"
    }
  ],
  "total": 6,
  "page": 1,
  "pageSize": 1
}
```

## Errores

### Sesión ajena o inexistente

`GET /api/sessions/{id}` → **404**

```json
{
  "error": "SESSION_NOT_FOUND"
}
```

### Registro con un rol privilegiado

`POST /api/auth/register` → **403**

Petición:

```json
{
  "fullName": "X",
  "email": "x@turning.local",
  "password": "TurningDemo2026!",
  "role": "Administrator"
}
```

```json
{
  "error": "AUTH_ROLE_FORBIDDEN",
  "code": "AUTH_ROLE_FORBIDDEN",
  "message": "Solo un administrador puede asignar roles privilegiados."
}
```

### Interlocutor que intenta abrir la sesión

`POST /api/experiment-sessions/{id}/conversation-turns` → **409**

Petición:

```json
{
  "message": "Intento abrir"
}
```

```json
{
  "error": "SESSION_NOT_STARTED",
  "code": "SESSION_NOT_STARTED",
  "message": "La sesión aún no ha sido iniciada por el participante."
}
```

### Segundo envío de la encuesta

`POST /api/sessions/{id}/survey/responses` → **409**

```json
{
  "error": "ALREADY_SUBMITTED"
}
```

### Ruta de administrador con una cuenta sin el rol

`GET /api/auth/users` → **403**

Sin cuerpo.
