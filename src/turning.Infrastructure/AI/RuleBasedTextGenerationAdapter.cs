using System.Diagnostics;
using Turning.Application.Interfaces;

namespace Turning.Infrastructure.AI;

/// <summary>
/// Adapter base (baseline) para la generación de texto mientras no responde ningún
/// proveedor real. Es determinístico y basado en reglas simples: no invoca servicios
/// externos, por lo que prácticamente no falla; aun así, mide latencia y reporta su estado
/// como lo exige el contrato.
/// </summary>
/// <remarks>
/// Es el último eslabón de la cadena de proveedores: cuando responde, la conversación
/// continúa pero se marca como degradada, porque el participante no está hablando con el
/// modelo que el experimento pretendía.
/// </remarks>
public sealed class RuleBasedTextGenerationAdapter : ITextGenerationPort
{
    /// <summary>
    /// Nombre con el que este adaptador se identifica en el contrato.
    /// </summary>
    public const string ProviderName = "rule-based";

    private const int MaxExcerptLength = 140;

    /// <inheritdoc />
    public Task<TextGenerationResult> GenerateAsync(TextGenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var text = BuildReply(request.UserInput);
            stopwatch.Stop();

            return Task.FromResult(new TextGenerationResult(
                Text: text,
                Provider: ProviderName,
                LatencyMs: stopwatch.ElapsedMilliseconds,
                Degraded: false,
                Model: ProviderName));
        }
        catch (Exception ex)
        {
            // Modo degradado: nunca debe tumbar la conversación. Se reporta explícitamente
            // en vez de propagar la excepción, tal como exige el contrato del baseline.
            stopwatch.Stop();

            return Task.FromResult(new TextGenerationResult(
                Text: "No fue posible generar una respuesta en este momento.",
                Provider: ProviderName,
                LatencyMs: stopwatch.ElapsedMilliseconds,
                Degraded: true,
                Model: ProviderName,
                FailureReason: ex.GetType().Name));
        }
    }

    private static string BuildReply(string? userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return "Estoy listo para continuar con la sesion experimental.";

        var excerpt = userInput.Trim();
        if (excerpt.Length > MaxExcerptLength)
            excerpt = excerpt[..MaxExcerptLength].TrimEnd() + "...";

        return $"Entiendo: \"{excerpt}\". Sigamos con la siguiente intervencion del experimento.";
    }
}
