using Turning.Domain.Entities;

namespace Turning.Application.Interfaces;

/// <summary>
/// Resultado de una generación de texto mediada por IA (o su baseline mock).
/// Cumple el contrato definido para el pipeline de datos/AI:
/// texto generado, proveedor usado, latencia medida y si operó en modo degradado.
/// </summary>
public sealed record TextGenerationResult(
    string Text,
    string Provider,
    long LatencyMs,
    bool Degraded,
    Guid? EventId = null);

/// <summary>
/// Puerto para generar texto del interlocutor mediado por IA.
/// </summary>
public interface ITextGenerationPort
{
    /// <summary>
    /// Genera una respuesta del interlocutor a partir de la sesión y el historial conversacional actual.
    /// </summary>
    Task<TextGenerationResult> GenerateInterlocutorReplyAsync(ExperimentSession session, IReadOnlyList<ConversationTurn> conversationHistory, CancellationToken cancellationToken = default);
}
