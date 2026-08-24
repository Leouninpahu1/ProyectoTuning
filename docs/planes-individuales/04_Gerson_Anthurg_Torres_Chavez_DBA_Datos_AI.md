# Plan Individual — Gerson Anthurg Torres Chavez

**Rol:** DBA SQL Server, modelo de datos, datasets y pipeline AI
**Ownership primario (R):** Base de datos, dataset, entrenamiento/baseline AI
**Apoyo:** Integración AI (con Juan), infraestructura (con Hector)
**Supervisión:** Líder técnico (A)

## Objetivo del MVP personal
Entregar SQL Server inicializable desde cero, modelo ER consistente y baseline AI reproducible detrás de `ITextGenerationPort`.

## Alcance incluido
- Modelo ER, migraciones SQL Server (`InitialSqlServer`), índices, FK, constraints
- Seed idempotente, script SQL, diccionario de datos, dataset sintético/controlado
- Contrato `ITextGenerationPort` (entrada: sessionId, historial, mensaje; salida: texto, proveedor, latencia, degraded)
- Baseline mock/rule-based medible, evaluación mínima, métricas latencia/error

## Fuera del MVP personal
- Fine-tuning avanzado, análisis estadístico definitivo, modelo productivo entrenado

## Entregables verificables
| # | Entregable | Evidencia |
|---|---|---|
| GE-01 | Migración `InitialSqlServer` aplicada en SQL Server | `dotnet ef database update` log |
| GE-02 | Script idempotente `scripts/db-init.sql` + seed | `src/turning.Infrastructure/Persistence/Seed/` |
| GE-03 | Diagrama ER + diccionario datos (tablas, FK, índices) | `docs/ER.md` |
| GE-04 | Dataset sintético versionado `data/synthetic-v1.csv` | `data/` |
| GE-05 | Baseline AI detrás de `ITextGenerationPort` con métrica latencia/error | `src/turning.Infrastructure/AI/RuleBasedTextGenerationAdapter.cs` |

## Plan 72 horas (24–27 ago)
- Día 1: Validar conexión `Server=(localdb)\MSSQLLocalDB`, ejecutar `dotnet ef database update`, revisar FK/índices en SSMS; documentar error si LocalDB no está
- Día 2: Revisar modelo `ExperimentSessions, ConversationTurns, ConditionAssignments, EmotionReadings, AvatarExpressions, Survey*, ExperimentEvents` — corregir huérfanos
- Día 3: Crear seed mínimo (1 Human + 1 AI con turnos) y dataset sintético 20 conversaciones; validar con `SELECT COUNT(*)`

## Plan semanal
| Semana | Objetivo | Tarea clave | Demo |
|---|---|---|---|
| 1 (24–30 ago) | Cierre técnico | SQL Server disponible, índices revisados | Base creada desde cero |
| 2 (31ago–6sep) | API base | Seed reproducible, health DB | API conectada a SQL Server |
| 3 (7–13 sep) | Dataset | Dataset sintético + casos controlados | Dataset versionado |
| 4 (14–20 sep) | Baseline | Adaptador mock integrado, métrica latencia | Entrada→respuesta reproducible |
| 5 (21–27 sep) | Estabilización | Calidad datos, logging, degradado `mock` | Demo Human/AI comparable |
| Cierre 28–30 sep | Entrega | Script SQL, ER, limitaciones | Go/no-go |

## Dependencias
- **Depende de:** Líder (acceso SQL Server)
- **Bloquea a:** Juan (sesiones necesitan DB), Hector (tests E2E necesitan seed), Sebastian (demo necesita datos)

## Definition of Done personal
- [ ] `Turning` DB se crea desde cero con `dotnet ef database update` sin intervención manual
- [ ] FK evitan huérfanos, índices en `SessionId, CreatedAt, Condition`
- [ ] Dataset reproducible y baseline devuelve `proveedor, latencia, degraded` medible

## Riesgo y mitigación
- **Riesgo:** LocalDB no disponible → contingencia: instancia SQL Server alternativa (documentada), SQLite solo para tests.
- **Riesgo:** Dataset insuficiente → mitigación: sintético mínimo desde semana 1, no esperar modelo real.

## Contrato AI (detalle)
- **Entrada:** `sessionId, condition, history[turns], currentMessage, metadata`
- **Salida:** `{text, provider, latencyMs, degraded: bool, eventId}`
- Si falla: `degraded=true`, `provider=mock`, registra `ExperimentEvents DegradedOperation` sin inventar emoción.
