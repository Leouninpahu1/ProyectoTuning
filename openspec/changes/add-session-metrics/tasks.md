## 1. Consultas agregadas

- [ ] 1.1 Crear `Features/Metrics` en `turning.Application` con las consultas de conteo por estado y por condición
- [ ] 1.2 Definir los DTOs de respuesta, sin exponer identificadores de participantes
- [ ] 1.3 Pruebas de las agregaciones con datos sembrados, incluido el caso sin sesiones

## 2. Endpoints

- [ ] 2.1 Crear `MetricsController` bajo `/api/metrics/sessions/` con la política `Researcher,Administrator`
- [ ] 2.2 Aceptar el rango de fechas como filtro opcional
- [ ] 2.3 Pruebas de contrato: `403` para `Participant`, `401` sin token

## 3. Latencia

- [ ] 3.1 Decidir la fuente de la medición: instrumentación en la API o derivación desde timestamps persistidos
- [ ] 3.2 Exponerla en el endpoint correspondiente

## 4. Cierre

- [ ] 4.1 Medir el coste de las agregaciones contra un volumen realista antes de considerar caché
- [ ] 4.2 `dotnet build` y `dotnet test` verdes
- [ ] 4.3 Verificar que Swagger refleja los endpoints nuevos
