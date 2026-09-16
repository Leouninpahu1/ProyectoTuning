using Microsoft.Extensions.Options;
using Turning.Application.Exceptions;
using System.Text.Json;
using Turning.Application.Features.Emotions;
using Turning.Application.Features.ExperimentSessions;
using Turning.Application.Interfaces;
using Turning.Domain.Entities;
using TurningApplicationException = Turning.Application.Exceptions.ApplicationException;

namespace Turning.Application.Features.ConversationTurns;

/// <summary>
/// Implementa el flujo base de conversación sobre una sesión experimental real.
/// </summary>
public sealed class ConversationTurnService : IConversationTurnService
{
    private const int MaxMessageLength = 4000;

    private readonly IConversationTurnRepository _conversationTurnRepository;
    private readonly IExperimentSessionRepository _experimentSessionRepository;
    private readonly IConversationOrchestrator _orchestrator;
    private readonly IExperimentEventPublisher _eventPublisher;
    private readonly ITextEmotionAnalysisService _emotionAnalysis;
    private readonly IEmotionReadingRepository _emotionReadingRepository;
    private readonly SessionOptions _sessionOptions;

    /// <summary>
    /// Constructor del servicio de conversación.
    /// </summary>
    public ConversationTurnService(
        IConversationTurnRepository conversationTurnRepository,
        IExperimentSessionRepository experimentSessionRepository,
        IConversationOrchestrator orchestrator,
        IExperimentEventPublisher eventPublisher,
        ITextEmotionAnalysisService emotionAnalysis,
        IEmotionReadingRepository emotionReadingRepository,
        IOptions<SessionOptions> sessionOptions)
    {
        _conversationTurnRepository = conversationTurnRepository;
        _experimentSessionRepository = experimentSessionRepository;
        _orchestrator = orchestrator;
        _eventPublisher = eventPublisher;
        _emotionAnalysis = emotionAnalysis;
        _emotionReadingRepository = emotionReadingRepository;
        _sessionOptions = sessionOptions?.Value ?? new SessionOptions();
    }

    /// <summary>
    /// Tope duro de turnos por peticion, aunque el cliente pida mas.
    /// </summary>
    public const int MaxListLimit = 200;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConversationTurnSnapshot>> ListAsync(
        Guid ownerUserId,
        Guid sessionId,
        int? afterSequence = null,
        int limit = MaxListLimit,
        CancellationToken cancellationToken = default)
    {
        if (ownerUserId == Guid.Empty)
            throw new TurningApplicationException("No fue posible resolver el usuario autenticado para consultar la conversación.", "CONVERSATION_INVALID_OWNER");

        if (afterSequence is < 0)
            throw new TurningApplicationException("La secuencia debe ser cero o mayor.", "CONVERSATION_INVALID_CURSOR");

        if (limit <= 0)
            throw new TurningApplicationException("El limite debe ser mayor que cero.", "CONVERSATION_INVALID_LIMIT");

        await GetConversationSessionAsync(ownerUserId, sessionId, cancellationToken);

        var effectiveLimit = Math.Min(limit, MaxListLimit);

        // Sin cursor la respuesta es la misma que antes de que existiera este parametro: un
        // cliente escrito contra el contrato anterior no se entera del cambio.
        var turns = afterSequence is null
            ? await _conversationTurnRepository.ListBySessionAsync(sessionId, cancellationToken)
            : await _conversationTurnRepository.ListBySessionAfterAsync(sessionId, afterSequence.Value, effectiveLimit, cancellationToken);

        return turns.Select(Map).ToArray();
    }

