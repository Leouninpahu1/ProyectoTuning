# Plan de MVP — ProyectoTuning / Turning

## 1. Resumen ejecutivo

Turning es una plataforma experimental para ejecutar sesiones de conversación y comparar una condición **Human** frente a una condición **AI**. El sistema registra sesiones, turnos, asignación experimental, lecturas emocionales, expresiones del avatar, encuestas, eventos y resultados.

El MVP objetivo es un flujo ejecutable y demostrable:

```text
Usuario autenticado → crear sesión → asignar condición → activar sesión
→ conversar → registrar resultado/emoción/evento → completar sesión
→ responder encuesta → consultar resultado
```

La arquitectura actual es una solución .NET 10 con API ASP.NET Core, capas Domain/Application/Infrastructure/API y EF Core. **SQL Server es el proveedor principal**. SQLite se conserva únicamente para pruebas y desarrollo rápido.

Estado de partida conocido:

- La solución compila.
- Existen 15 pruebas automatizadas y deben mantenerse en verde.
- La migración base `InitialSqlServer` ya fue regenerada para SQL Server.
- La instancia SQL Server todavía debe validarse y aplicar físicamente la migración.
- Persisten riesgos de autorización, encuestas, auditoría y configuración de producción que son prioridad MVP.

El criterio de éxito no es tener todas las funciones futuras: es demostrar el flujo experimental completo con datos persistidos y trazabilidad suficiente.

## 2. Alcance propuesto del MVP

### Incluido

1. Registro e inicio de sesión mediante JWT.
2. Creación de una sesión experimental.
3. Asignación persistida de `Human` o `AI`.
4. Activación explícita de la sesión.
5. Registro y consulta de turnos de conversación.
6. Adaptador AI inicial basado en reglas/mock, reemplazable posteriormente.
7. Registro de eventos experimentales.
8. Registro de emoción mediante adaptador mock o entrada controlada.
9. Actualización de la expresión del avatar.
10. Finalización, cancelación y expiración de sesión.
11. Encuesta mínima con respuestas persistidas y validación.
12. Resultado consultable por investigador autorizado y por el propietario permitido.
13. SQL Server inicializable mediante migración idempotente.
14. Demo reproducible con datos de prueba.

### Fuera del MVP

- Modelo de emociones entrenado en producción.
- Balanceo distribuido sofisticado bajo alta concurrencia.
- Tiempo real WebSocket completo.
- Panel analítico avanzado.
- Multi-tenant.
- Aplicación móvil.
- Despliegue cloud altamente disponible.
- Optimización estadística definitiva del experimento.

## 3. Arquitectura mínima necesaria

### Componentes

| Componente | Responsabilidad |
|---|---|
| `turning.API` | Controllers, autenticación, autorización, Swagger, health/readiness |
| `turning.Application` | Casos de uso, DTOs, contratos, validaciones y reglas de aplicación |
| `turning.Domain` | Entidades, estados de sesión y reglas invariantes |
| `turning.Infrastructure` | EF Core, SQL Server, SQLite de prueba, repositorios y adaptadores |
| SQL Server | Persistencia principal del MVP |
| SQLite | Tests y desarrollo opcional, nunca fuente de verdad de producción |
| Adaptador AI | Respuesta AI mínima; debe implementarse detrás de una interfaz |

### Regla arquitectónica

El flujo principal debe atravesar API → Application → Domain → Infrastructure → SQL Server. No se aceptan endpoints que escriban directamente en tablas ni lógica experimental duplicada en controllers.

### Configuración de proveedores

- Producción/staging: `Microsoft.EntityFrameworkCore.SqlServer`.
- Tests: SQLite en memoria o base aislada.
- Migraciones de entrega: generadas y revisadas con proveedor SQL Server.
- No se deben mezclar migraciones generadas desde SQLite con las de SQL Server.

## 4. Roles y ownership

Las competencias individuales todavía no fueron confirmadas. Las asignaciones iniciales son provisionales y deben validarse en la primera reunión.

