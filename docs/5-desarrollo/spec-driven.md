# Desarrollo Guiado por Especificaciones e Ingeniería Dirigida con Inteligencia Artificial

## Introducción

El desarrollo de software actual exige procesos que reduzcan la ambigüedad, mejoren la trazabilidad y permitan construir soluciones evolutivas con mayor calidad. En este contexto, el Desarrollo Guiado por Especificaciones propone que la implementación no comience directamente desde el código, sino desde una definición clara de requisitos, restricciones, contratos, criterios de aceptación y límites arquitectónicos. Bajo este enfoque, la especificación deja de ser un artefacto secundario y se convierte en el eje que orienta el diseño, la construcción y la validación del sistema.

De manera complementaria, la ingeniería dirigida con inteligencia artificial incorpora herramientas y capacidades de IA como apoyo al refinamiento de requerimientos, estructuración de módulos, exploración de escenarios, generación asistida de soluciones y validación técnica. La IA no reemplaza el criterio de ingeniería, sino que amplifica la capacidad del equipo para analizar alternativas, detectar inconsistencias y acelerar iteraciones de desarrollo sin perder control sobre la arquitectura ni sobre la intención funcional del producto.

## Desarrollo Guiado por Especificaciones

El Desarrollo Guiado por Especificaciones parte del principio de que cada funcionalidad debe existir primero como una definición verificable antes de su implementación. Esto implica describir qué problema se resuelve, quién interactúa con el sistema, qué entradas y salidas se esperan, cuáles son las reglas de negocio y cómo se comprobará que la solución cumple el objetivo propuesto.

En este enfoque, la especificación incluye como mínimo:

- requerimientos funcionales y no funcionales
- historias de usuario o casos de uso
- criterios de aceptación medibles
- contratos de integración, especialmente APIs
- restricciones de arquitectura y seguridad
- escenarios de error y casos borde

Trabajar de esta forma permite mantener coherencia entre necesidad, diseño e implementación. También facilita que cada módulo pueda evolucionar sin romper el sistema completo, ya que las decisiones técnicas se toman sobre una base explícitamente documentada.

## Ingeniería Dirigida con Inteligencia Artificial

La ingeniería dirigida con inteligencia artificial se entiende como la integración deliberada de capacidades de IA dentro del proceso de desarrollo para apoyar actividades de análisis, refinamiento, prototipado, implementación y verificación. En lugar de usar la IA solo como una herramienta de generación de texto o código, este enfoque la posiciona como un apoyo técnico dentro de un marco de trabajo gobernado por especificaciones, arquitectura y validaciones.

Aplicada correctamente, la inteligencia artificial puede apoyar en:

- transformar requerimientos ambiguos en definiciones más claras
- proponer módulos, capas y responsabilidades iniciales
- sugerir contratos API y estructuras de datos
- identificar inconsistencias entre requerimientos y arquitectura
- acelerar la implementación de funcionalidades repetitivas o de soporte
- apoyar la construcción de pruebas y revisiones técnicas

Sin embargo, su uso debe estar siempre subordinado a decisiones de ingeniería verificables. La IA puede proponer, acelerar y asistir, pero la aceptación final debe depender de criterios técnicos, pruebas y cumplimiento de la especificación.

## Integración de ambos enfoques

La combinación entre Desarrollo Guiado por Especificaciones e ingeniería dirigida con inteligencia artificial permite construir un proceso más riguroso y al mismo tiempo más ágil. La especificación define el marco de verdad del sistema, mientras que la IA acelera la exploración y la implementación dentro de ese marco.

La secuencia de trabajo puede resumirse así:

1. Levantar y organizar requerimientos del problema.
2. Traducir esos requerimientos a especificaciones funcionales y técnicas.
3. Delimitar módulos, contratos, entidades, reglas y criterios de aceptación.
4. Utilizar IA para apoyar el refinamiento, detectar vacíos y proponer estructuras iniciales.
5. Implementar por iteraciones pequeñas y verificables.
6. Validar cada avance con pruebas, compilación, revisiones y cumplimiento de especificaciones.
7. Retroalimentar la especificación con los hallazgos de cada iteración.

Este modelo reduce retrabajo porque evita construir sobre supuestos no resueltos y mejora la trazabilidad porque cada decisión de código puede relacionarse con una necesidad especificada previamente.

## Relación con Scrum

Estos enfoques se integran de manera natural con Scrum. El backlog puede alimentarse con requerimientos refinados y convertidos en slices implementables. Cada sprint puede tomar una parte de la especificación, convertirla en tareas técnicas, implementarla y validarla mediante criterios de aceptación definidos desde el inicio.

Dentro de Scrum, la especificación aporta claridad y la IA aporta aceleración. La primera reduce incertidumbre en la planificación; la segunda ayuda a acelerar análisis, refinamiento e implementación. Como resultado, el equipo puede mantener entregas incrementales con mayor control técnico y menor dispersión entre lo que se pide y lo que realmente se construye.

## Cómo se aplica en este repositorio

Las especificaciones se gestionan con [OpenSpec](https://github.com/Fission-AI/OpenSpec), en español.

| Quiero... | Está en |
|---|---|
| Saber qué hace hoy el sistema | [`openspec/specs/`](../../openspec/specs/), una carpeta por capacidad |
| Ver qué se propone cambiar | [`openspec/changes/`](../../openspec/changes/), con `proposal.md`, `design.md`, `tasks.md` y sus specs |
| Conocer el vocabulario y las decisiones transversales | [`openspec/config.yaml`](../../openspec/config.yaml), bloque `context` |
| Consultar una especificación antigua | [`specs/legacy/`](../../specs/legacy/), solo como referencia histórica |

El flujo de un cambio es:

```text
/opsx:explore    entender el problema y el código
/opsx:propose    redactar proposal.md, specs/, design.md y tasks.md
/opsx:apply      implementar las tareas
/opsx:archive    archivar el cambio y volcar sus deltas a openspec/specs/
```

Para validar todas las especificaciones: `openspec validate --all`.

Las reglas de ramas, commits y validación previa están en [como-contribuir.md](como-contribuir.md).

## Ventajas del enfoque

- mejora la trazabilidad entre requerimiento, diseño, implementación y prueba
- reduce ambigüedades antes de escribir código
- facilita la modularidad y la evolución de la arquitectura
- acelera iteraciones mediante apoyo de inteligencia artificial
- fortalece la calidad técnica al exigir validación contra especificaciones
- mejora la comunicación entre equipo técnico, academia y usuarios interesados

## Conclusiones

El Desarrollo Guiado por Especificaciones e ingeniería dirigida con inteligencia artificial constituyen un enfoque sólido para proyectos de software que requieren claridad, evolución progresiva y control arquitectónico. La especificación actúa como base estructural del proceso, mientras que la inteligencia artificial funciona como un acelerador técnico que incrementa productividad y capacidad de análisis.

Su aplicación conjunta permite construir soluciones más consistentes, auditables y adaptables, especialmente en proyectos donde confluyen requerimientos cambiantes, integraciones técnicas y necesidad de validación continua. En consecuencia, este enfoque no solo mejora la implementación del sistema, sino también la calidad del proceso de ingeniería que lo hace posible.
