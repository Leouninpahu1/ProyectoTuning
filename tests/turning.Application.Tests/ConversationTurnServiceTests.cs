using FluentAssertions;
using NSubstitute;
using Turning.Application.Features.ConversationTurns;
using Turning.Application.Interfaces;
using Turning.Domain.Entities;
using Xunit;

namespace Turning.Application.Tests;

/// <summary>
/// Pruebas del servicio de conversación.
/// </summary>
public class ConversationTurnServiceTests
{
    private readonly IConversationTurnRepository _conversationTurnRepository = Substitute.For<IConversationTurnRepository>();
    private readonly IExperimentSessionRepository _experimentSessionRepository = Substitute.For<IExperimentSessionRepository>();
    private readonly ITextGenerationPort _textGenerationPort = Substitute.For<ITextGenerationPort>();

    [Fact]
    public async Task AddAsync_ShouldPersistParticipantTurnAndAiInterlocutorReply_ForAiSessions()
    {
        // Arrange
        var service = new ConversationTurnService(_conversationTurnRepository, _experimentSessionRepository, _textGenerationPort);
        var ownerUserId = Guid.NewGuid();
        var session = ExperimentSession.Create(ownerUserId, ExperimentalCondition.AI);

        _experimentSessionRepository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _conversationTurnRepository.ListBySessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns([]);
        _textGenerationPort.GenerateInterlocutorReplyAsync(session, Arg.Any<IReadOnlyList<ConversationTurn>>(), Arg.Any<CancellationToken>())
            .Returns(new TextGenerationResult(
                Text: "Respuesta generada por IA.",
                Provider: "mock",
                LatencyMs: 5,
                Degraded: false));

        // Act
        var result = await service.AddAsync(ownerUserId, session.Id, new AddConversationTurnRequest
        {
            Sender = "Participant",
            Message = "Hola, inicio la conversación."
        });

        // Assert
        result.SequenceNumber.Should().Be(1);
        result.Sender.Should().Be("Participant");
        result.Message.Should().Be("Hola, inicio la conversación.");
        session.ConversationTurnCount.Should().Be(2);
        session.Status.Should().Be(ExperimentSessionStatus.Active);
        await _conversationTurnRepository.Received(1).AddAsync(Arg.Is<ConversationTurn>(turn =>
            turn.ExperimentSessionId == session.Id &&
            turn.SequenceNumber == 1 &&
            turn.Sender == ConversationActor.Participant &&
            turn.Message == "Hola, inicio la conversación."), Arg.Any<CancellationToken>());
        await _conversationTurnRepository.Received(1).AddAsync(Arg.Is<ConversationTurn>(turn =>
            turn.ExperimentSessionId == session.Id &&
            turn.SequenceNumber == 2 &&
            turn.Sender == ConversationActor.Interlocutor &&
            turn.Message == "Respuesta generada por IA."), Arg.Any<CancellationToken>());
        await _conversationTurnRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddAsync_ShouldPersistOnlyOneTurn_ForHumanSessions()
    {
        // Arrange
        var service = new ConversationTurnService(_conversationTurnRepository, _experimentSessionRepository, _textGenerationPort);
        var ownerUserId = Guid.NewGuid();
        var session = ExperimentSession.Create(ownerUserId, ExperimentalCondition.Human);

        _experimentSessionRepository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _conversationTurnRepository.ListBySessionAsync(session.Id, Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var result = await service.AddAsync(ownerUserId, session.Id, new AddConversationTurnRequest
        {
            Sender = "Participant",
            Message = "Inicio una sesión humana."
        });

        // Assert
        result.SequenceNumber.Should().Be(1);
        session.ConversationTurnCount.Should().Be(1);
        await _conversationTurnRepository.Received(1).AddAsync(Arg.Is<ConversationTurn>(turn =>
            turn.SequenceNumber == 1 && turn.Sender == ConversationActor.Participant), Arg.Any<CancellationToken>());
        await _textGenerationPort.DidNotReceive().GenerateInterlocutorReplyAsync(Arg.Any<ExperimentSession>(), Arg.Any<IReadOnlyList<ConversationTurn>>(), Arg.Any<CancellationToken>());
    }
}