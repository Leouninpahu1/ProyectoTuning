using Turning.Application.Interfaces;
using Turning.Domain.Entities;
using Turning.Infrastructure.Persistence;

namespace Turning.Infrastructure.Repositories;

/// <summary>
/// Repositorio EF Core para lecturas emocionales derivadas de la conversación.
/// </summary>
public sealed class EmotionReadingRepository : IEmotionReadingRepository
{
    private readonly TurningDbContext _dbContext;

    /// <summary>
    /// Constructor del repositorio de lecturas emocionales.
    /// </summary>
    public EmotionReadingRepository(TurningDbContext dbContext) => _dbContext = dbContext;

    /// <inheritdoc />
    public async Task AddAsync(EmotionReading reading, AvatarExpression? expression, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reading);

        await _dbContext.EmotionReadings.AddAsync(reading, cancellationToken);

        if (expression is not null)
            await _dbContext.AvatarExpressions.AddAsync(expression, cancellationToken);
    }
}
