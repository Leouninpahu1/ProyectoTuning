namespace Turning.Application.Features.ExperimentSessions;

/// <summary>
/// Estado inicial y resumido de una sesión experimental.
/// </summary>
public sealed class ExperimentSessionSnapshot
{
    /// <summary>
    /// Identificador único de la sesión.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Código legible de la sesión.
    /// </summary>
    public required string SessionCode { get; init; }

    /// <summary>
    /// Condición experimental asignada.
    /// </summary>
    public required string Condition { get; init; }

    /// <summary>
    /// Estado actual de la sesión.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Estado actual del avatar asociado.
    /// </summary>
    public required string AvatarState { get; init; }

    /// <summary>
    /// Cantidad de turnos conversacionales registrados.
    /// </summary>
    public int ConversationTurnCount { get; init; }

    /// <summary>
    /// Cantidad de muestras emocionales registradas.
    /// </summary>
    public int EmotionSampleCount { get; init; }

    /// <summary>
    /// Última emoción detectada.
    /// </summary>
    public string? LastDetectedEmotion { get; init; }

    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ActivatedAtUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public DateTime? LastActivityAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public DateTime? CancelledAtUtc { get; init; }
    public string? CancellationReason { get; init; }

    /// <summary>
    /// Indica si la conversación ya puede comenzar.
    /// </summary>
    public required string ConversationStage { get; init; }

    /// <summary>
    /// Indica el estado inicial del pipeline emocional.
    /// </summary>
    public required string EmotionStage { get; init; }

    public required string AvatarStage { get; init; }
}
public sealed class PagedSessionsResult
{
    public required List<ExperimentSessionSnapshot> Items { get; init; }
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}