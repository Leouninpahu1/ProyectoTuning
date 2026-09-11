namespace Turning.Application.Features.Events;
public sealed record EventDto(Guid Id, string Type, string PayloadJson, DateTime OccurredAtUtc, DateTime ExpiresAtUtc);
public interface IEventService { Task<IReadOnlyList<EventDto>> ListAsync(Guid sessionId, Guid? after, CancellationToken ct); }
public interface IResultsService
{
    /// <summary>
    /// Resultado completo de una sesion, en contratos publicos: nunca entidades de
    /// dominio, que expondrian OwnerUserId, RowVersion, IsDeleted y enums numericos.
    /// </summary>
    Task<Turning.Application.Features.Events.SessionResultDto> GetResultAsync(Guid sessionId, CancellationToken ct);

    /// <summary>
    /// Listado paginado de sesiones para analisis, con el mismo contrato de sesion
    /// que usa el resto de la API.
    /// </summary>
    Task<Turning.Application.Features.ExperimentSessions.PagedSessionsResult> ListAsync(DateTime? from, DateTime? to, string? condition, int page, int pageSize, CancellationToken ct);
}
