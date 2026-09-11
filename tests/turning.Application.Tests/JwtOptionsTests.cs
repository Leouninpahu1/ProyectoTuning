using FluentAssertions;
using Turning.Application.Features.Auth;
using Xunit;

namespace Turning.Application.Tests;

/// <summary>
/// Pruebas de la validación de configuración JWT (secretos por entorno).
/// </summary>
public class JwtOptionsTests
{
    private const string ValidKey = "3Qv8Zt6yR1pLmN4sK7dW0xB2cF5gH9jA8eU3iO6yT1rP";

    private static JwtOptions ValidOptions() => new()
    {
        Issuer = "Turning.API",
        Audience = "Turning.Web",
        SigningKey = ValidKey,
        ExpirationMinutes = 120
    };

    [Fact]
    public void Validate_ShouldReturnNoErrors_ForUsableConfiguration()
    {
        ValidOptions().Validate().Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldFail_WhenSigningKeyIsMissing()
    {
        var options = ValidOptions();
        options.SigningKey = null;

        options.Validate().Should().ContainSingle().Which.Should().Contain("Jwt:SigningKey");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("clave-corta")]
    public void Validate_ShouldFail_WhenSigningKeyIsBlankOrTooShort(string signingKey)
    {
        var options = ValidOptions();
        options.SigningKey = signingKey;

        options.Validate().Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_ShouldFail_WhenSigningKeyWasVersionedInTheRepository()
    {
        var options = ValidOptions();
        options.SigningKey = "please-change-this-development-key-with-at-least-32-chars";

        options.Validate().Should().ContainSingle().Which.Should().Contain("revocadas");
    }

    [Fact]
    public void Validate_ShouldFail_WhenExpirationIsNotPositive()
    {
        var options = ValidOptions();
        options.ExpirationMinutes = 0;

        options.Validate().Should().ContainSingle().Which.Should().Contain("ExpirationMinutes");
    }

    [Fact]
    public void Validate_ShouldReportEveryProblemAtOnce()
    {
        var options = new JwtOptions { Issuer = "", Audience = "", SigningKey = null, ExpirationMinutes = -1 };

        options.Validate().Should().HaveCount(4);
    }

    [Fact]
    public void IsRevokedSigningKey_ShouldDetectTheKeysThatWerePublishedInGit()
    {
        foreach (var revoked in JwtOptions.RevokedSigningKeys)
        {
            JwtOptions.IsRevokedSigningKey(revoked).Should().BeTrue();
        }

        JwtOptions.IsRevokedSigningKey(ValidKey).Should().BeFalse();
        JwtOptions.IsRevokedSigningKey(null).Should().BeFalse();
    }
}
