# 003 — Asignación de condición experimental

## Objetivo

Asignar exactamente una condición (Human o AI) a cada sesión nueva, de forma reproducible, auditable y sin permitir que el cliente fuerce el resultado.

## Dependencias

Depende de 001 y 002. No crea ni activa sesiones; entrega una decisión al caso de uso de sesiones.

## Reglas

- La condición se asigna una sola vez antes de activar la sesión.
- La preferencia enviada por el cliente es solo una señal opcional.
- La estrategia inicial será balanceo por conteo de sesiones completadas por condición; empate resuelto de forma determinista.
- Una sesión existente conserva su condición.
- Si no se puede asignar, no se crea una sesión huérfana y se devuelve error trazable.
- El conteo y la asignación se ejecutan dentro de una transacción; un conflicto de concurrencia se reintenta una vez y luego devuelve 409.
- Para evitar dos decisiones basadas en el mismo conteo, la transacción serializa la lectura y actualización del contador por condición, o usa una fila de configuración con control optimista de concurrencia.

## Contrato

    public interface IAssignmentService
    {
        Task<ConditionAssignment> AssignAsync(
            Guid ownerUserId,
            ExperimentalCondition? preferredCondition,
            CancellationToken cancellationToken = default);
    }

ConditionAssignment contiene Id, SessionId, Condition, Strategy, Reason y CreatedAtUtc.

## Persistencia

Tabla ConditionAssignments: una fila por sesión, índice único por SessionId, condición y estrategia obligatorias. La decisión se guarda en la misma transacción que la creación de ExperimentSession.

## Criterios de aceptación

- CA-ASS-001: Cada sesión creada tiene una sola condición válida.
- CA-ASS-002: El cliente no puede crear una sesión con una condición distinta a la decisión del backend.
- CA-ASS-003: Dos solicitudes concurrentes no generan dos asignaciones para la misma sesión.
- CA-ASS-004: Una falla del asignador evita la creación parcial y devuelve un error trazable.
