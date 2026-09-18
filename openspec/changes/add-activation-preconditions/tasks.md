## 1. Contrato de la comprobación

- [ ] 1.1 Definir en `turning.Application` la interfaz de comprobación de precondiciones y el resultado tipado que identifica cuál falló
- [ ] 1.2 Definir las opciones de configuración: precondiciones habilitadas, tiempo límite total, política de reintento y límite de concurrencia

## 2. Implementaciones

- [ ] 2.1 Comprobación de base de datos
- [ ] 2.2 Comprobación de proveedores externos, evaluada según la condición de la sesión
- [ ] 2.3 Comprobación del límite de sesiones `Active` concurrentes
- [ ] 2.4 Reintentos con espera creciente, acotados por el tiempo límite total

## 3. Integración con la activación

- [ ] 3.1 Invocar la comprobación en `ExperimentSessionService` antes de transicionar
- [ ] 3.2 Traducir el fallo a `503` en `SessionsController`, sin filtrar detalle interno
- [ ] 3.3 Verificar que una activación rechazada no deja `ActivatedAtUtc` ni `ExpiresAtUtc`

## 4. Pruebas

- [ ] 4.1 Activación con todas las precondiciones satisfechas
- [ ] 4.2 Activación rechazada por proveedor caído: la sesión sigue en `Created`
- [ ] 4.3 Activación rechazada por límite de concurrencia
- [ ] 4.4 Agotamiento del tiempo límite tratado como fallo
- [ ] 4.5 Fallo transitorio recuperado en el reintento
- [ ] 4.6 Comprobación deshabilitada por configuración

## 5. Cierre

- [ ] 5.1 `dotnet build` y `dotnet test` verdes
- [ ] 5.2 Avisar a Sebastian y a Hector del nuevo `503` en `activate`
