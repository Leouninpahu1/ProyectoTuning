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

    /// <summary>
    /// Participante dueno de la sesion (Salon A).
    /// </summary>
    public Guid OwnerUserId { get; private set; }

    /// <summary>
    /// Interlocutor humano asignado a la sesion (Salon B), si lo hay.
    /// </summary>
    /// <remarks>
    /// Solo tiene sentido en condicion Human. Es un escalar y no una tabla de participantes
    /// porque el protocolo es estrictamente diadico, y porque el filtro de autorizacion
    /// consulta esto en cada peticion con {sessionId}: un join en esa ruta se paga en todas.
    /// </remarks>
    public Guid? InterlocutorUserId { get; private set; }

    /// <summary>
    /// Momento en que el interlocutor se incorporo.
    /// </summary>
    public DateTime? InterlocutorJoinedAtUtc { get; private set; }

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

    /// <summary>
    /// Duracion por defecto al auto-activar una sesion con el primer turno.
    /// Coincide con el default de SessionOptions (RF-SES-01).
    /// </summary>
    public static readonly TimeSpan DefaultAutoActivationDuration = TimeSpan.FromSeconds(300);

    /// <summary>
    /// Registra un turno de conversacion. Si la sesion todavia esta en Created, la activa
    /// con la duracion indicada.
    /// </summary>
    /// <remarks>
    /// La duracion se recibe en vez de fijarla aqui: antes estaba escrita a mano como 300s
    /// e ignoraba SessionOptions, asi que activar por endpoint y activar por primer turno
    /// podian dar sesiones con vencimientos distintos.
    /// </remarks>
    public void RegisterConversationTurn(TimeSpan autoActivationDuration, DateTime? nowUtc = null)
    {
        if (Status == ExperimentSessionStatus.Created) Activate(autoActivationDuration, nowUtc);
        else EnsureActive();
        ConversationTurnCount++;
        RecordActivity(nowUtc);
    }

    /// <summary>
    /// Registra un turno usando <see cref="DefaultAutoActivationDuration"/>.
    /// </summary>
    public void RegisterConversationTurn(DateTime? nowUtc = null) =>
        RegisterConversationTurn(DefaultAutoActivationDuration, nowUtc);

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

    /// <summary>
    /// Registra una muestra emocional y deja constancia de la ultima emocion detectada.
    /// </summary>
    /// <remarks>
    /// Antes solo incrementaba el contador, asi que LastDetectedEmotion y AvatarState nunca
    /// cambiaban pese a que el frontend ya los pinta: mostraban "Neutral" y vacio para
    /// siempre.
    /// </remarks>
    public void IncrementEmotionSample(string? detectedEmotion = null, string? avatarState = null)
    {
        EmotionSampleCount++;

        if (!string.IsNullOrWhiteSpace(detectedEmotion))
            LastDetectedEmotion = detectedEmotion;

        if (!string.IsNullOrWhiteSpace(avatarState))
            AvatarState = avatarState;

        UpdatedAt = DateTime.UtcNow;
        LastActivityAtUtc = DateTime.UtcNow;
    }
    /// <summary>
    /// Asigna el interlocutor humano de la sesion.
    /// </summary>
    /// <remarks>
    /// Es idempotente para el mismo usuario: reintentar unirse no es un error.
    /// </remarks>
    public void AssignInterlocutor(Guid interlocutorUserId, DateTime? nowUtc = null)
    {
        if (interlocutorUserId == Guid.Empty)
            throw new ArgumentException("El interlocutor es obligatorio.", nameof(interlocutorUserId));

        if (Condition != ExperimentalCondition.Human)
            throw new DomainException("Solo las sesiones de condicion Human admiten un interlocutor humano.");

        if (IsTerminal)
            throw new DomainException($"Una sesion {Status} no admite interlocutor.");

        if (interlocutorUserId == OwnerUserId)
            throw new DomainException("El dueno de la sesion no puede ser tambien su interlocutor.");

        if (InterlocutorUserId is not null && InterlocutorUserId != interlocutorUserId)
            throw new DomainException("La sesion ya tiene otro interlocutor asignado.");

        if (InterlocutorUserId == interlocutorUserId)
            return;

        var now = nowUtc ?? DateTime.UtcNow;
        InterlocutorUserId = interlocutorUserId;
        InterlocutorJoinedAtUtc = now;
        UpdatedAt = now;
    }

    /// <summary>
    /// Libera al interlocutor, dejando la sesion disponible para otro.
    /// </summary>
    public void ReleaseInterlocutor(DateTime? nowUtc = null)
    {
        if (InterlocutorUserId is null)
            return;

        var now = nowUtc ?? DateTime.UtcNow;
        InterlocutorUserId = null;
        InterlocutorJoinedAtUtc = null;
        UpdatedAt = now;
    }

    /// <summary>
    /// Indica si el usuario es el interlocutor asignado de esta sesion.
    /// </summary>
    public bool IsInterlocutor(Guid userId) => InterlocutorUserId is not null && InterlocutorUserId == userId;

    /// <summary>
    /// Indica si la sesion espera todavia a un interlocutor humano.
    /// </summary>
    public bool IsAwaitingInterlocutor =>
        Condition == ExperimentalCondition.Human && InterlocutorUserId is null && !IsTerminal;

    public bool IsTerminal => Status is ExperimentSessionStatus.Completed or ExperimentSessionStatus.TimedOut or ExperimentSessionStatus.Cancelled;

    private void EnsureActive()
    {
        if (Status != ExperimentSessionStatus.Active)
            throw new DomainException($"Operación solo válida en Active. Estado: {Status}");
    }

    private static string BuildSessionCode(Guid sid) => $"EXP-{sid.ToString("N")[..8].ToUpperInvariant()}";
}
