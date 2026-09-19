# Especificaciones del proyecto Turning

> **Las especificaciones vivas del proyecto están en [`openspec/`](../openspec/), no aquí.**

Desde el 2026-09-12 el proyecto usa [OpenSpec](https://github.com/Fission-AI/OpenSpec)
para la gestión de especificaciones. Las specs numeradas `001`–`009` en formato
spec-kit quedaron deprecadas y se conservan en [`legacy/`](legacy/) únicamente
como referencia histórica.

## Dónde está cada cosa ahora

| Quiero… | Está en |
|---|---|
| Saber qué hace hoy el sistema | `openspec/specs/<capacidad>/spec.md` |
| Ver qué se propone cambiar | `openspec/changes/<cambio>/` |
| Conocer vocabulario y decisiones transversales | `openspec/config.yaml`, bloque `context` |
| Consultar una spec vieja | `specs/legacy/` |

## Flujo de trabajo

```
/opsx:explore    entender el problema y el código
/opsx:propose    redactar proposal.md, specs/, design.md, tasks.md
/opsx:apply      implementar las tareas
/opsx:archive    archivar el cambio y volcar sus deltas a openspec/specs/
```

Validar todo: `openspec validate --all`

## Capacidades

| Capacidad | Reemplaza a |
|---|---|
| `session-lifecycle` | 002 |
| `condition-assignment` | 003 |
| `conversation-turns` | 007 (parte de conversación) |
| `emotion-readings` | 004 |
| `avatar-expression` | 005 |
| `survey` | 006 |
| `experiment-results` | 008 |
| `realtime-events` | 009 |
| `authentication` | — (no tenía spec) |

El contenido normativo de 001 no era una capacidad sino vocabulario y decisiones
transversales: vive ahora en el bloque `context` de `openspec/config.yaml`.

## Cambios en curso

| Cambio | Qué resuelve |
|---|---|
| `harden-session-ownership-filter` | Cubre con pruebas el filtro de aislamiento y le quita la dependencia del nombre de parámetro |
| `restrict-role-assignment` | El registro público deja de conceder `Researcher` y `Administrator` |
| `add-session-metrics` | RF-SES-09: métricas agregadas de sesiones |
| `add-activation-preconditions` | RF-SES-10: validar precondiciones antes de activar |
| `add-batch-cancel` | RF-SES-07: cancelación en lote |
