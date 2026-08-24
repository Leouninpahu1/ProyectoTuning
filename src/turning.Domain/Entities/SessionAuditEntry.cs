using Turning.Domain.Common;

namespace Turning.Domain.Entities;

public sealed class SessionAuditEntry : BaseEntity
{
    private SessionAuditEntry() { }

    public Guid SessionId { get; private set; }
    public ExperimentSessionStatus PreviousStatus { get; private set; }
    public ExperimentSessionStatus NewStatus { get; private set; }
    public string ActorType { get; private set; } = string.Empty;
    public Guid? ActorId { get; private set; }
    public string? Reason { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string? MetadataJson { get; private set; }

    public static SessionAuditEntry Create(Guid sessionId, ExperimentSessionStatus prev, ExperimentSessionStatus next, string actorType, Guid? actorId = null, string? reason = null, string? metadataJson = null)
        => new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            PreviousStatus = prev,
            NewStatus = next,
            ActorType = actorType,
            ActorId = actorId,
            Reason = reason,
            OccurredAtUtc = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            MetadataJson = metadataJson
        };
}
