using Turning.Domain.Common;

namespace Turning.Domain.Entities;

/// <summary>
/// Actor que emite un turno conversacional.
/// </summary>
public enum ConversationActor
{
    /// <summary>
    /// Mensaje escrito por el participante autenticado.
    /// </summary>
    Participant = 1,

    /// <summary>
    /// Mensaje emitido por el interlocutor humano o mediado por IA.
    /// </summary>
    Interlocutor = 2
}

/// <summary>
/// Representa un mensaje persistido dentro de una sesión experimental.
/// </summary>
public sealed class ConversationTurn : BaseEntity
{
    private ConversationTurn()
    {
    }

    /// <summary>
    /// Identificador de la sesión experimental a la que pertenece el mensaje.
    /// </summary>
    public Guid ExperimentSessionId { get; private set; }

    /// <summary>
    /// Orden secuencial del mensaje dentro de la conversación.
    /// </summary>
    public int SequenceNumber { get; private set; }

    /// <summary>
    /// Actor que emitió el mensaje.
    /// </summary>
    public ConversationActor Sender { get; private set; }

    public string Message { get; private set; } = string.Empty;
    public Guid? OriginatingTurnId { get; private set; }

    /// <summary>
    /// Crea un turno de conversación validado y persistible.
    /// </summary>
    public static ConversationTurn Create(Guid experimentSessionId, int sequenceNumber, ConversationActor sender, string message)
    {
        if (experimentSessionId == Guid.Empty)
            throw new ArgumentException("La sesión experimental es obligatoria.", nameof(experimentSessionId));

        if (sequenceNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(sequenceNumber), "La secuencia debe iniciar en 1.");

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("El mensaje es obligatorio.", nameof(message));

        return new ConversationTurn
        {
            Id = Guid.NewGuid(),
            ExperimentSessionId = experimentSessionId,
            SequenceNumber = sequenceNumber,
            Sender = sender,
            Message = message.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}