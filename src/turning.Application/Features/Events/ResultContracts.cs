namespace Turning.Application.Features.Events;

using Turning.Application.Features.ExperimentSessions;

/// <summary>
/// Turno de conversación tal como se expone en un resultado.
/// </summary>
public sealed record ConversationTurnResultDto(
    Guid Id,
    int SequenceNumber,
    string Sender,
    string Message,
    Guid? OriginatingTurnId,
    DateTime CreatedAtUtc);

/// <summary>
/// Lectura emocional expuesta en un resultado.
/// </summary>
public sealed record EmotionReadingDto(
    Guid Id,
    Guid? ConversationTurnId,
    string Source,
    string Emotion,
    double Score,
    string Provider,
    bool IsDegraded,
    DateTime CapturedAtUtc);

/// <summary>
/// Expresión de avatar expuesta en un resultado.
/// </summary>
public sealed record AvatarExpressionDto(
    Guid Id,
    Guid EmotionReadingId,
    string ExpressionName,
    double Intensity,
    string ParametersJson,
    bool IsFallback,
    DateTime CreatedAtUtc);

/// <summary>
/// Respuesta individual de encuesta.
/// </summary>
public sealed record SurveyAnswerDto(Guid QuestionId, string Value);

/// <summary>
/// Encuesta respondida en una sesión. No expone <c>OwnerUserId</c>: el dueño de
/// la sesión ya está implícito en quién puede leer el recurso.
/// </summary>
public sealed record SurveyResponseDto(
    Guid Id,
    Guid SurveyDefinitionId,
    DateTime StartedAtUtc,
    DateTime? SubmittedAtUtc,
    IReadOnlyList<SurveyAnswerDto> Answers);

/// <summary>
/// Resultado completo de una sesión: el mismo contrato de sesión que devuelve
/// <c>GET /api/sessions/{id}</c> más los datos capturados durante el experimento.
/// </summary>
public sealed class SessionResultDto
{
    /// <summary>Sesión, en el contrato público compartido con el resto de la API.</summary>
    public required ExperimentSessionSnapshot Session { get; init; }

    /// <summary>Turnos de conversación, en orden de secuencia.</summary>
    public required IReadOnlyList<ConversationTurnResultDto> Conversation { get; init; }

    /// <summary>Lecturas emocionales, en orden de captura.</summary>
    public required IReadOnlyList<EmotionReadingDto> EmotionReadings { get; init; }

    /// <summary>Expresiones de avatar, en orden de creación.</summary>
    public required IReadOnlyList<AvatarExpressionDto> AvatarExpressions { get; init; }

    /// <summary>Encuestas respondidas para la sesión.</summary>
    public required IReadOnlyList<SurveyResponseDto> Survey { get; init; }

    /// <summary>Eventos de operación degradada ocurridos durante la sesión.</summary>
    public required IReadOnlyList<EventDto> DegradedEvents { get; init; }
}
