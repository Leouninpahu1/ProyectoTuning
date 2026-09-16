using Turning.Application.Features.AI;
using Turning.Application.Interfaces;

namespace Turning.Infrastructure.AI.Providers;

/// <summary>
/// Envuelve <see cref="RuleBasedTextGenerationAdapter"/> como eslabón final de la cadena.
/// </summary>
/// <remarks>
/// Siempre está configurado y no hace entrada/salida, así que no puede fallar: es lo que
/// garantiza que la conversación nunca se quede sin respuesta. Cuando responde él, el router
/// marca el resultado como degradado, porque el participante no está hablando con el modelo
/// que el experimento pretendía medir.
/// </remarks>
public sealed class RuleBasedTextGenerationProvider : ITextGenerationProvider
{
    private readonly RuleBasedTextGenerationAdapter _adapter;

    /// <summary>
    /// Constructor del proveedor terminal.
    /// </summary>
    public RuleBasedTextGenerationProvider(RuleBasedTextGenerationAdapter adapter) => _adapter = adapter;

    /// <inheritdoc />
    public string Name => AiOptions.RuleBasedProviderName;

    /// <inheritdoc />
    public bool IsConfigured => true;

    /// <inheritdoc />
    public async Task<ProviderReply> GenerateAsync(TextGenerationRequest request, CancellationToken cancellationToken)
    {
        var result = await _adapter.GenerateAsync(request, cancellationToken);

        return result.Degraded || string.IsNullOrWhiteSpace(result.Text)
            ? ProviderReply.Failed(ProviderFailureKind.Permanent, result.FailureReason, result.LatencyMs, 1)
            : ProviderReply.Ok(result.Text, result.Model, result.LatencyMs, 1);
    }
}