| Persona | Ownership provisional | Apoyo |
|---|---|---|
| Juan Diego Aguirre Torres | Backend y casos de uso de sesiones/conversación | Integración API |
| Sebastian Mena Loaiza | Frontend/demo y consumo de API | Pruebas de flujo |
| Hector Andres Restrepo Noguera | Integración, calidad, pruebas E2E e infraestructura | Backend |
| Gerson Anthurg Torres Chavez | DBA SQL Server, modelo de datos, datasets y pipeline AI | Integración AI |
| Yeni Alejandra González Sánchez | Coordinación, seguimiento, actas y semillero; máximo 4 h/semana | Todos |
| Líder técnico | Decisiones técnicas, priorización, aceptación y desbloqueos | Todos |

Regla: cada tarea crítica debe tener un único responsable, aunque tenga varios apoyos.

## 5. Plan general por fases

| Fase | Semana | Objetivo | Actividades verificables | Responsable | Apoyo | Entregable | Dependencias | Criterio de aceptación |
|---|---|---|---|---|---|---|---|---|
| 0. Cierre técnico | 24–30 ago | Cerrar alcance y bloqueantes | Confirmar contratos, corregir migraciones, levantar SQL Server y documentar flujo | Líder / Gerson | Juan, Hector | Alcance firmado y backlog priorizado | Acceso al repositorio y SQL Server | Se puede ejecutar el camino feliz en entorno local |
| 1. Base ejecutable | 31 ago–6 sep | Tener API y BD confiables | Aplicar migración, seed, auth, health/readiness y pruebas de aislamiento | Juan / Gerson | Hector | API conectada a SQL Server | Fase 0 | Login, sesión y consulta básica funcionan |
| 2. Vertical slice | 7–13 sep | Ejecutar una sesión completa sin IA avanzada | Crear, activar, conversar, registrar eventos y completar | Juan | Sebastian, Hector | Demo Human completa | API y BD | Una sesión queda persistida de inicio a fin |
| 3. AI baseline y datos | 7–13 sep | Tener AI mínima ejecutándose | Preparar dataset sintético/controlado y encapsular adaptador mock/rule-based | Gerson | Juan | Contrato AI + baseline | Vertical slice | Entrada produce respuesta reproducible |
| 4. Integración experimental | 14–20 sep | Integrar emoción, avatar y encuesta | Persistir lecturas, expresiones, respuestas y resultados; corregir autorización | Juan / Gerson | Hector, Sebastian | Demo Human/AI comparable | Fases 2 y 3 | Dos condiciones recorren el mismo flujo |
| 5. End-to-end MVP | 21–27 sep | Estabilizar y demostrar | Pruebas E2E, datos seed, logging, manejo de errores y despliegue reproducible | Hector | Todos | Release candidate | Todo lo anterior | Demo reproducible sin intervención manual oculta |
| 6. Cierre | 28–30 sep | Entregar MVP | Congelar alcance, ejecutar checklist, documentar limitaciones y grabar demo | Líder / Yeni | Todos | Release MVP | Release candidate | Go/no-go aprobado |

## 6. Plan semanal

### Semana 1 — 24–30 de agosto

**Objetivo:** cerrar el camino crítico técnico.

**Software funcionando:** API compilando, migración SQL Server generada, tests verdes y SQLite disponible para pruebas.

**Trabajo:**

- Juan: confirmar contratos de sesiones, activación, turnos y errores.
- Sebastian: documentar pantallas mínimas y probar manualmente los endpoints desde Swagger.
- Hector: preparar colección de pruebas API y checklist de smoke test.
- Gerson: validar SQL Server, aplicar `InitialSqlServer`, revisar índices/FK y preparar seed/dataset mínimo.
- Yeni: tablero, responsables, acta y seguimiento de bloqueos.

**Demo:** login → crear sesión → mostrar migración aplicada.

**Riesgo:** LocalDB o SQL Server no disponible. Contingencia: usar instancia SQL Server alternativa y mantener SQLite solo para tests.

### Semana 2 — 31 de agosto–6 de septiembre

**Objetivo:** API base con seguridad y persistencia confiables.

