namespace Turning.Application.Features.Events;
public sealed record EventDto(Guid Id, string Type, string PayloadJson, DateTime OccurredAtUtc, DateTime ExpiresAtUtc);
public interface IEventService { Task<IReadOnlyList<EventDto>> ListAsync(Guid sessionId, Guid? after, CancellationToken ct); }
public interface IResultsService
{
    Task<object> GetResultAsync(Guid sessionId, CancellationToken ct);
    Task<object> ListAsync(DateTime? from, DateTime? to, string? condition, int page, int pageSize, CancellationToken ct);
}
