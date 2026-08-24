using FluentAssertions;
using NSubstitute;
using Turning.Application.Features.ExperimentSessions;
using Turning.Application.Interfaces;
using Turning.Domain.Entities;
using Xunit;

namespace Turning.Application.Tests;

/// <summary>
/// Pruebas del servicio de sesiones experimentales.
/// </summary>
public class ExperimentSessionServiceTests
{
    private readonly IExperimentSessionRepository _experimentSessionRepository = Substitute.For<IExperimentSessionRepository>();

    [Fact]
    public async Task CreateBootstrapSessionAsync_ShouldPersistSessionWithInitialState()
    {
        var service = new ExperimentSessionService(_experimentSessionRepository, Microsoft.Extensions.Options.Options.Create(new SessionOptions()));
        var ownerUserId = Guid.NewGuid();

        // Act
        var result = await service.CreateBootstrapSessionAsync(ownerUserId, new CreateExperimentSessionRequest
        {
            PreferredCondition = "AI"
        });

        // Assert
        result.Condition.Should().Be("AI");
        result.Status.Should().Be("Created");
        result.AvatarState.Should().Be("Neutral");
        result.ConversationTurnCount.Should().Be(0);
        result.EmotionSampleCount.Should().Be(0);
        await _experimentSessionRepository.Received(1).AddAsync(Arg.Is<ExperimentSession>(session =>
            session.OwnerUserId == ownerUserId &&
            session.Condition == ExperimentalCondition.AI &&
            session.Status == ExperimentSessionStatus.Created), Arg.Any<CancellationToken>());
        await _experimentSessionRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}