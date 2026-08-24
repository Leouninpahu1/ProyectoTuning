using Turning.Application.Exceptions;
using Turning.Application.Interfaces;
using Turning.Domain.Entities;
using TurningApplicationException = Turning.Application.Exceptions.ApplicationException;

namespace Turning.Application.Features.ConversationTurns;

/// <summary>
/// Implementa el flujo base de conversación sobre una sesión experimental real.
/// </summary>
public sealed class ConversationTurnService : IConversationTurnService
{
    private readonly IConversationTurnRepository _conversationTurnRepository;
    private readonly IExperimentSessionRepository _experimentSessionRepository;
    private readonly ITextGenerationPort _textGenerationPort;

    /// <summary>
    /// Constructor del servicio de conversación.
    /// </summary>
    public ConversationTurnService(
        IConversationTurnRepository conversationTurnRepository,
        IExperimentSessionRepository experimentSessionRepository,
        ITextGenerationPort textGenerationPort)
    {
        _conversationTurnRepository = conversationTurnRepository;
        _experimentSessionRepository = experimentSessionRepository;
        _textGenerationPort = textGenerationPort;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConversationTurnSnapshot>> ListAsync(Guid ownerUserId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (ownerUserId == Guid.Empty)
            throw new TurningApplicationException("No fue posible resolver el usuario autenticado para consultar la conversación.", "CONVERSATION_INVALID_OWNER");

        await GetOwnedSessionAsync(ownerUserId, sessionId, cancellationToken);

        var turns = await _conversationTurnRepository.ListBySessionAsync(sessionId, cancellationToken);
        return turns.Select(Map).ToArray();
    }

    /// <inheritdoc />
    public async Task<ConversationTurnSnapshot> AddAsync(Guid ownerUserId, Guid sessionId, AddConversationTurnRequest request, CancellationToken cancellationToken = default)
    {
        if (ownerUserId == Guid.Empty)
            throw new TurningApplicationException("No fue posible resolver el usuario autenticado para registrar el mensaje.", "CONVERSATION_INVALID_OWNER");

        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Message))
            throw new TurningApplicationException("El mensaje es obligatorio.", "CONVERSATION_EMPTY_MESSAGE");

        if (request.Message.Trim().Length > 4000)
            throw new TurningApplicationException("El mensaje no puede superar los 4000 caracteres.", "CONVERSATION_MESSAGE_TOO_LONG");

        var session = await GetOwnedSessionAsync(ownerUserId, sessionId, cancellationToken);
        if (session.IsTerminal) throw new TurningApplicationException("Sesión terminal no acepta nuevos turnos.", "SESSION_TERMINAL");
        var sender = ParseSender(request.Sender);
        var existingTurns = await _conversationTurnRepository.ListBySessionAsync(session.Id, cancellationToken);
        var turn = ConversationTurn.Create(session.Id, session.ConversationTurnCount + 1, sender, request.Message);

        await _conversationTurnRepository.AddAsync(turn, cancellationToken);
        session.RegisterConversationTurn();

        if (session.Condition == ExperimentalCondition.AI && sender == ConversationActor.Participant)
        {
            try
            {
                var history = existingTurns.Concat([turn]).ToArray();
                var generatedReply = await _textGenerationPort.GenerateInterlocutorReplyAsync(session, history, cancellationToken);
                if (!string.IsNullOrWhiteSpace(generatedReply))
                {
                    var aiTurn = ConversationTurn.Create(session.Id, session.ConversationTurnCount + 1, ConversationActor.Interlocutor, generatedReply);
                    typeof(ConversationTurn).GetProperty("OriginatingTurnId")!.SetValue(aiTurn, turn.Id);
                    await _conversationTurnRepository.AddAsync(aiTurn, cancellationToken);
                    session.RegisterConversationTurn();
                }
            }
            catch (Exception) { /* degradado: turno participante conservado, IA no bloquea */ }
        }

        await _conversationTurnRepository.SaveChangesAsync(cancellationToken);

        return Map(turn);
    }

    private async Task<ExperimentSession> GetOwnedSessionAsync(Guid ownerUserId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _experimentSessionRepository.GetByIdAsync(sessionId, cancellationToken);

        if (session is null || session.OwnerUserId != ownerUserId)
            throw new NotFoundException("ExperimentSession", sessionId.ToString());

        return session;
    }

    private static ConversationActor ParseSender(string? sender)
    {
        if (string.IsNullOrWhiteSpace(sender))
        {
            return ConversationActor.Participant;
        }

        return sender.Trim().ToUpperInvariant() switch
        {
            "PARTICIPANT" => ConversationActor.Participant,
            "INTERLOCUTOR" => ConversationActor.Interlocutor,
            _ => throw new TurningApplicationException("El emisor debe ser Participant o Interlocutor.", "CONVERSATION_INVALID_SENDER")
        };
    }

    private static ConversationTurnSnapshot Map(ConversationTurn turn)
    {
        return new ConversationTurnSnapshot
        {
            Id = turn.Id,
            SessionId = turn.ExperimentSessionId,
            SequenceNumber = turn.SequenceNumber,
            Sender = turn.Sender.ToString(),
            Message = turn.Message,
            CreatedAtUtc = turn.CreatedAt
        };
    }
}