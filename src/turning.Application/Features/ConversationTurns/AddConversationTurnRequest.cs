namespace Turning.Application.Features.ConversationTurns;

/// <summary>
/// Solicitud para registrar un nuevo mensaje dentro de una sesión.
/// </summary>
public sealed class AddConversationTurnRequest
{
    /// <summary>
    /// Actor emisor del mensaje. Opcional y, en la practica, innecesario.
    /// </summary>
    /// <remarks>
    /// El emisor lo determina el servidor a partir de quien llama: el dueno de la sesion
    /// escribe como <c>Participant</c> y el interlocutor asignado como <c>Interlocutor</c>.
    /// Si se envia y contradice ese papel, la peticion se rechaza con
    /// <c>CONVERSATION_SENDER_NOT_ALLOWED</c> en vez de etiquetar mal el turno.
    ///
    /// Antes tenia "Participant" como valor por defecto, lo que hacia que cualquier cliente
    /// afirmara ser el participante sin pretenderlo.
    /// </remarks>
    public string? Sender { get; init; }

    /// <summary>
    /// Texto del mensaje.
    /// </summary>
    public string Message { get; init; } = string.Empty;
}