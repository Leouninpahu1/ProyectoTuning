using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Turning.Domain.Common;
using Turning.Domain.Entities;
using Turning.Infrastructure.Persistence;
using Turning.Infrastructure.Services;
using Xunit;

namespace Turning.Infrastructure.Tests;

/// <summary>
/// Pruebas de integración para la asignación balanceada de condición experimental,
/// contra SQL Server LocalDB (único proveedor soportado desde 2026-08-29).
/// </summary>
public class BalancedAssignmentServiceTests : IDisposable
{
    private readonly TurningDbContext _dbContext;
    private readonly Guid _ownerId;

    public BalancedAssignmentServiceTests()
    {
        var databaseName = $"TurningTests_{Guid.NewGuid():N}";
        var connectionString =
            $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<TurningDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        _dbContext = new TurningDbContext(options);
        _dbContext.Database.EnsureCreated();

        var owner = UserAccount.Create("assignment-owner@example.com", "Assignment Owner", "hash-value", UserRoles.Participant);
        _dbContext.UserAccounts.Add(owner);
        _dbContext.SaveChanges();
        _ownerId = owner.Id;
    }

    [Fact]
    public async Task AssignAsync_ShouldChooseHuman_WhenHumanCountIsLower()
    {
        await SeedSessionsAsync(human: 1, ai: 3);
        var service = new BalancedAssignmentService(_dbContext);
        var newSessionId = Guid.NewGuid();

        var result = await service.AssignAsync(newSessionId, preferred: null);

        result.Condition.Should().Be(ExperimentalCondition.Human);
        result.Strategy.Should().Be("CountBalanced");
    }

    [Fact]
    public async Task AssignAsync_ShouldChooseAI_WhenAICountIsLower()
    {
        await SeedSessionsAsync(human: 3, ai: 1);
        var service = new BalancedAssignmentService(_dbContext);
        var newSessionId = Guid.NewGuid();

        var result = await service.AssignAsync(newSessionId, preferred: null);

        result.Condition.Should().Be(ExperimentalCondition.AI);
    }

    [Fact]
    public async Task AssignAsync_ShouldChooseHumanDeterministically_OnTie()
    {
        await SeedSessionsAsync(human: 2, ai: 2);
        var service = new BalancedAssignmentService(_dbContext);
        var newSessionId = Guid.NewGuid();

        var result = await service.AssignAsync(newSessionId, preferred: null);

        result.Condition.Should().Be(ExperimentalCondition.Human);
        result.Reason.Should().Contain("Tie");
    }

    [Fact]
    public async Task AssignAsync_ShouldReturnExistingAssignment_WhenSessionAlreadyAssigned()
    {
        var service = new BalancedAssignmentService(_dbContext);
        var session = ExperimentSession.Create(_ownerId, ExperimentalCondition.Human);
        await _dbContext.ExperimentSessions.AddAsync(session);
        await _dbContext.SaveChangesAsync();
        var sessionId = session.Id;

        var first = await service.AssignAsync(sessionId, preferred: null);
        await _dbContext.SaveChangesAsync();

        var second = await service.AssignAsync(sessionId, preferred: null);

        second.Id.Should().Be(first.Id);
        second.Condition.Should().Be(first.Condition);
    }

    private async Task SeedSessionsAsync(int human, int ai)
    {
        for (var i = 0; i < human; i++)
            await _dbContext.ExperimentSessions.AddAsync(ExperimentSession.Create(_ownerId, ExperimentalCondition.Human));
        for (var i = 0; i < ai; i++)
            await _dbContext.ExperimentSessions.AddAsync(ExperimentSession.Create(_ownerId, ExperimentalCondition.AI));
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
