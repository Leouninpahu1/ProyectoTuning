# Plan Individual — Sebastian Mena Loaiza

**Rol:** Frontend / demo y consumo de API
**Ownership primario (R):** Frontend/demo, experiencia del flujo
**Apoyo:** Pruebas manuales, reporte de defectos (con Hector)
**Supervisión:** Líder técnico (A)

## Objetivo del MVP personal
Permitir que una persona sin conocer el código ejecute el flujo completo y que los errores de API se muestren claramente.

## Alcance incluido
- Pantalla/cliente mínimo: login, lista/crear sesión, conversación, encuesta, resultado
- Consumo exclusivo vía `src/turning.API` (nunca DB directa)
- Instrucciones de demo, capturas, manejo de estados `Created/Active/Completed/TimedOut/Cancelled`

## Fuera del MVP personal
- Tiempo real WebSocket completo, diseño avanzado, mobile

## Entregables verificables
| # | Entregable | Evidencia |
|---|---|---|
| SE-01 | Cliente mínimo (Blazor o colección) para login→sesión→conversación | `src/turning.Web` o Postman collection |
| SE-02 | Manejo de errores de API (401/409/404) visible al usuario | Capturas |
| SE-03 | Instrucciones demo para persona externa | `docs/demo.md` |
| SE-04 | Reporte semanal de defectos con pasos reproducibles | Issue list |

## Plan 72 horas (24–27 ago)
- Día 1: Probar manualmente `register/login` y `POST /api/sessions` desde Swagger; documentar payloads
- Día 2: Wireframe mínimo: login, crear sesión, ver estado, lista de turnos
- Día 3: Demo interna `login→sesión` y feedback a Juan sobre contratos

## Plan semanal
| Semana | Objetivo | Tarea clave | Demo |
|---|---|---|---|
| 1 (24–30 ago) | Camino crítico técnico | Probar endpoints base, documentar flujo visual | Login → crear sesión |
| 2 (31ago–6sep) | API base | Validar que dos usuarios ven solo sus datos | Captura aislamiento |
| 3 (7–13 sep) | Vertical slice | Conversación Human funcional en UI | Turnos persistidos en UI |
| 4 (14–20 sep) | Integración | Integrar AI baseline, emoción/avatar mock, encuesta | Human/AI en UI |
| 5 (21–27 sep) | Estabilización | Pulir errores, loading, seed demo | Externa ejecuta flujo sin ayuda |
| Cierre 28–30 sep | Release | Video demo grabado | Go/no-go |

## Dependencias
- **Depende de:** Juan (endpoints), Gerson (datos seed), Hector (checklist)
- **Bloquea a:** Hector (validación E2E necesita UI)

## Definition of Done personal
- [ ] Flujo ejecutable sin abrir código
- [ ] Errores de API comprensibles en UI
- [ ] Demo documentada, reproducible por terceros

## Riesgo y mitigación
- **Riesgo:** Contratos API inestables → mitigación: reunión diaria corta con Juan, congelar DTOs en semana 2.
