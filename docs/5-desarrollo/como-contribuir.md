# Cómo contribuir

## Flujo de ramas

El proyecto usa GitHub flow con forks. No hay rama `develop` ni ramas de release: `main` es la única rama de larga duración.

1. Actualizar el fork con `main` del repositorio del proyecto, `Leouninpahu1/ProyectoTuning`.
2. Crear una rama por tema, por ejemplo `feat/password-reset-email` o `docs/estructura-arbol`.
3. Hacer commits pequeños y subir la rama al fork.
4. Abrir un PR hacia `main` del proyecto. Cada PR resuelve un solo tema.

## Commits

Formato `tipo(alcance): descripción`, en español. El alcance es opcional.

| Tipo | Se usa para |
|---|---|
| `feat` | Funcionalidad nueva |
| `fix` | Corrección de un error |
| `docs` | Documentación |
| `test` | Pruebas |
| `chore` | Mantenimiento sin cambio funcional |
| `revert` | Deshacer un commit anterior |

Alcances frecuentes: `api`, `auth`, `web`, `ai`, `sessions`, `conversation`, `results`, `specs`.

```text
fix(auth): impedir que el registro publico conceda roles privilegiados
```

Para mover un archivo, usar `git mv` en un commit separado de las ediciones. Así `git log --follow` conserva su historia.

## Antes de escribir código

Un cambio de comportamiento parte de una especificación en `openspec/`. El proceso está en [spec-driven.md](spec-driven.md).

## Antes de abrir el PR

El repositorio no tiene integración continua, así que estas validaciones se ejecutan en local. Las pruebas de persistencia necesitan SQL Server LocalDB.

```powershell
dotnet build turning.sln
dotnet test turning.sln --no-restore
openspec validate --all
# Con la API ya iniciada con ./scripts/dev.ps1:
./scripts/verify-workflow.ps1
```

Si el cambio afecta la API, también:

- Actualizar [endpoints-principales.md](../3-api-e2e/endpoints-principales.md), [ejemplos-respuestas.md](../3-api-e2e/ejemplos-respuestas.md) y la colección de Postman.
- Correr la colección como indica [su guía](../3-api-e2e/README.md#ejecutar-la-colección).
- Avisar a quien consume ese contrato en el frontend.

Antes de una demo o entrega, completar el [checklist de release](checklist-release.md).

## Secretos

Nunca se suben claves al repositorio. Cómo configurarlas está en [configuracion-secretos.md](../configuracion-secretos.md).
