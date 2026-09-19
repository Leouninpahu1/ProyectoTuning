using System.Text.Json;
using Turning.Application.Interfaces;
using Turning.Domain.Entities;
using Turning.Infrastructure.Persistence;

namespace Turning.Infrastructure.Services;

/// <summary>
/// Publica eventos de sesión sobre <see cref="TurningDbContext"/>.
/// </summary>
/// <remarks>
/// Encola sin guardar: comparte la unidad de trabajo con los repositorios, de modo que un
/// evento nunca queda persistido sin el dato que lo motivó, ni al revés.
/// </remarks>
public sealed class ExperimentEventPublisher : IExperimentEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Límite de la columna PayloadJson.
    /// </summary>
    private const int MaxPayloadLength = 4000;

    private readonly TurningDbContext _dbContext;

    /// <summary>
    /// Constructor del publicador de eventos.
    /// </summary>
    public ExperimentEventPublisher(TurningDbContext dbContext) => _dbContext = dbContext;

    /// <inheritdoc />
    public Guid Publish(Guid sessionId, string type, object payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        if (json.Length > MaxPayloadLength)
        {
            // Truncar antes que reventar el insert: el evento es trazabilidad, no puede
            // tumbar la operación que lo generó.
            json = JsonSerializer.Serialize(
                new { truncated = true, originalLength = json.Length },
                SerializerOptions);
        }

        var experimentEvent = ExperimentEvent.Create(sessionId, type, json);
        _dbContext.ExperimentEvents.Add(experimentEvent);
        return experimentEvent.Id;
    }
}
