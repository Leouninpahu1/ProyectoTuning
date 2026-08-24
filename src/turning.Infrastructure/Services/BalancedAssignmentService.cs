using Microsoft.EntityFrameworkCore;
using Turning.Application.Interfaces;
using Turning.Domain.Entities;
using Turning.Infrastructure.Persistence;

namespace Turning.Infrastructure.Services;

public sealed class BalancedAssignmentService : IAssignmentService
{
    private readonly TurningDbContext _db;
    public BalancedAssignmentService(TurningDbContext db) => _db = db;

    public async Task<ConditionAssignment> AssignAsync(Guid sessionId, ExperimentalCondition? preferred, CancellationToken ct = default)
    {
        var existing = await _db.ConditionAssignments.FirstOrDefaultAsync(x => x.SessionId == sessionId, ct);
        if (existing != null) return existing;
        var strategy = "CountBalanced";
        var human = await _db.ExperimentSessions.CountAsync(s => s.Condition == ExperimentalCondition.Human, ct);
        var ai = await _db.ExperimentSessions.CountAsync(s => s.Condition == ExperimentalCondition.AI, ct);
        ExperimentalCondition chosen;
        string reason;
        if (human < ai) { chosen = ExperimentalCondition.Human; reason = $"Human:{human} < AI:{ai}"; }
        else if (ai < human) { chosen = ExperimentalCondition.AI; reason = $"AI:{ai} < Human:{human}"; }
        else { chosen = ExperimentalCondition.Human; reason = $"Tie:{human} deterministic Human"; }
        var a = ConditionAssignment.Create(sessionId, chosen, strategy, reason);
        _db.ConditionAssignments.Add(a);
        return a;
    }
}