    /// <inheritdoc />
    public async Task<ConversationTurnResult> AddAsync(Guid ownerUserId, Guid sessionId, AddConversationTurnRequest request, CancellationToken cancellationToken = default)
    {
        if (ownerUserId == Guid.Empty)
            throw new TurningApplicationException("No fue posible resolver el usuario autenticado para registrar el mensaje.", "CONVERSATION_INVALID_OWNER");

        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Message))
            throw new TurningApplicationException("El mensaje es obligatorio.", "CONVERSATION_EMPTY_MESSAGE");

        if (request.Message.Trim().Length > MaxMessageLength)
            throw new TurningApplicationException($"El mensaje no puede superar los {MaxMessageLength} caracteres.", "CONVERSATION_MESSAGE_TOO_LONG");

        var (session, sender) = await GetConversationSessionAsync(ownerUserId, sessionId, cancellationToken);
        if (session.IsTerminal)
            throw new TurningApplicationException("Sesión terminal no acepta nuevos turnos.", "SESSION_TERMINAL");

        EnsureSenderMatchesRole(request.Sender, sender);
        var previousTurns = await _conversationTurnRepository.ListBySessionAsync(session.Id, cancellationToken);
        var autoActivation = TimeSpan.FromSeconds(_sessionOptions.DurationSeconds);

        var nextSequence = await _conversationTurnRepository.GetNextSequenceNumberAsync(session.Id, cancellationToken);
        var turn = ConversationTurn.Create(session.Id, nextSequence, sender, request.Message);

        await _conversationTurnRepository.AddAsync(turn, cancellationToken);
        session.RegisterConversationTurn(autoActivation);
        _eventPublisher.Publish(session.Id, ExperimentEventTypes.ConversationTurnAdded, new
        {
            turnId = turn.Id,
            sequenceNumber = turn.SequenceNumber,
            sender = turn.Sender.ToString()
        });

        // El análisis emocional corre sobre el texto del participante (RF-AES-01) y alimenta
        // tanto el avatar como el contexto que recibe la IA. Nunca lanza: como mucho devuelve
        // el resultado neutral de fallback.
        var emotion = _emotionAnalysis.Analyze(turn.Message);
        await PersistEmotionAsync(session, turn, emotion, cancellationToken);

        // El mensaje del participante se hace durable ANTES de salir a la red. Con un solo
        // SaveChanges al final, una caída durante la generación se llevaría por delante el
        // turno que el participante ya dio por enviado.
        await _conversationTurnRepository.SaveChangesAsync(cancellationToken);

        var emotionContext = new Interfaces.EmotionContext(
            emotion.EmotionId, emotion.Confidence, emotion.Intensity, emotion.Source);

        var outcome = await _orchestrator.ResolveReplyAsync(session, turn, previousTurns, emotionContext, cancellationToken);

        if (outcome is null)
        {
            // Condición Human: o bien habló el participante y la respuesta llegará de una
            // persona en otro turno, o bien este turno ya es el del interlocutor humano.
            var humanSource = sender == ConversationActor.Interlocutor
                ? ConversationTurnResult.SourceHuman
                : ConversationTurnResult.SourceNone;

            return Map(turn, source: humanSource, emotion: emotion);
        }

        ConversationTurn? replyTurn = null;
        Guid? degradedEventId = null;

        if (outcome.HasReply)
        {
            replyTurn = ConversationTurn.Create(session.Id, turn.SequenceNumber + 1, ConversationActor.Interlocutor, outcome.Text!);
            replyTurn.LinkOriginatingTurn(turn.Id);
            await _conversationTurnRepository.AddAsync(replyTurn, cancellationToken);
            session.RegisterConversationTurn(autoActivation);

            _eventPublisher.Publish(session.Id, ExperimentEventTypes.AiReplyGenerated, new
            {
                turnId = replyTurn.Id,
                originatingTurnId = turn.Id,
                provider = outcome.Provider,
                model = outcome.Model,
                latencyMs = outcome.LatencyMs,
                degraded = outcome.Degraded
            });
        }

        if (outcome.Degraded)
        {
            // La degradación se registra siempre, haya o no texto: que la conversación siga
            // no significa que el experimento tenga el dato que esperaba.
            degradedEventId = _eventPublisher.Publish(session.Id, ExperimentEventTypes.DegradedOperation, new
            {
                operation = "TextGeneration",
                provider = outcome.Provider,
                failureReason = outcome.FailureReason,
                originatingTurnId = turn.Id,
                producedReply = outcome.HasReply
            });
        }

        await _conversationTurnRepository.SaveChangesAsync(cancellationToken);

        return Map(
            turn,
            source: replyTurn is null ? ConversationTurnResult.SourceNone : ConversationTurnResult.SourceAi,
            replyTurn: replyTurn,
            outcome: outcome,
            degradedEventId: degradedEventId,
            emotion: emotion);
    }

    /// <summary>
    /// Persiste la lectura emocional del turno y su expresión de avatar, y refleja el
    /// resultado en el agregado de la sesión.
    /// </summary>
    private async Task PersistEmotionAsync(
        ExperimentSession session,
        ConversationTurn turn,
        TextEmotionDto emotion,
        CancellationToken cancellationToken)
    {
        var reading = EmotionReading.CreateFromText(
            sessionId: session.Id,
            conversationTurnId: turn.Id,
            emotionId: emotion.EmotionId,
            intensity: emotion.Intensity,
            confidence: emotion.Confidence,
            polarity: emotion.Polarity,
            modelVersion: emotion.ModelVersion,
            scoresJson: emotion.Scores.Count == 0 ? null : JsonSerializer.Serialize(emotion.Scores),
            analysisLatencyMs: emotion.LatencyMs,
            fallbackUsed: emotion.FallbackUsed);

        var expression = AvatarExpression.FromReading(reading);

        await _emotionReadingRepository.AddAsync(reading, expression, cancellationToken);
        session.IncrementEmotionSample(reading.Emotion, expression.ExpressionName);
    }

    private async Task<ExperimentSession> GetOwnedSessionAsync(Guid ownerUserId, Guid sessionId, CancellationToken cancellationToken)
    {
        var (session, _) = await GetConversationSessionAsync(ownerUserId, sessionId, cancellationToken);
        return session;
    }

    /// <summary>
    /// Resuelve la sesion y el papel con el que participa quien llama.
    /// </summary>
    /// <remarks>
    /// El emisor sale de la identidad, no del cuerpo de la peticion: antes el cliente podia
    /// declararse "Interlocutor" y el dueno acababa escribiendo los dos lados de la
    /// conversacion, que es justo lo que specs/007 prohibe y lo que invalidaria el
    /// experimento.
    /// </remarks>
    private async Task<(ExperimentSession Session, ConversationActor Actor)> GetConversationSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await _experimentSessionRepository.GetByIdAsync(sessionId, cancellationToken);

        if (session is null)
            throw new NotFoundException("ExperimentSession", sessionId.ToString());

        if (session.OwnerUserId == userId)
            return (session, ConversationActor.Participant);

        if (session.IsInterlocutor(userId))
            return (session, ConversationActor.Interlocutor);

        // Mismo 404 para la sesion ajena que para la inexistente.
        throw new NotFoundException("ExperimentSession", sessionId.ToString());
    }

    /// <summary>
    /// Comprueba que el emisor declarado por el cliente coincide con el papel real.
    /// </summary>
    /// <remarks>
    /// Se falla de forma ruidosa en vez de ignorar el campo en silencio: un cliente que cree
    /// estar escribiendo como interlocutor y acaba etiquetado como participante generaria
    /// datos de investigacion mal atribuidos, y eso no se detecta mirando la respuesta.
    /// </remarks>
    private static void EnsureSenderMatchesRole(string? declaredSender, ConversationActor actualActor)
    {
        if (string.IsNullOrWhiteSpace(declaredSender))
            return;

        var declared = declaredSender.Trim().ToUpperInvariant() switch
        {
            "PARTICIPANT" => ConversationActor.Participant,
            "INTERLOCUTOR" => ConversationActor.Interlocutor,
            _ => throw new TurningApplicationException("El emisor debe ser Participant o Interlocutor.", "CONVERSATION_INVALID_SENDER")
        };

        if (declared != actualActor)
        {
            throw new TurningApplicationException(
                $"El emisor lo determina el servidor segun quien llama: en esta sesion eres '{actualActor}'. "
                + "Omite el campo 'sender' o envia ese valor.",
                "CONVERSATION_SENDER_NOT_ALLOWED");
        }
    }

    private static ConversationTurnResult Map(
        ConversationTurn turn,
        string source,
        ConversationTurn? replyTurn = null,
        InterlocutorReplyOutcome? outcome = null,
        Guid? degradedEventId = null,
        TextEmotionDto? emotion = null) =>
        new()
        {
            Id = turn.Id,
            SessionId = turn.ExperimentSessionId,
            SequenceNumber = turn.SequenceNumber,
            Sender = turn.Sender.ToString(),
            Message = turn.Message,
            CreatedAtUtc = turn.CreatedAt,
            OriginatingTurnId = turn.OriginatingTurnId,
            InterlocutorTurn = replyTurn is null ? null : Map(replyTurn),
            Source = source,
            Ai = outcome is null || replyTurn is null
                ? null
                : new AiMetadataDto
                {
                    Provider = outcome.Provider,
                    Model = outcome.Model,
                    LatencyMs = outcome.LatencyMs,
                    Attempts = outcome.Attempts,
                    ProvidersTried = outcome.ProvidersTried
                },
            Emotion = emotion,
            Degraded = outcome?.Degraded ?? false,
            DegradedReason = outcome?.Degraded == true ? outcome.FailureReason ?? "TextGenerationDegraded" : null,
            EventId = degradedEventId
        };

    private static ConversationTurnSnapshot Map(ConversationTurn turn)
    {
        return new ConversationTurnSnapshot
        {
            Id = turn.Id,
            SessionId = turn.ExperimentSessionId,
            SequenceNumber = turn.SequenceNumber,
            Sender = turn.Sender.ToString(),
            Message = turn.Message,
            CreatedAtUtc = turn.CreatedAt,
            OriginatingTurnId = turn.OriginatingTurnId
        };
    }
}
