using Microsoft.EntityFrameworkCore;
using Turning.Application.Interfaces;
using Turning.Domain.Entities;
using Turning.Infrastructure.Persistence;

namespace Turning.Infrastructure.Repositories;

/// <summary>
/// Repositorio EF Core para turnos conversacionales.
/// </summary>
public sealed class ConversationTurnRepository : IConversationTurnRepository
{
    private readonly TurningDbContext _dbContext;

    /// <summary>
    /// Constructor del repositorio de conversación.
    /// </summary>
    public ConversationTurnRepository(TurningDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task AddAsync(ConversationTurn turn, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turn);
        await _dbContext.ConversationTurns.AddAsync(turn, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConversationTurn>> ListBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ConversationTurns
            .Where(turn => turn.ExperimentSessionId == sessionId && !turn.IsDeleted)
            .OrderBy(turn => turn.SequenceNumber)
            .ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConversationTurn>> ListBySessionAfterAsync(Guid sessionId, int afterSequence, int limit, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ConversationTurns
            .Where(turn => turn.ExperimentSessionId == sessionId && !turn.IsDeleted && turn.SequenceNumber > afterSequence)
            .OrderBy(turn => turn.SequenceNumber)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetNextSequenceNumberAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        // Incluye los turnos borrados logicamente a proposito: el indice unico
        // (ExperimentSessionId, SequenceNumber) no filtra por IsDeleted, asi que reutilizar
        // la secuencia de un turno borrado chocaria igual.
        var maxSequence = await _dbContext.ConversationTurns
            .Where(turn => turn.ExperimentSessionId == sessionId)
            .MaxAsync(turn => (int?)turn.SequenceNumber, cancellationToken);

        return (maxSequence ?? 0) + 1;
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}