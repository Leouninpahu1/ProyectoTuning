using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Turning.Application.Features.ConversationTurns;
using Turning.Application.Interfaces;
using Turning.Domain.Entities;
using Xunit;

namespace Turning.Application.Tests;

/// <summary>
/// Pruebas del enrutamiento por condición experimental (RF-EXP-01).
/// </summary>
public class ConversationOrchestratorTests
{
    private readonly ITextGenerationPort _textGenerationPort = Substitute.For<ITextGenerationPort>();

    private ConversationOrchestrator CreateOrchestrator() =>
        new(_textGenerationPort, NullLogger<ConversationOrchestrator>.Instance);

    private static ConversationTurn ParticipantTurn(Guid sessionId, int sequence = 1, string message = "Hola.") =>
        ConversationTurn.Create(sessionId, sequence, ConversationActor.Participant, message);

    [Fact]
    public async Task ResolveReplyAsync_ShouldNotGenerate_ForHumanSessions()
    {
        var orchestrator = CreateOrchestrator();
        var session = ExperimentSession.Create(Guid.NewGuid(), ExperimentalCondition.Human);

        var outcome = await orchestrator.ResolveReplyAsync(session, ParticipantTurn(session.Id), []);

        // En condición Human responde una persona: no generar es el comportamiento correcto.
        outcome.Should().BeNull();
        await _textGenerationPort.DidNotReceive().GenerateAsync(Arg.Any<TextGenerationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveReplyAsync_ShouldNotGenerate_WhenTheTurnComesFromTheInterlocutor()
    {
        var orchestrator = CreateOrchestrator();
        var session = ExperimentSession.Create(Guid.NewGuid(), ExperimentalCondition.AI);
        var interlocutorTurn = ConversationTurn.Create(session.Id, 1, ConversationActor.Interlocutor, "Ya respondí.");

        var outcome = await orchestrator.ResolveReplyAsync(session, interlocutorTurn, []);

        // Si no, la conversación se encadenaría consigo misma.
        outcome.Should().BeNull();
        await _textGenerationPort.DidNotReceive().GenerateAsync(Arg.Any<TextGenerationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveReplyAsync_ShouldSendTrimmedHistory_ForAiSessions()
    {
        var orchestrator = CreateOrchestrator();
        var session = ExperimentSession.Create(Guid.NewGuid(), ExperimentalCondition.AI);
        var history = Enumerable.Range(1, 40)
            .Select(i => ConversationTurn.Create(session.Id, i, ConversationActor.Participant, $"Mensaje {i}"))
            .ToArray();

        _textGenerationPort.GenerateAsync(Arg.Any<TextGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TextGenerationResult("Respuesta.", "rule-based", 3, false));

        await orchestrator.ResolveReplyAsync(session, ParticipantTurn(session.Id, 41, "Ultimo."), history);

        // RF-EXP-02: el contexto es una ventana, no la conversación entera.
        await _textGenerationPort.Received(1).GenerateAsync(
            Arg.Is<TextGenerationRequest>(request =>
                request.SessionId == session.Id &&
                request.UserInput == "Ultimo." &&
                request.ConversationHistory.Count == ConversationHistoryBuilder.MaxHistoryMessages),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveReplyAsync_ShouldReturnDegradedOutcome_WhenThePortThrows()
    {
        var orchestrator = CreateOrchestrator();
        var session = ExperimentSession.Create(Guid.NewGuid(), ExperimentalCondition.AI);

        _textGenerationPort.GenerateAsync(Arg.Any<TextGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns<TextGenerationResult>(_ => throw new HttpRequestException("proveedor caido"));

        var outcome = await orchestrator.ResolveReplyAsync(session, ParticipantTurn(session.Id), []);

        // No propaga: el turno del participante no puede perderse por un fallo externo,
        // pero la degradación se informa en vez de silenciarse.
        outcome.Should().NotBeNull();
        outcome!.Degraded.Should().BeTrue();
        outcome.HasReply.Should().BeFalse();
        outcome.FailureReason.Should().Be(nameof(HttpRequestException));
    }

    [Fact]
    public async Task ResolveReplyAsync_ShouldPropagateCancellation()
    {
        var orchestrator = CreateOrchestrator();
        var session = ExperimentSession.Create(Guid.NewGuid(), ExperimentalCondition.AI);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _textGenerationPort.GenerateAsync(Arg.Any<TextGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns<TextGenerationResult>(_ => throw new OperationCanceledException());

        var act = async () => await orchestrator.ResolveReplyAsync(session, ParticipantTurn(session.Id), [], null, cts.Token);

        // Que el cliente abandone la petición no es una degradación del proveedor.
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ResolveReplyAsync_ShouldCarryProviderMetadata()
    {
        var orchestrator = CreateOrchestrator();
        var session = ExperimentSession.Create(Guid.NewGuid(), ExperimentalCondition.AI);

        _textGenerationPort.GenerateAsync(Arg.Any<TextGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TextGenerationResult(
                Text: "Respuesta.",
                Provider: "openrouter",
                LatencyMs: 850,
                Degraded: true,
                Model: "modelo-gratuito",
                Attempts: 2,
                ProvidersTried: ["openai", "openrouter"]));

        var outcome = await orchestrator.ResolveReplyAsync(session, ParticipantTurn(session.Id), []);

        outcome!.Provider.Should().Be("openrouter");
        outcome.Model.Should().Be("modelo-gratuito");
        outcome.Attempts.Should().Be(2);
        outcome.ProvidersTried.Should().ContainInOrder("openai", "openrouter");
        outcome.Degraded.Should().BeTrue();
    }
}
