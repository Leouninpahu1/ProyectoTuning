using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Turning.Domain.Entities;
using Turning.Infrastructure.Persistence;
using Turning.Infrastructure.Repositories;
using Xunit;

namespace Turning.Infrastructure.Tests;

/// <summary>
/// Pruebas de integración para la capa Infrastructure.
/// </summary>
public class InfrastructureIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TurningDbContext _dbContext;

    /// <summary>
    /// Inicializa una base SQLite en memoria para la prueba.
    /// </summary>
    public InfrastructureIntegrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TurningDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TurningDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task UserAccountRepository_ShouldReturnPersistedUserByEmail()
    {
        // Arrange
        var repository = new UserAccountRepository(_dbContext);
        var user = UserAccount.Create("auth@example.com", "Auth User", "hash-value");

        await repository.AddAsync(user);
        await repository.SaveChangesAsync();

        // Act
        var result = await repository.GetByEmailAsync("auth@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("auth@example.com");
        result.FullName.Should().Be("Auth User");
    }

    [Fact]
    public async Task ExperimentSessionRepository_ShouldReturnLatestSessionForOwner()
    {
        // Arrange
        var repository = new ExperimentSessionRepository(_dbContext);
        var owner = UserAccount.Create("owner@example.com", "Session Owner", "hash-value");
        _dbContext.UserAccounts.Add(owner);
        await _dbContext.SaveChangesAsync();
        var ownerUserId = owner.Id;
        var olderSession = ExperimentSession.Create(ownerUserId, ExperimentalCondition.Human);
        var newerSession = ExperimentSession.Create(ownerUserId, ExperimentalCondition.AI);

        await repository.AddAsync(olderSession);
        await repository.SaveChangesAsync();
        await Task.Delay(10);
        await repository.AddAsync(newerSession);
        await repository.SaveChangesAsync();

        // Act
        var result = await repository.GetLatestByOwnerAsync(ownerUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(newerSession.Id);
        result.Condition.Should().Be(ExperimentalCondition.AI);
    }

    [Fact]
    public async Task ConversationTurnRepository_ShouldReturnTurnsOrderedBySequence()
    {
        // Arrange
        var sessionRepository = new ExperimentSessionRepository(_dbContext);
        var conversationRepository = new ConversationTurnRepository(_dbContext);
        var owner = UserAccount.Create("conversation-owner@example.com", "Conversation Owner", "hash-value");
        _dbContext.UserAccounts.Add(owner);
        await _dbContext.SaveChangesAsync();
        var session = ExperimentSession.Create(owner.Id, ExperimentalCondition.AI);

        await sessionRepository.AddAsync(session);
        await sessionRepository.SaveChangesAsync();

        await conversationRepository.AddAsync(ConversationTurn.Create(session.Id, 1, ConversationActor.Participant, "Hola"));
        await conversationRepository.AddAsync(ConversationTurn.Create(session.Id, 2, ConversationActor.Interlocutor, "Hola, te escucho."));
        await conversationRepository.SaveChangesAsync();

        // Act
        var result = await conversationRepository.ListBySessionAsync(session.Id);

        // Assert
        result.Should().HaveCount(2);
        result[0].SequenceNumber.Should().Be(1);
        result[0].Message.Should().Be("Hola");
        result[1].SequenceNumber.Should().Be(2);
        result[1].Sender.Should().Be(ConversationActor.Interlocutor);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