**Software funcionando:** registro/login, crear/listar sesión propia, health/readiness y SQL Server.

**Trabajo:** corregir aislamiento por propietario, secretos por variables de entorno, health que compruebe BD y seed reproducible.

**Demo:** dos usuarios autenticados; cada uno solo ve sus sesiones.

### Semana 3 — 7–13 de septiembre

**Objetivo:** vertical slice de conversación.

**Software funcionando:** sesión creada → activada explícitamente → turnos registrados → respuesta AI baseline → sesión completada.

**Demo:** ejecutar una sesión Human y una AI con sus turnos persistidos.

### Semana 4 — 14–20 de septiembre

**Objetivo:** agregar evidencia experimental.

**Software funcionando:** emociones, avatar, eventos, encuesta y resultado.

**Demo:** completar sesión y consultar resultado/encuesta con datos consistentes.

### Semana 5 — 21–27 de septiembre

**Objetivo:** estabilización y release candidate.

**Software funcionando:** flujo E2E reproducible con logs, errores controlados y datos limpios.

**Demo:** una persona externa al desarrollo sigue el README y ejecuta el flujo.

### Cierre MVP — 28–30 de septiembre

**Objetivo:** congelar alcance y demostrar.

**Salida:** release etiquetado, script SQL, README, colección API, diagrama ER, limitaciones conocidas y demo grabada.

## 7. Plan individual de Juan Diego

**Responsabilidad principal:** backend de sesiones y conversación.

**Responsabilidad secundaria:** contratos API y corrección de reglas de ciclo de vida.

**Entregables:** endpoints de sesión y turnos, validaciones, activación explícita, finalización, errores coherentes y pruebas de casos de uso.

**Definition of Done:** casos de uso probados, autorización aplicada, DTOs documentados, sin acceso directo desde controllers a EF Core y flujo validado contra SQL Server.

## 8. Plan individual de Sebastian

**Responsabilidad principal:** frontend/demo y experiencia del flujo.

**Responsabilidad secundaria:** pruebas manuales y reporte de defectos.

**Entregables:** pantalla o cliente mínimo para login, sesión, conversación, encuesta y resultado; instrucciones de demo.

**Definition of Done:** una persona puede ejecutar el flujo sin conocer el código y los errores de API se muestran claramente.

## 9. Plan individual de Hector

**Responsabilidad principal:** integración, pruebas E2E y calidad.

**Responsabilidad secundaria:** infraestructura local y documentación de ejecución.

**Entregables:** colección de requests, pruebas de autorización, smoke test, checklist de release y README reproducible.

**Definition of Done:** build, tests, migración, arranque y camino feliz se validan con comandos documentados.

## 10. Plan individual de Gerson

**Responsabilidad principal:** SQL Server y datos.

**Responsabilidades:** revisar modelo ER, migraciones, índices, constraints, seed, dataset sintético, calidad de datos y contrato de entrada/salida del baseline AI.

**Entregables:** base SQL Server inicializada, script idempotente, datos de prueba, diccionario de datos y evaluación mínima del baseline.

**Definition of Done:** la base se crea desde cero, las FK evitan huérfanos, el dataset es reproducible y el baseline devuelve resultados medibles.

## 11. Plan individual de Yeni

**Dedicación máxima:** aproximadamente 4 horas semanales.

**Actividades:** actualización del tablero, acta breve, seguimiento de compromisos, registro de bloqueos, coordinación con semilleros, consolidación semanal y preparación de demo.

**Definition of Done:** cada semana tiene responsables, fecha, estado, bloqueo y evidencia de demo; Yeni no asume desarrollo principal.

## 12. Hitos y fechas

| Hito | Fecha máxima | Evidencia |
|---|---:|---|
| H0 Requerimientos mínimos cerrados | 28 ago | Alcance MVP y backlog |
| H1 Arquitectura definida | 29 ago | Diagrama y reglas de capas |
| H2 Repositorio/entornos | 30 ago | Build reproducible |
| H3 SQL Server disponible | 2 sep | Migración aplicada |
| H4 API básica | 6 sep | Auth + sesiones |
| H5 Vertical slice sin IA avanzada | 13 sep | Sesión Human completa |
| H6 Dataset mínimo | 13 sep | Dataset versionado |
| H7 Baseline AI | 14 sep | Respuesta reproducible |
| H8 API ↔ AI | 17 sep | Turno AI integrado |
| H9 Flujo E2E | 22 sep | Demo completa |
| H10 MVP demostrable | 30 sep | Release y demo |

