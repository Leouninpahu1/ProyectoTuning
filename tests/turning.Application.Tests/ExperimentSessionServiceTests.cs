using FluentAssertions;
using NSubstitute;
using Turning.Application.Exceptions;
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

    [Fact]
    public async Task ListByParticipantAsync_ShouldThrowForbidden_WhenRequesterIsNotOwnerAndNotPrivileged()
    {
        var service = new ExperimentSessionService(_experimentSessionRepository, Microsoft.Extensions.Options.Options.Create(new SessionOptions()));
        var participantId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();

        var act = () => service.ListByParticipantAsync(participantId, requestingUserId, isPrivilegedRequester: false, page: 1, pageSize: 50);

        var ex = await act.Should().ThrowAsync<Turning.Application.Exceptions.ApplicationException>();
        ex.Which.Code.Should().Be("SESSION_FORBIDDEN");
        await _experimentSessionRepository.DidNotReceive().ListByOwnerAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListByParticipantAsync_ShouldReturnSessions_WhenRequesterIsOwner()
    {
        var service = new ExperimentSessionService(_experimentSessionRepository, Microsoft.Extensions.Options.Options.Create(new SessionOptions()));
        var participantId = Guid.NewGuid();
        _experimentSessionRepository.ListByOwnerAsync(participantId, 1, 50, Arg.Any<CancellationToken>()).Returns(new List<ExperimentSession>());
        _experimentSessionRepository.CountByOwnerAsync(participantId, Arg.Any<CancellationToken>()).Returns(0);

        var result = await service.ListByParticipantAsync(participantId, requestingUserId: participantId, isPrivilegedRequester: false, page: 1, pageSize: 50);

        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task ListByParticipantAsync_ShouldReturnSessions_WhenRequesterIsPrivileged()
    {
        var service = new ExperimentSessionService(_experimentSessionRepository, Microsoft.Extensions.Options.Options.Create(new SessionOptions()));
        var participantId = Guid.NewGuid();
        var researcherId = Guid.NewGuid();
        _experimentSessionRepository.ListByOwnerAsync(participantId, 1, 50, Arg.Any<CancellationToken>()).Returns(new List<ExperimentSession>());
        _experimentSessionRepository.CountByOwnerAsync(participantId, Arg.Any<CancellationToken>()).Returns(0);

        var result = await service.ListByParticipantAsync(participantId, requestingUserId: researcherId, isPrivilegedRequester: true, page: 1, pageSize: 50);

        result.Total.Should().Be(0);
    }
}