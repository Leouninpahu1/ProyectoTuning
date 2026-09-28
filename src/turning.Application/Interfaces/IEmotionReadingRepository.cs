using Turning.Domain.Entities;

namespace Turning.Application.Interfaces;

/// <summary>
/// Contrato de persistencia para lecturas emocionales derivadas de la conversación.
/// </summary>
/// <remarks>
/// No expone <c>SaveChanges</c> a propósito: la lectura emocional se guarda en la misma
/// unidad de trabajo que el turno que la originó.
/// </remarks>
public interface IEmotionReadingRepository
{
    /// <summary>
    /// Encola una lectura emocional y su expresión de avatar derivada.
    /// </summary>
    Task AddAsync(EmotionReading reading, AvatarExpression? expression, CancellationToken cancellationToken = default);
}
