using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Turning.Application.Features.ExperimentSessions;
using Turning.Application.Interfaces;
using Turning.Domain.Entities;
using Xunit;
using TurningApplicationException = Turning.Application.Exceptions.ApplicationException;

namespace Turning.Application.Tests;

/// <summary>
/// Pruebas del aislamiento por propietario con interlocutor humano.
/// </summary>
/// <remarks>
/// Es el riesgo declarado en el Definition of Done: fuga de datos entre usuarios. Añadir un
/// segundo usuario a la sesión es justo el cambio que puede romperlo, así que estas pruebas
/// comprueban tanto que el interlocutor entra donde debe como que no entra donde no debe.
/// </remarks>
public class SessionAccessLevelTests
{
    private readonly IExperimentSessionRepository _repo = Substitute.For<IExperimentSessionRepository>();

    private ExperimentSessionService CreateService() =>
        new(_repo, Options.Create(new SessionOptions()));

    private ExperimentSession ArrangeSession(ExperimentalCondition condition, Guid ownerId, Guid? interlocutorId = null)
    {
        var session = ExperimentSession.Create(ownerId, condition);
        if (interlocutorId is not null)
            session.AssignInterlocutor(interlocutorId.Value);

        _repo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        return session;
    }

    [Fact]
    public async Task GetAccessLevelAsync_ShouldReturnOwner_ForTheSessionOwner()
    {
        var ownerId = Guid.NewGuid();
        var session = ArrangeSession(ExperimentalCondition.Human, ownerId);

        var level = await CreateService().GetAccessLevelAsync(session.Id, ownerId, false);

        level.Should().Be(SessionAccessLevel.Owner);
    }

    [Fact]
    public async Task GetAccessLevelAsync_ShouldReturnInterlocutor_ForTheAssignedInterlocutor()
    {
        var interlocutorId = Guid.NewGuid();
        var session = ArrangeSession(ExperimentalCondition.Human, Guid.NewGuid(), interlocutorId);

        var level = await CreateService().GetAccessLevelAsync(session.Id, interlocutorId, false);

        level.Should().Be(SessionAccessLevel.Interlocutor);
    }

    [Fact]
    public async Task GetAccessLevelAsync_ShouldReturnNone_ForAStranger()
    {
        var session = ArrangeSession(ExperimentalCondition.Human, Guid.NewGuid(), Guid.NewGuid());

        var level = await CreateService().GetAccessLevelAsync(session.Id, Guid.NewGuid(), false);

        level.Should().Be(SessionAccessLevel.None);
    }

