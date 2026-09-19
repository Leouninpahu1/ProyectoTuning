## 1. Acordar la vía de alta privilegiada

- [ ] 1.1 Llevar al equipo las tres opciones: seed inicial de administrador, endpoint restringido a `Administrator`, o comando de configuración fuera de la API
- [ ] 1.2 Registrar la decisión en `design.md` antes de tocar código

## 2. Habilitar la vía nueva

- [ ] 2.1 Implementar el mecanismo acordado
- [ ] 2.2 Dejar registro auditable de cada concesión de rol privilegiado
- [ ] 2.3 Prueba: un actor no autorizado no puede conceder roles

## 3. Cerrar el registro público

- [ ] 3.1 Forzar `Participant` en `AuthService.RegisterAsync`
- [ ] 3.2 Ajustar `RegisterRequest` y el contrato en Swagger
- [ ] 3.3 Reescribir `RegisterAsync_WithPrivilegedRole_ShouldGrantIt_PendienteDeRestriccion` para afirmar que el rol concedido es `Participant`
- [ ] 3.4 Prueba: registrarse pidiendo `Administrator` produce un `Participant`

## 4. Migración

- [ ] 4.1 Migrar scripts, seeds y colecciones de prueba que hoy crean `Researcher` por registro público
- [ ] 4.2 `dotnet test` verde
- [ ] 4.3 Avisar al equipo del cambio de contrato
