# C4 Baseline To Clean Architecture Mapping

## Source Summary

Este documento traduce el PDF `DiagramasC4 (1).pdf` a una base operable dentro de este repositorio.

### Page 1 - Context

- **Participante**: usa el sistema para realizar el experimento, evaluar interacciones y completar cuestionarios.
- **Interlocutor Humano**: persona real que interactúa con el participante y cuya identidad puede ser suplantada por IA.
- **Sistema Experimental de Avatar Emocional**: gestiona conversación, análisis emocional, respuestas de IA, animación del avatar y cuestionarios.
- **Hume AI API**: analiza emociones a partir de video o audio.
- **OpenAI API**: genera respuestas de lenguaje natural.

### Page 2 - Containers

- **Client Web App**: cliente web que renderiza avatares, interfaz de chat, captura webcam y muestra cuestionarios.
- **Backend API Server**: servidor central que orquesta mensajes, intervención de IA, sesiones y cuestionarios.
- **Database**: almacena conversaciones, emociones, respuestas generadas y resultados de cuestionarios.

### Page 3 - Frontend Components

- **CameraCaptureService**
- **SurveyComponent**
- **WebSocketClient**
- **ChatInterface**
- **EmotionVisualizer**
- **AvatarRenderer**

Nota de aterrizaje: aunque el C4 original nombra `WebSocketClient`, la decisión actual del repositorio es que `src/turning.Web` consuma el backend a través de APIs expuestas por `src/turning.API`.

### Page 4/5 - Backend Components

- **WebController**
- **ExperimentService**
- **SessionManagerService**
- **SurveyService**
- **AssignmentService**
- **AvatarExpressionService**
- **EmotionAnalysisPort**
- **TextGenerationPort**
- **HumeAIAdapter**
- **OpenAIAdapter**
- **ExpressionDatabaseRepository**
- **SurveyRepository**
- **Database**

## Current Repository Mapping

El C4 no requiere proyectos nuevos todavía. La base propuesta cabe en la solución actual:

### `src/turning.Web`

Equivale al contenedor **Client Web App**.

Responsabilidades base:

- Composición visual del flujo de experimento.
- Interfaz de chat para participante e interlocutor.
- Captura de video/audio mediante JS interop.
- Renderizado del avatar y consumo de datos devueltos por la API.
- Presentación y captura de cuestionarios.
- Consumo exclusivo de endpoints expuestos por `src/turning.API`.

Componentes a aterrizar en Blazor + JS interop:

- `CameraCaptureService` como servicio JS interop para WebRTC.
- `ApiClient` como cliente HTTP del backend.
- `ChatInterface` como componente Razor de conversación.
- `SurveyComponent` como componente Razor de cuestionarios.
- `EmotionVisualizer` como traductor de eventos emocionales a parámetros visuales.
- `AvatarRenderer` como integración WebGL/Three.js/Canvas o puente hacia Unity assets.

### `src/turning.API`

Equivale al punto de entrada del contenedor **Backend API Server**.

Responsabilidades base:

- Controllers y endpoints API HTTP para consumo del frontend.
- Autorización, validación de entrada y mapeo de contratos.
- Delegación hacia Application sin lógica de negocio profunda.

Componentes a aterrizar aquí:

- `WebController` y los futuros endpoints HTTP de integración para `src/turning.Web`.

### `src/turning.Application`

Equivale a la orquestación del experimento y a los puertos del C4.

Responsabilidades base:

- Casos de uso y coordinación entre sesión, conversación, cuestionarios y expresiones.
- Definición de puertos para análisis emocional y generación de texto.
- DTOs, commands, queries y contratos internos.

Servicios del C4 que deben vivir aquí:

- `ExperimentService`
- `SessionManagerService`
- `SurveyService`
- `AssignmentService`
- `AvatarExpressionService`
- `EmotionAnalysisPort`
- `TextGenerationPort`

### `src/turning.Domain`

Equivale al núcleo de reglas y entidades del experimento.

Responsabilidades base:

- Entidades y value objects independientes de frameworks.
- Reglas de negocio sobre sesiones, asignación de condición, turnos, cuestionarios y expresiones.

Entidades sugeridas para empezar:

- `ExperimentSession`
- `ConditionAssignment`
- `ConversationTurn`
- `EmotionReading`
- `AvatarExpressionMapping`
- `SurveyDefinition`
- `SurveyResponse`

