## 1. Confirmar el diseño

- [ ] 1.1 Confirmar con el equipo la semántica tolerante frente a la atómica, y registrarlo en `design.md`
- [ ] 1.2 Definir el contrato de respuesta por sesión: cancelada, no encontrada, o rechazada con motivo

## 2. Caso de uso

- [ ] 2.1 Implementar la cancelación en lote en `ExperimentSessionService`, reutilizando la validación de la cancelación individual
- [ ] 2.2 Aplicar el límite de 100 sesiones antes de tocar la base de datos
- [ ] 2.3 Implementar la selección por criterio de estado y rango de fechas
- [ ] 2.4 Emitir una entrada de auditoría por sesión cancelada

## 3. Endpoint

- [ ] 3.1 Añadir el endpoint de lote en `SessionsController` con la política `Administrator`
- [ ] 3.2 Validar el motivo obligatorio antes de procesar

## 4. Pruebas

- [ ] 4.1 Lote válido: todas canceladas y auditadas
- [ ] 4.2 Lote mixto: las terminales se informan sin abortar el resto
- [ ] 4.3 Lote con identificadores inexistentes
- [ ] 4.4 Lote sin motivo y lote excedido rechazados sin efectos
- [ ] 4.5 Acceso sin rol `Administrator` rechazado

## 5. Cierre

- [ ] 5.1 `dotnet build` y `dotnet test` verdes
- [ ] 5.2 Verificar que Swagger refleja el endpoint nuevo
