# Specs deprecadas (formato spec-kit)

**Deprecadas el 2026-09-12.** Se conservan como referencia histórica. No son
normativas: la fuente de verdad es [`openspec/`](../../openspec/).

## Por qué se deprecaron

Al migrar se contrastaron las nueve specs contra el código y aparecieron tres
problemas que hacían inseguro seguir usándolas:

1. **Contradicen la decisión de SQL Server.** `README-original.md`, `001` y `002`
   siguen presentando SQLite como opción válida para pruebas y como buffer local,
   con reglas de sincronización incluidas. SQLite se eliminó del proyecto el
   2026-08-29. El `plan.md` de 002 llega a declarar SQLite como almacenamiento
   primario.
2. **Dos numeraciones RF incompatibles.** El Project usa `RF-SES-01..10` y la
   spec 002 usa `RF-SES-001..010`, y no son el mismo conjunto.
3. **Contratos que no coinciden con el código.** Ver la tabla de abajo.

Además, los `tasks.md` de 003 a 009 son idénticos entre sí y los artefactos de
Fase 0/1 que los planes declaran (`research.md`, `data-model.md`, `quickstart.md`,
`contracts/`) nunca se crearon.

## Equivalencias

| Spec deprecada | Capacidad OpenSpec |
|---|---|
| 001-avatar-emotional-foundation | bloque `context` de `openspec/config.yaml` |
| 002-session-management | `session-lifecycle` |
| 003-assignment-service | `condition-assignment` |
| 004-emotion-database | `emotion-readings` |
| 005-avatar-expression | `avatar-expression` |
| 006-survey-service | `survey` |
| 007-experiment-orchestration | `conversation-turns` |
| 008-results-repository | `experiment-results` |
| 009-web-realtime | `realtime-events` |

Lo especificado pero no implementado no pasó a `openspec/specs/` — que describe
lo que el sistema hace hoy — sino a `openspec/changes/`: métricas (RF-SES-09),
precondiciones de activación (RF-SES-10) y cancelación en lote (RF-SES-07).

## Contratos que la spec describía mal

| La spec decía | El código hace |
|---|---|
| `POST /api/sessions/{id}/turns` (007) | `POST /api/experiment-sessions/{id}/conversation-turns` |
| `GET /api/sessions/{id}/turns` (007) | `GET /api/experiment-sessions/{id}/conversation-turns` |
| `GET /api/participants/{id}/sessions` (RF-SES-007) | `GET /api/sessions/participant/{participantId}` |
| `POST /api/admin/sessions/{id}/cancel` (Project RF-SES-07) | `POST /api/sessions/{id}/cancel` |

## Requisitos que citan estas specs

`CLAUDE.md`, `diags/bitacora-sesiones.md` y varios nombres de tests citan los
identificadores `RF-SES-00X` y `CA-SES-00X` de la spec 002. Esas referencias
siguen resolviéndose contra `legacy/002-session-management/spec.md`. Las
capacidades de OpenSpec no usan identificadores numerados: sus requisitos se
nombran por su enunciado.
