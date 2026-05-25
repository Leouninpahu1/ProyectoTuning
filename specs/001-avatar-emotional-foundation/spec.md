# Feature Specification — RF-EXP-04  
## Deliver Enriched Response to Frontend

**Feature Branch:** `002-rf-exp-04-enriched-response`  
**Status:** Draft  
**Created:** 2026-05-10

# Scope Boundary (IMPORTANTE)

Esta feature **NO implementa el experimento completo**.

Esta feature SOLO cubre:

→ Construcción del payload enriquecido  
→ Serialización JSON  
→ Entrega en tiempo real vía WebSocket  

No incluye:
- Gestión completa de sesiones
- Persistencia del experimento
- UI completa del cliente web
- Implementación real de IA o análisis emocional
- Base de datos



# Context

El backend debe enviar al frontend mensajes conversacionales enriquecidos con emoción y metadatos.

El frontend necesita este payload para sincronizar:

- Texto del mensaje
- Expresión facial del avatar
- Fuente del mensaje (Humano vs IA)
- Tiempo del evento

Todo debe llegar en **tiempo real mediante WebSocket**.


# Problem Statement

Actualmente:

- No existe un payload estándar unificado.
- No existe sincronización texto + emoción.
- No existe diferenciación Human vs AI.
- No existe contrato de transmisión en tiempo real.

Se necesita un contrato estable para entregar mensajes enriquecidos al frontend.


# Actors

| Actor | Responsabilidad |
| ExperimentService | Construir el payload enriquecido |
| AvatarExpressionService | Proveer emoción resultante |
| EmotionAdapter | Fuente externa de emociones |
| WebSocketClient | Transporte en tiempo real |
| WebController | Consumidor frontend |

# Core Capability

Construir y transmitir un **Enriched Conversation Message**.


# Functional Requirements

## FR-01 — Construcción del payload enriquecido

El sistema MUST construir el siguiente JSON:

```json
{
  "sessionId": "uuid",
  "messageText": "string",
  "emotionId": "joy|sadness|anger|neutral",
  "emotionConfidence": 0.0,
  "timestamp": "ISO-8601",
  "source": "human|ai",
  "messageType": "text_response"
}

