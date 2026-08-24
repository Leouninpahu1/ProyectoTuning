using Microsoft.Extensions.Options;
using Turning.Application.Exceptions;
using Turning.Application.Interfaces;
using Turning.Domain.Entities;
using Turning.Domain.Exceptions;
using TurningApplicationException = Turning.Application.Exceptions.ApplicationException;

namespace Turning.Application.Features.ExperimentSessions;

/// <summary>
/// Implementa el bootstrap inicial de sesiones experimentales.
/// </summary>
public sealed class ExperimentSessionService : IExperimentSessionService
{
    private readonly IExperimentSessionRepository _repo;
    private readonly SessionOptions _opts;
    private readonly IAssignmentService _assignment;
    public ExperimentSessionService(IExperimentSessionRepository repo, IOptions<SessionOptions> opts, IAssignmentService? assignment = null) { _repo = repo; _opts = opts.Value; _assignment = assignment!; }

    public async Task<ExperimentSessionSnapshot> CreateBootstrapSessionAsync(Guid ownerUserId, CreateExperimentSessionRequest? request = null, CancellationToken cancellationToken = default)
    {
        if (ownerUserId == Guid.Empty) throw new TurningApplicationException("No fue posible resolver el usuario autenticado para crear la sesion.", "SESSION_INVALID_OWNER");
        var preferred = string.IsNullOrWhiteSpace(request?.PreferredCondition) ? (ExperimentalCondition?)null : ParseCondition(request!.PreferredCondition);
        var sessionId = Guid.NewGuid();
        ExperimentalCondition condition;
        if (_assignment != null) { var a = await _assignment.AssignAsync(sessionId, preferred, cancellationToken); condition = a.Condition; }
        else condition = preferred ?? ExperimentalCondition.AI;
        var session = ExperimentSession.Create(ownerUserId, condition, explicitId: sessionId);
        await _repo.AddAsync(session, cancellationToken);
        await _repo.SaveChangesAsync(cancellationToken);
        return Map(session);
    }
    public async Task<ExperimentSessionSnapshot?> GetLatestSessionAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        if (ownerUserId == Guid.Empty) throw new TurningApplicationException("No fue posible resolver el usuario autenticado.", "SESSION_INVALID_OWNER");
        var s = await _repo.GetLatestByOwnerAsync(ownerUserId, cancellationToken);
        return s is null ? null : Map(s);
    }
    public async Task<ExperimentSessionSnapshot> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _repo.GetByIdAsync(id, ct) ?? throw new TurningApplicationException("Sesion no encontrada.", "SESSION_NOT_FOUND");
        return Map(s);
    }
    public async Task<PagedSessionsResult> ListByParticipantAsync(Guid participantId, int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 50) throw new TurningApplicationException("Paginacion invalida.", "SESSION_INVALID_PAGE");
        var items = await _repo.ListByOwnerAsync(participantId, page, pageSize, ct);
        var total = await _repo.CountByOwnerAsync(participantId, ct);
        return new PagedSessionsResult { Items = items.Select(Map).ToList(), Total = total, Page = page, PageSize = pageSize };
    }
    public async Task<ExperimentSessionSnapshot> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _repo.GetByIdAsync(id, ct) ?? throw new TurningApplicationException("Sesion no encontrada.", "SESSION_NOT_FOUND");
        try { s.Activate(TimeSpan.FromSeconds(_opts.DurationSeconds)); } catch (DomainException ex) { throw new TurningApplicationException(ex.Message, "SESSION_CONFLICT"); }
        try { await _repo.SaveChangesAsync(ct); } catch (Exception ex) when (ex.GetType().Name.Contains("Concurrency")) { throw new TurningApplicationException("Conflicto de concurrencia.", "SESSION_CONFLICT"); }
        return Map(s);
    }
    public async Task<ExperimentSessionSnapshot> CompleteAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _repo.GetByIdAsync(id, ct) ?? throw new TurningApplicationException("Sesion no encontrada.", "SESSION_NOT_FOUND");
        try { s.Complete(); } catch (DomainException ex) { throw new TurningApplicationException(ex.Message, "SESSION_CONFLICT"); }
        try { await _repo.SaveChangesAsync(ct); } catch (Exception ex) when (ex.GetType().Name.Contains("Concurrency")) { throw new TurningApplicationException("Conflicto de concurrencia.", "SESSION_CONFLICT"); }
        return Map(s);
    }
    public async Task<ExperimentSessionSnapshot> CancelAsync(Guid id, string reason, Guid actorId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new TurningApplicationException("Motivo obligatorio.", "SESSION_INVALID_REASON");
        var s = await _repo.GetByIdAsync(id, ct) ?? throw new TurningApplicationException("Sesion no encontrada.", "SESSION_NOT_FOUND");
        try { s.Cancel(reason); } catch (DomainException ex) { throw new TurningApplicationException(ex.Message, "SESSION_CONFLICT"); }
        try { await _repo.SaveChangesAsync(ct); } catch (Exception ex) when (ex.GetType().Name.Contains("Concurrency")) { throw new TurningApplicationException("Conflicto de concurrencia.", "SESSION_CONFLICT"); }
        return Map(s);
    }

    private static ExperimentalCondition ParseCondition(string? preferredCondition)
    {
        if (string.IsNullOrWhiteSpace(preferredCondition))
        {
            return ExperimentalCondition.AI;
        }

        return preferredCondition.Trim().ToUpperInvariant() switch
        {
            "AI" => ExperimentalCondition.AI,
            "HUMAN" => ExperimentalCondition.Human,
            _ => throw new TurningApplicationException("La condicion experimental debe ser AI o Human.", "SESSION_INVALID_CONDITION")
        };
    }

    private static ExperimentSessionSnapshot Map(ExperimentSession s) => new()
    {
        Id = s.Id, SessionCode = s.SessionCode, Condition = s.Condition.ToString(), Status = s.Status.ToString(),
        AvatarState = s.AvatarState, ConversationTurnCount = s.ConversationTurnCount, EmotionSampleCount = s.EmotionSampleCount,
        LastDetectedEmotion = s.LastDetectedEmotion, CreatedAtUtc = s.CreatedAt, ActivatedAtUtc = s.ActivatedAtUtc, ExpiresAtUtc = s.ExpiresAtUtc,
        LastActivityAtUtc = s.LastActivityAtUtc, CompletedAtUtc = s.CompletedAtUtc, CancelledAtUtc = s.CancelledAtUtc, CancellationReason = s.CancellationReason,
        ConversationStage = s.ConversationTurnCount == 0 ? "ready-for-first-turn" : "in-progress",
        EmotionStage = s.EmotionSampleCount == 0 ? "ready-for-first-signal" : "monitoring", AvatarStage = s.AvatarState
    };
}