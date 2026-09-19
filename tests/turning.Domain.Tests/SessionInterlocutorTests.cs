using FluentAssertions;
using Turning.Domain.Entities;
using Turning.Domain.Exceptions;
using Xunit;

namespace Turning.Domain.Tests;

/// <summary>
/// Pruebas del emparejamiento con el interlocutor humano.
/// </summary>
public class SessionInterlocutorTests
{
    private static ExperimentSession HumanSession() =>
        ExperimentSession.Create(Guid.NewGuid(), ExperimentalCondition.Human);

    [Fact]
    public void AssignInterlocutor_ShouldPairTheSession()
    {
        var session = HumanSession();
        var interlocutor = Guid.NewGuid();

        session.AssignInterlocutor(interlocutor);

        session.InterlocutorUserId.Should().Be(interlocutor);
        session.InterlocutorJoinedAtUtc.Should().NotBeNull();
        session.IsInterlocutor(interlocutor).Should().BeTrue();
        session.IsAwaitingInterlocutor.Should().BeFalse();
    }

    [Fact]
    public void AssignInterlocutor_ShouldRejectAiSessions()
    {
        var session = ExperimentSession.Create(Guid.NewGuid(), ExperimentalCondition.AI);

        var act = () => session.AssignInterlocutor(Guid.NewGuid());

        // En condición AI responde el modelo: un interlocutor humano ahí no significa nada.
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AssignInterlocutor_ShouldRejectTheOwner()
    {
        var ownerId = Guid.NewGuid();
        var session = ExperimentSession.Create(ownerId, ExperimentalCondition.Human);

        var act = () => session.AssignInterlocutor(ownerId);

        // Es justo lo que el frontend permitía hacer con un desplegable: hablar consigo mismo.
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AssignInterlocutor_ShouldRejectASecondInterlocutor()
    {
        var session = HumanSession();
        session.AssignInterlocutor(Guid.NewGuid());

        var act = () => session.AssignInterlocutor(Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AssignInterlocutor_ShouldBeIdempotentForTheSameUser()
    {
        var session = HumanSession();
        var interlocutor = Guid.NewGuid();
        session.AssignInterlocutor(interlocutor);
        var joinedAt = session.InterlocutorJoinedAtUtc;

        var act = () => session.AssignInterlocutor(interlocutor);

        // Reintentar unirse tras recargar la página no puede ser un error.
        act.Should().NotThrow();
        session.InterlocutorJoinedAtUtc.Should().Be(joinedAt);
    }

    [Fact]
    public void AssignInterlocutor_ShouldRejectTerminalSessions()
    {
        var session = HumanSession();
        session.Cancel("fin del experimento");

        var act = () => session.AssignInterlocutor(Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ReleaseInterlocutor_ShouldFreeTheSession()
    {
        var session = HumanSession();
        session.AssignInterlocutor(Guid.NewGuid());

        session.ReleaseInterlocutor();

        session.InterlocutorUserId.Should().BeNull();
        session.IsAwaitingInterlocutor.Should().BeTrue();
    }

    [Fact]
    public void IsInterlocutor_ShouldBeFalse_ForAnyoneElse()
    {
        var session = HumanSession();
        session.AssignInterlocutor(Guid.NewGuid());

        session.IsInterlocutor(Guid.NewGuid()).Should().BeFalse();
        session.IsInterlocutor(Guid.Empty).Should().BeFalse();
    }

    [Fact]
    public void IsAwaitingInterlocutor_ShouldBeFalse_ForAiSessions()
    {
        ExperimentSession.Create(Guid.NewGuid(), ExperimentalCondition.AI)
            .IsAwaitingInterlocutor.Should().BeFalse();
    }
}
