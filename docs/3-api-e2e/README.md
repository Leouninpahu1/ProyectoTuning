# 3. Flujo E2E completo

Endpoints principales: registro, activación, conversación y resultados.

- [endpoints-principales.md](endpoints-principales.md): flujo, reglas de acceso y códigos de respuesta de los 32 endpoints.
- [ejemplos-respuestas.md](ejemplos-respuestas.md): respuestas reales de la API.
- [recorrido-demo.md](recorrido-demo.md): el mismo flujo desde la Web, con dos cuentas.
- [turning-api.postman_collection.json](turning-api.postman_collection.json) y [turning-api-local.postman_environment.json](turning-api-local.postman_environment.json): colección de Postman y su environment.

## Ejecutar la colección

Necesita la API en Development, porque usa las cuentas demo. Desde la raíz del repositorio:

```powershell
./scripts/dev.ps1
npx newman run docs/3-api-e2e/turning-api.postman_collection.json -e docs/3-api-e2e/turning-api-local.postman_environment.json
```

También se puede importar en Postman los dos archivos y usar el Collection Runner. Cada corrida crea sus propias cuentas y sesiones con prefijo `e2e-`, y se puede repetir sin limpiar la base.

| Carpeta | Qué comprueba |
|---|---|
| 1. Preparación | Salud de la API, registro y login |
| 2. Obtener una sesión de cada condición | Crea sesiones hasta tener una Human y una AI |
| 3. Flujo E2E, condición Human | Dos personas, de la unión por código a los resultados |
| 4. Flujo E2E, condición AI | Conversación con respuesta generada, encuesta y resultados |
| 5. Resto de la API | Emociones, avatar, consultas y administración |
| 6. Reglas de acceso | Lo que la API debe rechazar |
| Manual: reset-password | No entra en la corrida automática, porque el token llega por correo |

La corrida termina en la carpeta 6. Para `reset-password`:

1. Enviar el correo con `POST /auth/forgot-password`, que la carpeta 5 ya ejecuta.
2. Abrir el `.eml` de `%LOCALAPPDATA%\Turning\mail` con un cliente de correo, porque el cuerpo va en base64, y copiar el valor de `token` del enlace en la variable `resetToken`.
3. Enviar la petición manualmente desde Postman.

Para más casos de concurrencia y permisos está `scripts/verify-workflow.ps1`.
