using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Turning.API.Extensions;
using Turning.Application.Interfaces;
using Turning.Domain.Common;
using Turning.Domain.Entities;
using Xunit;

namespace Turning.API.Tests;

public class JwtRoleValidationTests
{
    [Theory]
    [InlineData(UserRoles.Researcher, true)]
    [InlineData(UserRoles.Participant, false)]
    public async Task TokenRole_ShouldMatchTheCurrentStoredRole(string storedRole, bool accepted)
    {
        var users = Substitute.For<IUserAccountRepository>();
        var user = UserAccount.Create("role@example.com", "Role", "hash", storedRole);
        users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SigningKey"] = new string('x', 64)
        }).Build();
        var environment = Substitute.For<IWebHostEnvironment>();
        environment.EnvironmentName.Returns("Production");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(users);
        services.AddJwtAuthentication(configuration, environment);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("Bearer");
        var context = new TokenValidatedContext(new DefaultHttpContext { RequestServices = provider },
            new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)), options)
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, UserRoles.Researcher)
            ], "test"))
        };

        await options.Events.TokenValidated(context);

        if (accepted) context.Result.Should().BeNull();
        else context.Result!.Failure.Should().NotBeNull();
    }
}