## 13. Ruta crítica

### CRÍTICA

SQL Server disponible; migración aplicada; autenticación y aislamiento; ciclo de vida de sesión; conversación; baseline AI; persistencia de resultados; pruebas E2E; README y demo.

### IMPORTANTE

Emociones, avatar, encuesta completa, auditoría detallada, health/readiness, logging estructurado y colección API.

### POST-MVP

Modelo entrenado avanzado, tiempo real, analítica avanzada, optimización de asignación bajo concurrencia, despliegue altamente disponible y aplicación móvil.

## 14. Matriz RACI

| Área | R | A | C | I |
|---|---|---|---|---|
| Arquitectura | Líder técnico | Líder técnico | Juan, Gerson, Hector | Yeni |
| Backend | Juan | Líder técnico | Hector | Sebastian |
| Frontend/demo | Sebastian | Líder técnico | Hector, Juan | Yeni |
| Base de datos | Gerson | Líder técnico | Juan | Todos |
| Dataset | Gerson | Líder técnico | Juan | Yeni |
| Entrenamiento/baseline AI | Gerson | Líder técnico | Juan | Todos |
| Integración AI | Juan | Líder técnico | Gerson, Hector | Sebastian |
| Infraestructura | Hector | Líder técnico | Gerson | Todos |
| Pruebas | Hector | Líder técnico | Juan, Sebastian | Todos |
| Documentación | Yeni | Líder técnico | Hector | Todos |
| Coordinación/semillero | Yeni | Líder técnico | Todos | Todos |
| MVP | Líder técnico | Líder técnico | Todos | Interesados |

R = Responsible, A = Accountable, C = Consulted, I = Informed.

## 15. Estrategia de datos e IA

### Problema inicial

Generar y registrar una respuesta de interlocutor AI en una sesión experimental y conservar la trazabilidad necesaria para comparar Human frente a AI.

### Entrada

Identificador de sesión, condición, historial de turnos, mensaje actual y metadatos mínimos de la sesión.

### Salida

Texto de respuesta, proveedor/modelo, latencia, estado degradado y evento de trazabilidad.

### Estrategia incremental

1. Baseline rule-based/mock reproducible.
2. Dataset sintético y casos de conversación controlados.
3. Métrica inicial: respuesta generada, latencia, tasa de error y cobertura de casos.
4. Adaptador estable detrás de `ITextGenerationPort`.
5. Evaluación manual de un conjunto pequeño de casos.
6. Reemplazo posterior por modelo real sin cambiar el caso de uso.

La prioridad de septiembre es integración funcional, no alcanzar una métrica científica definitiva.

## 16. Definition of Done del MVP

- Código versionado y release etiquetado.
- README permite levantar el proyecto.
- SQL Server se inicializa con migración idempotente.
- SQLite funciona para tests.
- JWT y secretos no están hardcodeados para producción.
- API documentada mediante Swagger/colección de requests.
- Flujo de sesión completo ejecutable.
- Datos Human y AI persistidos.
- Baseline AI ejecutable e integrado.
- Emoción, avatar, evento, encuesta y resultado mínimamente funcionales.
- Autorización evita consultar datos de otro usuario.
- Logs y manejo mínimo de errores.
- Pruebas del camino crítico en verde.
- Diagrama ER y limitaciones documentados.
- Demo reproducible por una persona externa al equipo.

## 17. Riesgos y mitigaciones

