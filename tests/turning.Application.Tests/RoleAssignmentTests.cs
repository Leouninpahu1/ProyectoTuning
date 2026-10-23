using FluentAssertions;
using NSubstitute;
using Turning.Application.Features.Auth;
using Turning.Application.Interfaces;
using Turning.Domain.Common;
using Turning.Domain.Entities;
using Xunit;

namespace Turning.Application.Tests;

public class RoleAssignmentTests
{
    [Theory]
    [InlineData(UserRoles.Participant)]
    [InlineData(UserRoles.Researcher)]
    public async Task NonAdministrator_ShouldNotAssignRoles(string actorRole)
    {
        var users = Substitute.For<IUserAccountRepository>();
        var actor = UserAccount.Create("actor@example.com", "Actor", "hash", actorRole);
        users.GetByIdAsync(actor.Id, Arg.Any<CancellationToken>()).Returns(actor);
        var service = new AuthService(users, Substitute.For<IPasswordHasherService>(), Substitute.For<ITokenService>());

        var action = () => service.AssignRoleAsync(actor.Id, Guid.NewGuid(), UserRoles.Administrator);

        (await action.Should().ThrowAsync<Turning.Application.Exceptions.ApplicationException>()).Which.Code.Should().Be("AUTH_FORBIDDEN");
        await users.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Administrator_ShouldAssignACanonicalRole()
    {
        var users = Substitute.For<IUserAccountRepository>();
        var actor = UserAccount.Create("admin@example.com", "Admin", "hash", UserRoles.Administrator);
        var participant = UserAccount.Create("participant@example.com", "Participant", "hash", UserRoles.Participant);
        users.GetByIdAsync(actor.Id, Arg.Any<CancellationToken>()).Returns(actor);
        users.GetByIdAsync(participant.Id, Arg.Any<CancellationToken>()).Returns(participant);
        var service = new AuthService(users, Substitute.For<IPasswordHasherService>(), Substitute.For<ITokenService>());

        var result = await service.AssignRoleAsync(actor.Id, participant.Id, " researcher ");

        result.Role.Should().Be(UserRoles.Researcher);
        participant.Role.Should().Be(UserRoles.Researcher);
        await users.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
