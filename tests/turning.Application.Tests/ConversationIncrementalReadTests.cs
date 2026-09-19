using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Turning.Application.Features.ConversationTurns;
using Turning.Application.Features.Emotions;
using Turning.Application.Features.ExperimentSessions;
using Turning.Application.Interfaces;
using Turning.Domain.Entities;
using Xunit;
using TurningApplicationException = Turning.Application.Exceptions.ApplicationException;

namespace Turning.Application.Tests;

/// <summary>
/// Pruebas de la lectura incremental de la conversación.
/// </summary>
/// <remarks>
/// Es lo que permite al participante ver el mensaje del interlocutor humano sin WebSocket:
/// el cliente sondea pidiendo lo posterior a la última secuencia que ya tiene.
/// </remarks>
public class ConversationIncrementalReadTests
{
    private readonly IConversationTurnRepository _turns = Substitute.For<IConversationTurnRepository>();
    private readonly IExperimentSessionRepository _sessions = Substitute.For<IExperimentSessionRepository>();
    private readonly IConversationOrchestrator _orchestrator = Substitute.For<IConversationOrchestrator>();
    private readonly IExperimentEventPublisher _events = Substitute.For<IExperimentEventPublisher>();
    private readonly ITextEmotionAnalysisService _emotions = Substitute.For<ITextEmotionAnalysisService>();
    private readonly IEmotionReadingRepository _readings = Substitute.For<IEmotionReadingRepository>();

    private ConversationTurnService CreateService() =>
        new(_turns, _sessions, _orchestrator, _events, _emotions, _readings, Options.Create(new SessionOptions()));

    private ExperimentSession ArrangeSession(Guid ownerId)
    {
        var session = ExperimentSession.Create(ownerId, ExperimentalCondition.Human);
        _sessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        return session;
    }

    private static ConversationTurn[] Turns(Guid sessionId, params int[] sequences) =>
        sequences.Select(i => ConversationTurn.Create(sessionId, i, ConversationActor.Participant, $"Mensaje {i}")).ToArray();

    [Fact]
    public async Task ListAsync_ShouldReturnEverything_WhenNoCursorIsGiven()
    {
        var ownerId = Guid.NewGuid();
        var session = ArrangeSession(ownerId);
        _turns.ListBySessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns(Turns(session.Id, 1, 2, 3));

        var result = await CreateService().ListAsync(ownerId, session.Id);

        // Sin cursor, la respuesta es exactamente la de antes: el contrato viejo sigue vivo.
        result.Should().HaveCount(3);
        await _turns.DidNotReceive().ListBySessionAfterAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_ShouldReturnOnlyNewerTurns_WhenACursorIsGiven()
    {
        var ownerId = Guid.NewGuid();
        var session = ArrangeSession(ownerId);
        _turns.ListBySessionAfterAsync(session.Id, Arg.Is(2), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Turns(session.Id, 3, 4));

        var result = await CreateService().ListAsync(ownerId, session.Id, afterSequence: 2);

        result.Should().HaveCount(2);
        result.Select(t => t.SequenceNumber).Should().ContainInOrder(3, 4);
    }

    [Fact]
    public async Task ListAsync_ShouldAcceptZeroAsCursor()
    {
        var ownerId = Guid.NewGuid();
        var session = ArrangeSession(ownerId);
        _turns.ListBySessionAfterAsync(session.Id, Arg.Is(0), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Turns(session.Id, 1, 2));

        // Un cliente que arranca sin nada usa 0 y recibe la conversación desde el principio.
        var result = await CreateService().ListAsync(ownerId, session.Id, afterSequence: 0);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListAsync_ShouldCapTheLimit()
    {
        var ownerId = Guid.NewGuid();
        var session = ArrangeSession(ownerId);
        _turns.ListBySessionAfterAsync(session.Id, Arg.Is(0), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);

        await CreateService().ListAsync(ownerId, session.Id, afterSequence: 0, limit: 10_000);

        // Un cliente no puede pedir la base de datos entera en una sola petición.
        await _turns.Received(1).ListBySessionAfterAsync(
            session.Id, Arg.Is(0), Arg.Is(ConversationTurnService.MaxListLimit), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_ShouldRejectANegativeCursor()
    {
        var ownerId = Guid.NewGuid();
        var session = ArrangeSession(ownerId);

        var act = async () => await CreateService().ListAsync(ownerId, session.Id, afterSequence: -1);

        await act.Should().ThrowAsync<TurningApplicationException>()
            .Where(ex => ex.Code == "CONVERSATION_INVALID_CURSOR");
    }

    [Fact]
    public async Task ListAsync_ShouldRejectANonPositiveLimit()
    {
        var ownerId = Guid.NewGuid();
        var session = ArrangeSession(ownerId);

        var act = async () => await CreateService().ListAsync(ownerId, session.Id, afterSequence: 0, limit: 0);

        await act.Should().ThrowAsync<TurningApplicationException>()
            .Where(ex => ex.Code == "CONVERSATION_INVALID_LIMIT");
    }

    [Fact]
    public async Task ListAsync_ShouldLetTheInterlocutorPoll()
    {
        var interlocutorId = Guid.NewGuid();
        var session = ExperimentSession.Create(Guid.NewGuid(), ExperimentalCondition.Human);
        session.AssignInterlocutor(interlocutorId);
        _sessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _turns.ListBySessionAfterAsync(session.Id, Arg.Is(1), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Turns(session.Id, 2));

        var result = await CreateService().ListAsync(interlocutorId, session.Id, afterSequence: 1);

        // El Salón B necesita sondear igual que el Salón A.
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task ListAsync_ShouldRejectAStrangerEvenWithACursor()
    {
        var session = ArrangeSession(Guid.NewGuid());

        var act = async () => await CreateService().ListAsync(Guid.NewGuid(), session.Id, afterSequence: 0);

        await act.Should().ThrowAsync<Turning.Application.Exceptions.NotFoundException>();
    }
}
