using Microsoft.EntityFrameworkCore;
using Turning.Application.Features.Events;
using Turning.Infrastructure.Persistence;

namespace Turning.Infrastructure.Services;

public sealed class EventService : IEventService
{
    private readonly TurningDbContext _db;
    public EventService(TurningDbContext db) => _db = db;
    public async Task<IReadOnlyList<EventDto>> ListAsync(Guid sessionId, Guid? after, CancellationToken ct)
    {
        var session = await _db.ExperimentSessions.FindAsync(new object[]{sessionId}, ct) ?? throw new InvalidOperationException("SESSION_NOT_FOUND");
        if (after.HasValue && after != Guid.Empty)
        {
            var chk = await _db.ExperimentEvents.FirstOrDefaultAsync(e=>e.Id==after, ct);
            if (chk is null) { var any = await _db.ExperimentEvents.AnyAsync(e=>e.SessionId==sessionId, ct); if (any) throw new InvalidOperationException("EVENT_EXPIRED"); }
        }
        var afterEntity = after.HasValue ? await _db.ExperimentEvents.FirstOrDefaultAsync(e=>e.Id==after, ct) : null;
        var q = _db.ExperimentEvents.Where(e=>e.SessionId==sessionId).OrderBy(e=>e.OccurredAtUtc).AsQueryable();
        if (afterEntity != null) q = q.Where(e=>e.OccurredAtUtc > afterEntity.OccurredAtUtc || (e.OccurredAtUtc == afterEntity.OccurredAtUtc && e.Id.CompareTo(after.Value) > 0));
        var list = await q.Take(100).ToListAsync(ct);
        return list.Select(e=> new EventDto(e.Id,e.Type,e.PayloadJson,e.OccurredAtUtc,e.ExpiresAtUtc)).ToArray();
    }
}
public sealed class ResultsService : IResultsService
{
    private readonly TurningDbContext _db;
    public ResultsService(TurningDbContext db) => _db = db;
    public async Task<object> GetResultAsync(Guid sessionId, CancellationToken ct)
    {
        var s = await _db.ExperimentSessions.FindAsync(new object[]{sessionId}, ct) ?? throw new InvalidOperationException("SESSION_NOT_FOUND");
        var turns = await _db.ConversationTurns.Where(t=>t.ExperimentSessionId==sessionId).OrderBy(t=>t.SequenceNumber).ToListAsync(ct);
        var emotions = await _db.EmotionReadings.Where(e=>e.SessionId==sessionId).OrderBy(e=>e.CapturedAtUtc).ToListAsync(ct);
        var avatars = await _db.AvatarExpressions.Where(a=>a.SessionId==sessionId).OrderBy(a=>a.CreatedAt).ToListAsync(ct);
        var responses = await _db.SurveyResponses.Where(r=>r.SessionId==sessionId).ToListAsync(ct);
        var events = await _db.ExperimentEvents.Where(e=>e.SessionId==sessionId).OrderBy(e=>e.OccurredAtUtc).ToListAsync(ct);
        return new { session = s, conversation = turns.OrderBy(t=>t.SequenceNumber), emotionReadings = emotions.OrderBy(e=>e.CapturedAtUtc), avatarExpressions = avatars.OrderBy(a=>a.CreatedAt), survey = responses, degradedEvents = events.Where(e=>e.Type=="DegradedOperation").OrderBy(e=>e.OccurredAtUtc) };
    }
    public async Task<object> ListAsync(DateTime? from, DateTime? to, string? condition, int page, int pageSize, CancellationToken ct)
    {
        var q = _db.ExperimentSessions.AsQueryable();
        if (from.HasValue) q = q.Where(s=>s.CreatedAt>=from.Value);
        if (to.HasValue) q = q.Where(s=>s.CreatedAt<=to.Value);
        if (!string.IsNullOrWhiteSpace(condition) && Enum.TryParse<Turning.Domain.Entities.ExperimentalCondition>(condition,true,out var c)) q = q.Where(s=>s.Condition==c);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(s=>s.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);
        return new { total, page, pageSize, items };
    }
}
