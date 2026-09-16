using Turning.Application.Interfaces;
using Turning.Domain.Entities;

namespace Turning.Application.Features.ConversationTurns;

/// <summary>
/// Construye el historial que se envía al proveedor de generación de texto.
/// </summary>
/// <remarks>
/// RF-EXP-02 pide una ventana de los últimos mensajes, no la conversación completa: el
/// contexto tiene que caber en el límite de tokens del proveedor y el coste crece con cada
/// turno. Antes se pasaba el historial entero.
/// </remarks>
public static class ConversationHistoryBuilder
{
    /// <summary>
    /// Número máximo de mensajes históricos que se envían al proveedor (RF-EXP-02).
    /// </summary>
    public const int MaxHistoryMessages = 15;

    /// <summary>
    /// Convierte turnos de dominio en mensajes de chat, conservando solo los más recientes.
    /// </summary>
    /// <param name="turns">Turnos en orden cronológico.</param>
    /// <param name="maxMessages">Tamaño de la ventana. Por defecto <see cref="MaxHistoryMessages"/>.</param>
    public static IReadOnlyList<ChatMessage> Build(IEnumerable<ConversationTurn> turns, int maxMessages = MaxHistoryMessages)
    {
        ArgumentNullException.ThrowIfNull(turns);

        if (maxMessages <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxMessages), "La ventana debe ser mayor que cero.");

        var ordered = turns.OrderBy(turn => turn.SequenceNumber).ToArray();

        // Se recorta por la cola: lo reciente manda el hilo de la conversación.
        var window = ordered.Length > maxMessages
            ? ordered[^maxMessages..]
            : ordered;

        return window
            .Select(turn => new ChatMessage(ToRole(turn.Sender), turn.Message, turn.CreatedAt))
            .ToArray();
    }

    private static string ToRole(ConversationActor sender) => sender switch
    {
        ConversationActor.Participant => ChatMessage.RoleUser,
        ConversationActor.Interlocutor => ChatMessage.RoleAssistant,
        _ => ChatMessage.RoleUser
    };
}
