# Turning - Solución Clean Architecture con .NET 10

> **Fundación 001 (2026-08-24)**: Persistencia oficial **SQL Server** via `TurningDbContext.UseSqlServer()` (LocalDB `Turning`), **SQLite** solo como buffer local/dev/test no autoritativo (CA-003). Dual-provider en `DependencyInjectionExtensions`.

Solución profesional desarrollada siguiendo **Clean Architecture**, **SOLID**, **DDD (Domain-Driven Design)** y buenas prácticas empresariales.

## 📋 Tabla de Contenidos

- [Estructura del Proyecto](#estructura-del-proyecto)
- [Arquitectura](#arquitectura)
- [Requisitos](#requisitos)
- [Instalación](#instalación)
- [Ejecución](#ejecución)
- [Testing](#testing)
- [Convenciones](#convenciones)
- [Próximos Pasos](#próximos-pasos)

## 🏗️ Estructura del Proyecto

```
turning/
├── src/
│   ├── turning.Domain/                 # Lógica de negocio pura
│   │   ├── Entities/                   # Entidades del dominio
│   │   ├── ValueObjects/               # Value Objects
│   │   ├── Enums/                      # Enumeraciones
│   │   ├── Events/                     # Eventos de dominio
│   │   ├── Exceptions/                 # Excepciones de dominio
│   │   └── Common/                     # Clases base y comunes
│   │
│   ├── turning.Application/            # Lógica de aplicación
│   │   ├── Interfaces/                 # Interfaces/contratos
│   │   ├── DTOs/                       # Data Transfer Objects
│   │   ├── Features/                   # Casos de uso
│   │   │   ├── HealthCheck/Queries/
│   │   │   └── Sample/
│   │   │       ├── Commands/
│   │   │       └── Queries/
│   │   ├── Behaviors/                  # Pipeline behaviors (validación, logging)
│   │   ├── Exceptions/                 # Excepciones de aplicación
│   │   └── DependencyInjection/        # Configuración DI
│   │
│   ├── turning.Infrastructure/         # Implementaciones concretas
│   │   ├── Persistence/                # DbContext, migraciones
│   │   ├── Repositories/               # Implementaciones de repositorios
│   │   ├── Services/                   # Servicios externos
│   │   ├── Configurations/             # Configuraciones
│   │   └── DependencyInjection/        # Registro de servicios
│   │
│   └── turning.API/                    # Punto de entrada
│       ├── Controllers/                # Endpoints REST
│       ├── Endpoints/                  # Minimal APIs (alternativa)
│       ├── Middleware/                 # Middleware personalizado
│       ├── Extensions/                 # Extensiones de servicios
│       ├── Configuration/              # Configuración general
│       └── Program.cs                  # Host configuration
│
├── tests/
│   ├── turning.Domain.Tests/           # Pruebas del dominio
│   ├── turning.Application.Tests/      # Pruebas de aplicación
│   └── turning.Infrastructure.Tests/   # Pruebas de infraestructura
│
├── turning.sln                         # Archivo de solución
├── .gitignore
└── README.md
```

## 🎯 Arquitectura

### Principios de Clean Architecture

La solución implementa los 4 anillos de Clean Architecture:

```
┌─────────────────────────────────────────┐
│          PRESENTACIÓN (API)             │  ← Controllers, Endpoints, DTOs externos
├─────────────────────────────────────────┤
│        LÓGICA DE APLICACIÓN             │  ← Use Cases, DTOs, Handlers
├─────────────────────────────────────────┤
│       LÓGICA DE NEGOCIO (Dominio)       │  ← Entidades, Value Objects, Reglas
├─────────────────────────────────────────┤
│        HERRAMIENTAS & FRAMEWORKS        │  ← BD, APIs externas, Librerías
└─────────────────────────────────────────┘
```

### Dependencias

```
Domain
  ↑
  │  (solo depende de)
  │
Application
  ↑     ↑
  │     └── depende de Domain
  │
Infrastructure ─→ depende de Application y Domain
  ↑
  │
  └── API depende de Infrastructure y Application
```

### Capas y Responsabilidades

#### 🟦 **turning.Domain**
- **Propósito:** Contiene la lógica de negocio pura e independiente
- **NO depende de:** Nada de la solución (aislado)
- **Contiene:**
  - Entidades (`BaseEntity`, `SampleEntity`)
  - Value Objects
  - Eventos de Dominio (`DomainEvent`)
  - Excepciones de Dominio (`DomainException`)
  - Enums y Tipos de Negocio
- **Ejemplo:** `SampleEntity` con métodos como `Create()`, `UpdateName()`, `Activate()`

#### 🟩 **turning.Application**
- **Propósito:** Orquesta la lógica del negocio para implementar casos de uso
- **Depende de:** `Domain`
- **Contiene:**
  - Interfaces/Contratos (`ISampleRepository`)
  - DTOs (`SampleDto`)
  - Handlers de Commands/Queries
  - Validadores (FluentValidation)
  - Behaviors (cross-cutting concerns)
  - Excepciones de Aplicación
- **Patrón:** CQRS y mensajería con Wolverine (pendiente de incorporación)

#### 🟨 **turning.Infrastructure**
- **Propósito:** Implementaciones técnicas y acceso a externos
- **Depende de:** `Application`, `Domain`
- **Contiene:**
  - Repositorios concretos (`InMemorySampleRepository`)
  - DbContext (preparado para EF Core)
  - Servicios externos
  - Configuraciones
  - Unit of Work (si aplica)
- **Nota:** Las implementaciones substituyen interfaces definidas en Application

#### 🔵 **turning.API**
- **Propósito:** Punto de entrada y presentación
- **Depende de:** `Application`, `Infrastructure`
- **Contiene:**
  - Controllers REST
  - Endpoints (Minimal APIs)
  - Middleware personalizado
  - Swagger/OpenAPI
  - Configuración de DI
  - Configuración de CORS, logging, etc.

### Principios SOLID Implementados

1. **S - Single Responsibility:** Cada clase tiene una sola razón para cambiar
2. **O - Open/Closed:** Abierto para extensión, cerrado para modificación
3. **L - Liskov Substitution:** Interfaces bien definidas (`ISampleRepository`)
4. **I - Interface Segregation:** Interfaces específicas y pequeñas
5. **D - Dependency Inversion:** Depender de abstracciones, no de concreciones

## 📦 Requisitos

- **.NET 10 SDK** o superior
- **Visual Studio 2022** / **VS Code** / **Rider** (opcional)
- **Git** (para control de versiones)

### Verificar instalación de .NET

```powershell
dotnet --version
```

## 🚀 Instalación

### 1. Clonar o navegar al proyecto

```powershell
cd c:\Users\leona\source\repos\Epistech\Test de Turing
```

### 2. Restaurar dependencias

```powershell
dotnet restore turning.sln
```

### 3. Compilar la solución

```powershell
dotnet build turning.sln
```

## ▶️ Ejecución

### Ejecutar la API

```powershell
dotnet run --project src\turning.API\turning.API.csproj
```

La API estará disponible en:
- **HTTP:** `https://localhost:7001`
- **Swagger UI:** `https://localhost:7001/swagger`

### Endpoints Disponibles

#### Health Check
```
GET /api/health
GET /api/health/status?message=Mi%20mensaje
```

**Respuesta:**
```json
{
  "isHealthy": true,
  "status": "OK",
  "timestamp": "2026-03-12T10:30:00Z",
  "message": "Todo está funcionando correctamente"
}
```

## 🧪 Testing

### Ejecutar todas las pruebas

```powershell
dotnet test turning.sln
```

### Ejecutar pruebas de un proyecto específico

```powershell
dotnet test tests\turning.Domain.Tests\turning.Domain.Tests.csproj
dotnet test tests\turning.Application.Tests\turning.Application.Tests.csproj
dotnet test tests\turning.Infrastructure.Tests\turning.Infrastructure.Tests.csproj
```

### Ejecutar con cobertura (requiere parámetros adicionales)

```powershell
dotnet test turning.sln /p:CollectCoverageFlag=true
```

## 📝 Convenciones

### Namespaces

```
Turning.Domain.Entities
Turning.Domain.ValueObjects
Turning.Domain.Events
Turning.Domain.Exceptions
Turning.Domain.Common

Turning.Application.Features.HealthCheck.Queries
Turning.Application.Features.Sample.Commands
Turning.Application.Features.Sample.Queries
Turning.Application.Interfaces
Turning.Application.DTOs
Turning.Application.Behaviors
Turning.Application.Exceptions
Turning.Application.DependencyInjection

Turning.Infrastructure.Repositories
Turning.Infrastructure.Persistence
Turning.Infrastructure.Services
Turning.Infrastructure.DependencyInjection

Turning.API.Controllers
Turning.API.Endpoints
Turning.API.Middleware
Turning.API.Extensions
Turning.API.Configuration
```

### Naming Conventions

| Elemento | Convención | Ejemplo |
|----------|-----------|---------|
| Entidades | PascalCase | `SampleEntity` |
| Value Objects | PascalCase | `Money`, `Address` |
| Repositorios (Interface) | `I{Nombre}Repository` | `ISampleRepository` |
| Repositorios (Implementación) | `{Nombre}Repository` | `SampleRepository` |
| DTOs | `{Entidad}Dto` | `SampleDto` |
| Commands | `{Action}{Entidad}Command` | `CreateSampleCommand` |
| Queries | `Get{Entidad}Query` | `GetSampleByIdQuery` |
| Handlers | `{CommandOrQuery}Handler` | `GetSampleByIdQueryHandler` |
| Services | `I{Nombre}Service`, `{Nombre}Service` | `IEmailService`, `EmailService` |

### Documentación de Código

Todo el código incluye comentarios XML para documentación:

```csharp
/// <summary>
/// Descripción breve del elemento.
/// </summary>
/// <param name="paramName">Descripción del parámetro.</param>
/// <returns>Descripción del valor retornado.</returns>
```

## 🔮 Próximos Pasos

### 1. Implementar Entity Framework Core
```powershell
dotnet add src/turning.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add src/turning.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
```

### 2. Configurar Base de Datos
- Crear `TurningDbContext` en `Infrastructure/Persistence`
- Configurar entidades con Fluent API
- Crear migraciones

### 3. Incorporar Wolverine para CQRS y mensajería
- Registrar handlers y mensajería en `turning.Application` y `turning.API`
- Definir el transporte y pipeline una vez llegue la implementación base

### 4. Agregar Validación con FluentValidation
- Crear validadores en `Application/Features/{Feature}/`
- Registrar en DependencyInjection

### 5. Implementar Autenticación/Autorización
- JWT o Identity
- Policies de autorización

### 6. Agregar Logging y Telemetría
- Application Insights
- Structured logging con Serilog

### 7. Containerización
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/turning.API/turning.API.csproj", "src/turning.API/"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "turning.API.dll"]
```

## 📚 Referencias

- [Clean Architecture - Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Domain-Driven Design - Eric Evans](https://www.domainlanguage.com/ddd/)
- [Microsoft - Clean Architecture with ASP.NET Core](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)

## 🎓 Recursos Educativos

### Libros Recomendados
- **Clean Architecture** - Robert C. Martin
- **Implementing Domain-Driven Design** - Vaughn Vernon
- **The C# Player's Guide** - RB Whitaker
- **C# 14 in a Nutshell** - Joseph Albahari

### Cursos y Tutoriales
- Microsoft Learn - ASP.NET Core
- Pluralsight - Clean Architecture Path
- Udemy - Clean Code & Clean Architecture

## ⚖️ Licencia

Proyecto de demostración para Epistech.

## 👥 Contribuidores

- **Epistech** - Desarrollo Inicial

## 📞 Soporte

Para preguntas o issues, contactar al equipo de desarrollo.

---

**Última actualización:** 12 de marzo de 2026
**Versión:** 1.0.0
