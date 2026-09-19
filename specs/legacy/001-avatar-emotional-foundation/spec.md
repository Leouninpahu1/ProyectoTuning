# 001 — Fundación del experimento de avatar emocional

## Propósito

Definir la arquitectura, el vocabulario y el flujo mínimo común para ejecutar un ensayo conversacional con condición Humano o IA, registrar emociones, actualizar un avatar y cerrar con una encuesta.

Esta especificación es normativa para las especificaciones 002–009. No implementa cada módulo; define sus límites y contratos compartidos.

## Decisiones

- Plataforma: .NET 10, ASP.NET Core, Blazor Web App y Clean Architecture.
- Persistencia oficial: SQL Server mediante `TurningDbContext`, proveedor de EF Core para SQL Server y migraciones.
- SQLite es opcional para pruebas, prototipos o buffer local de interacciones; no sustituye SQL Server ni contiene la fuente oficial de sesiones, resultados o auditoría.
- Proveedores externos: OpenAI y Hume AI se representan mediante puertos en Application y adaptadores en Infrastructure. La primera versión puede usar adaptadores simulados.
- Comunicación web: HTTP API. Tiempo real se define en 009 y no permite acceso directo a infraestructura.
- Agregado raíz: `ExperimentSession`.
- La condición `Human` significa que la respuesta del interlocutor proviene de una persona; `AI` significa que la respuesta la genera el adaptador de IA. La condición no describe el avatar ni el proveedor de análisis emocional.

## Actores

- Participante: realiza el ensayo y responde la encuesta.
- Investigador: consulta sesiones y resultados autorizados.
- Administrador: administra cancelaciones y configuración.
- Sistema: ejecuta asignación, análisis, temporizadores y persistencia.

## Flujo principal

1. El participante autenticado solicita una sesión.
2. El backend crea la sesión y asigna `Human` o `AI`.
3. La sesión se activa cuando sus precondiciones están satisfechas.
4. Se registran turnos de conversación bajo `sessionId`.
5. El sistema registra lecturas emocionales y deriva expresiones del avatar.
6. Al completar, expirar o cancelar la sesión se entrega una encuesta.
7. Las respuestas y resultados quedan consultables por sesión.

## Entidades comunes

- `ExperimentSession`: identidad, condición, estado y timestamps.
- `ConditionAssignment`: decisión de condición, estrategia y motivo.
- `ConversationTurn`: mensaje ordenado de participante o interlocutor.
- `EmotionReading`: lectura emocional normalizada asociada a sesión y, opcionalmente, turno.
- `AvatarExpression`: expresión derivada de una emoción y sus parámetros.
- `SurveyDefinition`, `SurveyQuestion`: definición versionada del cuestionario.
- `SurveyResponse`, `SurveyAnswer`: respuestas del participante.
- `ExperimentResult`: vista/materialización consultable de los datos del ensayo.
- `DegradedEvent`: fallo o degradación de una integración, asociado a una sesión y sin datos sensibles.

## Requisitos funcionales

- **FR-001**: El sistema debe crear sesiones identificables y asociarlas a un usuario autenticado.
- **FR-002**: El backend debe asignar exactamente una condición `Human` o `AI` por sesión.
- **FR-003**: El cliente web debe consumir únicamente endpoints de `src/turning.API`.
- **FR-004**: El sistema debe persistir turnos, asignaciones, lecturas emocionales, expresiones, respuestas y resultados con referencia a la sesión.
- **FR-005**: El sistema debe mantener los puertos de IA y emociones en Application y los adaptadores concretos en Infrastructure.
- **FR-006**: Un fallo externo debe conservar la sesión y registrar un resultado degradado o error trazable.
- **FR-007**: Cada módulo debe tener pruebas unitarias y de integración para sus criterios de aceptación.
- **FR-008**: Todo resultado degradado debe persistir `SessionId`, tipo de operación, código, mensaje seguro, timestamp UTC y si permite reintento.
- **FR-009**: Los eventos degradados deben persistirse en `ExperimentEvents` como eventos de tipo `DegradedOperation`, usando los mismos campos y retención que los demás eventos.

## No objetivos de 001

- Definir el algoritmo 50/50 de asignación; corresponde a 003.
- Definir el formato detallado de emociones; corresponde a 004.
- Definir la tabla de expresiones; corresponde a 005.
- Definir preguntas concretas de encuesta; corresponde a 006.
- Definir métricas y exportación; corresponde a 008.
- Definir transporte de eventos en tiempo real; corresponde a 009.

## Criterios de aceptación

- **CA-001**: Todas las especificaciones posteriores usan los nombres `Human`, `AI`, `Created`, `Active`, `Completed`, `TimedOut` y `Cancelled`.
- **CA-002**: Todas las entidades de ejecución requieren un `ExperimentSession` existente; las definiciones reutilizables de encuestas pueden existir sin sesión.
- **CA-003**: SQL Server es la fuente oficial de persistencia; SQLite solo puede utilizarse como soporte local no autoritativo para interacciones.
- **CA-004**: El flujo completo puede rastrearse desde sesión hasta resultado usando `sessionId`.