| Riesgo | Probabilidad | Impacto | Prevención | Contingencia |
|---|---|---|---|---|
| SQL Server no disponible | Media | Alto | Probar conexión en semana 1 | Instancia SQL Server alternativa; SQLite solo como respaldo de desarrollo |
| Migración incompatible | Media | Alto | Generar con proveedor SQL Server y revisar script | Detener nuevas funciones y reparar esquema |
| Dataset insuficiente | Alta | Medio | Dataset sintético mínimo desde semana 1 | Baseline mock/rule-based |
| Modelo AI retrasado | Alta | Alto | Contrato y adaptador desacoplados | Mantener baseline integrado |
| Fuga de datos entre usuarios | Media | Crítico | Pruebas de autorización por endpoint | Bloquear release hasta corregir |
| Encuesta incompleta | Media | Medio | Validar respuestas y persistir cada answer | Encuesta mínima fija para demo |
| Sobreingeniería | Alta | Alto | Backlog CRÍTICO/POST-MVP | Congelar funcionalidades nuevas |
| Integrantes bloqueados | Media | Alto | Ownership único y tareas paralelas | Reasignación semanal |
| Vulnerabilidades NuGet | Media | Alto | Revisar paquetes antes del release | Actualizar o justificar excepción documentada |
| Fallo de infraestructura | Media | Alto | README y script de seed | Demo local reproducible |

## 18. Plan de contingencia

Si el modelo AI definitivo no está listo el 14 de septiembre, se conserva el adaptador mock/rule-based como baseline oficial del MVP. Debe aceptar el mismo contrato, producir respuesta determinista y registrar proveedor `mock`/`degraded`. El modelo real se incorpora posteriormente sin modificar sesiones, turnos, eventos ni resultados.

Si SQL Server no está disponible para la demo, se puede demostrar el flujo con SQLite únicamente como contingencia técnica, dejando explícito que el criterio de aceptación de producción sigue siendo SQL Server.

## 19. Elementos que deben aplazarse a POST-MVP

- Entrenamiento avanzado y fine-tuning.
- Análisis estadístico definitivo.
- Tiempo real completo.
- Escalamiento horizontal.
- Panel de investigación avanzado.
- Exportaciones complejas.
- Multi-tenant y administración avanzada.
- Optimización de consultas no demostrada como cuello de botella.
- Automatización completa de despliegue cloud.

## 20. Estado esperado el 30 de septiembre

Debe existir una release demostrable en la que un usuario pueda autenticarse, crear una sesión, recibir una condición Human/AI, activarla, conversar, obtener una respuesta AI baseline, registrar emoción/avatar/eventos, completar la sesión, diligenciar la encuesta y consultar un resultado autorizado. La base debe poder crearse en SQL Server desde cero, el flujo debe estar probado y las limitaciones deben estar documentadas.

# PLAN DE ACCIÓN DE LAS PRÓXIMAS 72 HORAS

| Persona | Acción inmediata | Evidencia esperada |
|---|---|---|
| Juan Diego | Revisar endpoints de sesiones/turnos; corregir activación explícita, autorización y errores HTTP | PR o commit con pruebas de propietario |
| Sebastian | Preparar el flujo visual mínimo y probarlo contra Swagger/API | Capturas o demo login → sesión → conversación |
| Hector | Crear smoke test reproducible y colección de requests; documentar arranque | `README` actualizado y checklist ejecutado |
| Gerson | Conectar SQL Server, aplicar `InitialSqlServer`, revisar FK/índices y preparar seed | Base creada desde cero y script validado |
| Yeni | Actualizar tablero, registrar responsables/bloqueos y convocar revisión semanal | Acta, tablero y lista de decisiones |
| Líder técnico | Aprobar alcance, resolver bloqueantes y congelar POST-MVP | Decisión de alcance y criterio Go/No-Go |

### Comando de validación técnico

```powershell
$env:ASPNETCORE_ENVIRONMENT="Production"

dotnet build turning.sln --no-restore
dotnet test turning.sln --no-restore
dotnet ef database update `
  --project src/turning.Infrastructure `
  --startup-project src/turning.API `
  --connection "Server=(localdb)\MSSQLLocalDB;Database=Turning;Trusted_Connection=True;TrustServerCertificate=True"
```

La salida de estos comandos debe quedar adjunta a la evidencia de H3 y H4.
