using FluentAssertions;
using Turning.Domain.Common;
using Turning.Domain.Entities;
using Xunit;

namespace Turning.Domain.Tests;

/// <summary>
/// Pruebas de la entidad UserAccount, centradas en la validación del rol.
/// </summary>
public class UserAccountTests
{
    [Theory]
    [InlineData("Participant")]
    [InlineData("Researcher")]
    [InlineData("Administrator")]
    public void Create_WithKnownRole_ShouldAssignIt(string role)
    {
        var user = UserAccount.Create("user@example.com", "Usuario", "hash-value", role);

        user.Role.Should().Be(role);
    }

    [Theory]
    [InlineData("participant", "Participant")]
    [InlineData("ADMINISTRATOR", "Administrator")]
    [InlineData("  Researcher  ", "Researcher")]
    public void Create_WithKnownRoleInAnyCasing_ShouldNormalizeIt(string role, string esperado)
    {
        var user = UserAccount.Create("user@example.com", "Usuario", "hash-value", role);

        user.Role.Should().Be(esperado);
    }

    [Theory]
    [InlineData("SuperAdmin")]
    [InlineData("root")]
    [InlineData("Admin")]
    public void Create_WithUnknownRole_ShouldThrow(string role)
    {
        Action act = () => UserAccount.Create("user@example.com", "Usuario", "hash-value", role);

        act.Should().Throw<ArgumentException>().WithParameterName("role");
    }

    [Fact]
    public void Create_WithoutRole_ShouldThrow()
    {
        Action act = () => UserAccount.Create("user@example.com", "Usuario", "hash-value", "  ");

        act.Should().Throw<ArgumentException>().WithParameterName("role");
    }
}