### `src/turning.Infrastructure`

Equivale a adaptadores, persistencia y proveedores externos.

Responsabilidades base:

- Implementación de puertos de IA y emociones.
- Repositorios concretos y acceso a datos.
- Configuración de persistencia relacional.

Componentes del C4 que deben vivir aquí:

- `HumeAIAdapter`
- `OpenAIAdapter`
- `ExpressionDatabaseRepository`
- `SurveyRepository`
- `Database` / DbContext / migraciones / modelos persistentes

## Proposed Functional Modules

Estos módulos no implican proyectos nuevos; son agrupaciones lógicas para refinar requisitos y slices.

1. **Session And Assignment Module**
Define inicio/cierre de ensayo, asignación Humano/IA y estado de sesión.

2. **Conversation Orchestration Module**
Gestiona recepción de mensajes, estado del chat, intervención de IA y eventos de salida.

3. **Emotion Analysis Module**
Recibe frames o señales, consulta Hume AI y produce lecturas emocionales normalizadas.

4. **Avatar Expression Module**
Traduce emociones o contexto textual en parámetros de expresión facial o animación del avatar.

5. **Survey Module**
Expone cuestionarios, captura respuestas y las asocia al ensayo.

6. **Experiment Persistence Module**
Consolida escritura y recuperación de sesiones, turnos, emociones y resultados.

7. **Web Client Experience Module**
Implementa chat, captura multimodal, visualización emocional y renderizado del avatar en el frontend consumiendo la API del backend.

## Suggested Initial Vertical Slices

### Slice 1 - Session bootstrap

- Crear sesión experimental.
- Asignar condición Humano/IA.
- Publicar estado inicial al cliente.

### Slice 2 - Basic API-driven conversation

- Conectar cliente web con backend mediante APIs.
- Enviar/recibir mensajes de chat a través de la capa API.
- Persistir turnos de conversación.

### Slice 3 - Survey completion

- Entregar cuestionario al cerrar ensayo.
- Capturar y persistir respuestas.

### Slice 4 - Emotion pipeline

- Capturar frames desde frontend.
- Consultar Hume AI.
- Devolver resultados emocionales al cliente a través de la API.

### Slice 5 - AI intervention

- Definir criterio de intervención.
- Consultar OpenAI.
- Persistir y devolver la respuesta generada.

### Slice 6 - Avatar rendering

- Traducir emoción a expresión.
- Actualizar avatar en el cliente.

## Architecture Notes Derived From The C4

- El C4 dibuja backend como `Java / Python`, pero la base del repo ya es .NET 10. Se mantiene .NET como plataforma principal y se preservan los nombres lógicos del C4 como servicios/casos de uso.
- El frontend del C4 menciona React/Vue y WebAssembly, pero la solución actual ya contiene un Blazor Web App. La base propuesta usa Blazor como shell y JS interop para WebRTC, consumo de APIs HTTP y renderizado visual avanzado.
- Restricción arquitectónica vigente: `src/turning.Web` se conecta únicamente a `src/turning.API`; cualquier necesidad futura de actualización incremental debe seguir entrando por esa misma frontera API.
- La solución actual aún tiene placeholders (`ISampleRepository`, `InMemorySampleRepository`). Esa superficie debe reemplazarse progresivamente por módulos de experimento reales en lugar de convivir con dos modelos conceptuales.

## Refinamientos posteriores no bloqueantes

Decisiones cerradas para esta versión:

- `Human` identifica un interlocutor humano y `AI` una respuesta generada por IA; no son estados del avatar ni del análisis emocional.
- El cliente usa HTTP API en la primera versión; el transporte de eventos de 009 usa polling y queda preparado para SignalR.
- La persistencia oficial de esta fase es SQL Server con EF Core. SQLite puede usarse como buffer local de interacciones o proveedor de pruebas, pero SQL Server conserva la fuente de verdad.

- La primera versión puede usar la misma Web App; una vista separada para el interlocutor queda como evolución.
- Se admiten señales de video, audio o texto según el adaptador disponible; el contrato normalizado es el mismo.
- Las preguntas se versionan en SurveyDefinition; un backoffice para administrarlas queda fuera de esta entrega.
- La condición AI implica respuesta generada por IA en el flujo orquestado; reglas de intervención más finas quedan como evolución.
- La persistencia debe mantener trazabilidad por SessionId y evitar PII en logs; una política de anonimización avanzada queda como evolución.