    [Fact]
    public async Task GetAccessLevelAsync_ShouldReturnNone_ForAMissingSession()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ExperimentSession?)null);

        var level = await CreateService().GetAccessLevelAsync(Guid.NewGuid(), Guid.NewGuid(), false);

        // Indistinguible de una sesión ajena: es lo que impide enumerar GUIDs.
        level.Should().Be(SessionAccessLevel.None);
    }

    [Fact]
    public async Task GetAccessLevelAsync_ShouldKeepOwnerAboveInterlocutor_ForTheOwner()
    {
        var ownerId = Guid.NewGuid();
        var session = ArrangeSession(ExperimentalCondition.Human, ownerId, Guid.NewGuid());

        var level = await CreateService().GetAccessLevelAsync(session.Id, ownerId, false);

        level.Should().Be(SessionAccessLevel.Owner);
        ((int)level).Should().BeGreaterThan((int)SessionAccessLevel.Interlocutor);
    }

    [Fact]
    public async Task ActivateAsync_ShouldRejectTheInterlocutor()
    {
        var interlocutorId = Guid.NewGuid();
        var session = ArrangeSession(ExperimentalCondition.Human, Guid.NewGuid(), interlocutorId);

        var act = async () => await CreateService().ActivateAsync(session.Id, interlocutorId, false);

        // El interlocutor conversa; no gobierna el ciclo de vida del experimento ajeno.
        await act.Should().ThrowAsync<TurningApplicationException>()
            .Where(ex => ex.Code == "SESSION_NOT_FOUND");
    }

    [Fact]
    public async Task CompleteAsync_ShouldRejectTheInterlocutor()
    {
        var interlocutorId = Guid.NewGuid();
        var session = ArrangeSession(ExperimentalCondition.Human, Guid.NewGuid(), interlocutorId);

        var act = async () => await CreateService().CompleteAsync(session.Id, interlocutorId, false);

        await act.Should().ThrowAsync<TurningApplicationException>()
            .Where(ex => ex.Code == "SESSION_NOT_FOUND");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldAllowTheInterlocutor()
    {
        var interlocutorId = Guid.NewGuid();
        var session = ArrangeSession(ExperimentalCondition.Human, Guid.NewGuid(), interlocutorId);

        var snapshot = await CreateService().GetByIdAsync(session.Id, interlocutorId, false);

        // Necesita leer la sesión para saber en qué estado está la conversación.
        snapshot.Id.Should().Be(session.Id);
        snapshot.HasInterlocutor.Should().BeTrue();
    }

    [Fact]
    public async Task IsSessionAccessibleAsync_ShouldStillRequireOwner()
    {
        var interlocutorId = Guid.NewGuid();
        var session = ArrangeSession(ExperimentalCondition.Human, Guid.NewGuid(), interlocutorId);

        // El método antiguo conserva su significado: las rutas que no declaran el opt-in
        // siguen viendo exactamente lo que veían antes de que existiera el interlocutor.
        (await CreateService().IsSessionAccessibleAsync(session.Id, interlocutorId, false)).Should().BeFalse();
        (await CreateService().IsSessionAccessibleAsync(session.Id, session.OwnerUserId, false)).Should().BeTrue();
    }

    [Fact]
    public async Task JoinAsInterlocutorAsync_ShouldPairBySessionCode()
    {
        var session = ArrangeSession(ExperimentalCondition.Human, Guid.NewGuid());
        var joiner = Guid.NewGuid();
        _repo.GetByCodeAsync(session.SessionCode, Arg.Any<CancellationToken>()).Returns(session);

        var snapshot = await CreateService().JoinAsInterlocutorAsync(session.SessionCode, joiner);

        snapshot.HasInterlocutor.Should().BeTrue();
        snapshot.InterlocutorUserId.Should().Be(joiner);
    }

    [Fact]
    public async Task JoinAsInterlocutorAsync_ShouldRejectTheOwner()
    {
        var ownerId = Guid.NewGuid();
        var session = ArrangeSession(ExperimentalCondition.Human, ownerId);
        _repo.GetByCodeAsync(session.SessionCode, Arg.Any<CancellationToken>()).Returns(session);

        var act = async () => await CreateService().JoinAsInterlocutorAsync(session.SessionCode, ownerId);

        await act.Should().ThrowAsync<TurningApplicationException>()
            .Where(ex => ex.Code == "SESSION_SELF_PAIRING");
    }

    [Fact]
    public async Task JoinAsInterlocutorAsync_ShouldRejectAnUnknownCode()
    {
        _repo.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((ExperimentSession?)null);

        var act = async () => await CreateService().JoinAsInterlocutorAsync("EXP-NOEXISTE", Guid.NewGuid());

        await act.Should().ThrowAsync<TurningApplicationException>()
            .Where(ex => ex.Code == "SESSION_NOT_FOUND");
    }

    [Fact]
    public async Task JoinAsInterlocutorAsync_ShouldRejectAiSessions()
    {
        var session = ArrangeSession(ExperimentalCondition.AI, Guid.NewGuid());
        _repo.GetByCodeAsync(session.SessionCode, Arg.Any<CancellationToken>()).Returns(session);

        var act = async () => await CreateService().JoinAsInterlocutorAsync(session.SessionCode, Guid.NewGuid());

        await act.Should().ThrowAsync<TurningApplicationException>()
            .Where(ex => ex.Code == "SESSION_CONFLICT");
    }
}
