using Turning.Domain.Common;
using Turning.Domain.Exceptions;

namespace Turning.Domain.Entities;

public enum ExperimentalCondition
{
    Human = 1,
    AI = 2
}

public enum ExperimentSessionStatus
{
    Created = 1,
    Bootstrapped = 1,
    Active = 2,
    Completed = 3,
    TimedOut = 4,
    Cancelled = 5
}

public sealed class ExperimentSession : BaseEntity
{
    private ExperimentSession() { }

    public Guid OwnerUserId { get; private set; }
    public string SessionCode { get; private set; } = string.Empty;
    public ExperimentalCondition Condition { get; private set; }
    public ExperimentSessionStatus Status { get; private set; }
    public string AvatarState { get; private set; } = string.Empty;
    public string? LastDetectedEmotion { get; private set; }
    public int ConversationTurnCount { get; private set; }
    public int EmotionSampleCount { get; private set; }
    public DateTime? ActivatedAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public DateTime? LastActivityAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public byte[] RowVersion { get; private set; } = [0];

    public static ExperimentSession Create(Guid ownerUserId, ExperimentalCondition condition, TimeSpan? duration = null, Guid? explicitId = null)
    {
        if (ownerUserId == Guid.Empty)
            throw new ArgumentException("El identificador del usuario es obligatorio.", nameof(ownerUserId));
        var sid = explicitId ?? Guid.NewGuid();
        return new ExperimentSession
        {
            Id = sid,
            OwnerUserId = ownerUserId,
            SessionCode = BuildSessionCode(sid),
            Condition = condition,
            Status = ExperimentSessionStatus.Created,
            AvatarState = "Neutral",
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Activate(TimeSpan duration, DateTime? nowUtc = null)
    {
        if (Status != ExperimentSessionStatus.Created)
            throw new DomainException($"Solo Created puede activarse. Estado actual: {Status}");
        var now = nowUtc ?? DateTime.UtcNow;
        Status = ExperimentSessionStatus.Active;
        ActivatedAtUtc = now;
        LastActivityAtUtc = now;
        ExpiresAtUtc = now.Add(duration);
        UpdatedAt = now;
    }

    public void RecordActivity(DateTime? nowUtc = null)
    {
        if (Status != ExperimentSessionStatus.Active) return;
        var now = nowUtc ?? DateTime.UtcNow;
        LastActivityAtUtc = now;
        UpdatedAt = now;
    }

    public void RegisterConversationTurn(DateTime? nowUtc = null)
    {
        if (Status == ExperimentSessionStatus.Created) Activate(TimeSpan.FromSeconds(300), nowUtc);
        else EnsureActive();
        ConversationTurnCount++;
        RecordActivity(nowUtc);
    }

    public void Complete(DateTime? nowUtc = null)
    {
        EnsureActive();
        var now = nowUtc ?? DateTime.UtcNow;
        Status = ExperimentSessionStatus.Completed;
        CompletedAtUtc = now;
        UpdatedAt = now;
    }

    public void Expire(DateTime? nowUtc = null)
    {
        EnsureActive();
        var now = nowUtc ?? DateTime.UtcNow;
        Status = ExperimentSessionStatus.TimedOut;
        CompletedAtUtc = now;
        UpdatedAt = now;
    }

    public void Cancel(string reason, DateTime? nowUtc = null)
    {
        if (Status != ExperimentSessionStatus.Created && Status != ExperimentSessionStatus.Active)
            throw new DomainException($"Solo Created/Active puede cancelarse. Estado: {Status}");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Motivo obligatorio.", nameof(reason));
        var now = nowUtc ?? DateTime.UtcNow;
        Status = ExperimentSessionStatus.Cancelled;
        CancellationReason = reason.Trim();
        CancelledAtUtc = now;
        UpdatedAt = now;
    }

    public void IncrementEmotionSample(){ EmotionSampleCount++; UpdatedAt=DateTime.UtcNow; LastActivityAtUtc=DateTime.UtcNow; }
    public bool IsTerminal => Status is ExperimentSessionStatus.Completed or ExperimentSessionStatus.TimedOut or ExperimentSessionStatus.Cancelled;

    private void EnsureActive()
    {
        if (Status != ExperimentSessionStatus.Active)
            throw new DomainException($"Operación solo válida en Active. Estado: {Status}");
    }

    private static string BuildSessionCode(Guid sid) => $"EXP-{sid.ToString("N")[..8].ToUpperInvariant()}";
}
