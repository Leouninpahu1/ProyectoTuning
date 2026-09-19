using FluentAssertions;
using Turning.Application.Features.ConversationTurns;
using Turning.Application.Interfaces;
using Turning.Domain.Entities;
using Xunit;

namespace Turning.Application.Tests;

/// <summary>
/// Pruebas del recorte de historial que se envía al proveedor de IA (RF-EXP-02).
/// </summary>
public class ConversationHistoryBuilderTests
{
    private static ConversationTurn[] BuildTurns(int count, Guid sessionId) =>
        Enumerable.Range(1, count)
            .Select(i => ConversationTurn.Create(
                sessionId,
                i,
                i % 2 == 1 ? ConversationActor.Participant : ConversationActor.Interlocutor,
                $"Mensaje {i}"))
            .ToArray();

    [Fact]
    public void Build_ShouldKeepTheMostRecentMessages_WhenHistoryExceedsTheWindow()
    {
        var turns = BuildTurns(40, Guid.NewGuid());

        var history = ConversationHistoryBuilder.Build(turns);

        history.Should().HaveCount(ConversationHistoryBuilder.MaxHistoryMessages);
        history[^1].Content.Should().Be("Mensaje 40");
        history[0].Content.Should().Be("Mensaje 26");
    }

    [Fact]
    public void Build_ShouldReturnEverything_WhenHistoryFitsInTheWindow()
    {
        var turns = BuildTurns(3, Guid.NewGuid());

        var history = ConversationHistoryBuilder.Build(turns);

        history.Should().HaveCount(3);
    }

    [Fact]
    public void Build_ShouldMapSendersToChatRoles()
    {
        var sessionId = Guid.NewGuid();
        var turns = BuildTurns(2, sessionId);

        var history = ConversationHistoryBuilder.Build(turns);

        history[0].Role.Should().Be(ChatMessage.RoleUser);
        history[1].Role.Should().Be(ChatMessage.RoleAssistant);
    }

    [Fact]
    public void Build_ShouldOrderBySequence_EvenIfTheInputIsShuffled()
    {
        var sessionId = Guid.NewGuid();
        var turns = BuildTurns(5, sessionId).Reverse().ToArray();

        var history = ConversationHistoryBuilder.Build(turns);

        history.Select(message => message.Content)
            .Should().ContainInOrder("Mensaje 1", "Mensaje 2", "Mensaje 3", "Mensaje 4", "Mensaje 5");
    }

    [Fact]
    public void Build_ShouldReturnEmpty_WhenThereIsNoHistory()
    {
        ConversationHistoryBuilder.Build([]).Should().BeEmpty();
    }
}
