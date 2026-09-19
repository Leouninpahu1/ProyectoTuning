using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Turning.Application.Features.AI;
using Turning.Application.Interfaces;
using Turning.Infrastructure.AI.Providers;

namespace Turning.Infrastructure.AI;

/// <summary>
/// Recorre la cadena de proveedores y devuelve la primera respuesta válida.
/// </summary>
/// <remarks>
/// Es la única implementación de <see cref="ITextGenerationPort"/> que se registra: para la
/// capa de aplicación hay un solo puerto, y que detrás haya OpenAI, OpenRouter o el
/// adaptador por reglas es un detalle de infraestructura.
///
/// El orden lo fija <c>Ai:ProviderOrder</c> y termina siempre en el adaptador por reglas,
/// que no puede fallar: así la conversación nunca se queda sin respuesta, aunque marcada
/// como degradada para que el experimento sepa que ese turno no vino del modelo previsto.
/// </remarks>
public sealed class TextGenerationRouter : ITextGenerationPort
{
    private readonly IReadOnlyList<ITextGenerationProvider> _providers;
    private readonly ProviderCircuitBreaker _circuitBreaker;
    private readonly AiOptions _options;
    private readonly ILogger<TextGenerationRouter> _logger;

    /// <summary>
    /// Constructor del router.
    /// </summary>
    public TextGenerationRouter(
        IEnumerable<ITextGenerationProvider> providers,
        ProviderCircuitBreaker circuitBreaker,
        IOptions<AiOptions> options,
        ILogger<TextGenerationRouter> logger)
    {
        _options = options?.Value ?? new AiOptions();
        _circuitBreaker = circuitBreaker;
        _logger = logger;

        var byName = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        // El orden de la configuración manda; lo que no esté en ella no se usa.
        _providers = _options.ProviderOrder
            .Where(byName.ContainsKey)
            .Select(name => byName[name])
            .ToArray();
    }

    /// <summary>
    /// Proveedores activos, en el orden en que se intentan.
    /// </summary>
    public IReadOnlyList<string> Chain => _providers.Select(p => p.Name).ToArray();

    /// <inheritdoc />
    public async Task<TextGenerationResult> GenerateAsync(TextGenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();
        var budget = TimeSpan.FromMilliseconds(_options.TotalBudgetMs);
        var tried = new List<string>();
        var attempts = 0;
        string? lastFailure = null;

        foreach (var provider in _providers)
        {
            if (!provider.IsConfigured)
            {
                _logger.LogDebug("Se salta el proveedor {Provider}: sin configuración utilizable", provider.Name);
                continue;
            }

            if (!_circuitBreaker.CanAttempt(provider.Name))
            {
                _logger.LogDebug("Se salta el proveedor {Provider}: su circuito está abierto", provider.Name);
                continue;
            }

            var remaining = budget - stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero && !IsTerminal(provider))
            {
                // Sin presupuesto no se intenta nada más, salvo el eslabón final, que es
                // local y responde al instante.
                _logger.LogWarning("Presupuesto de {BudgetMs} ms agotado antes de {Provider}", _options.TotalBudgetMs, provider.Name);
                lastFailure ??= "BudgetExceeded";
                continue;
            }

            tried.Add(provider.Name);

            using var budgetCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (!IsTerminal(provider) && remaining > TimeSpan.Zero)
                budgetCts.CancelAfter(remaining);

            ProviderReply reply;
            try
            {
                reply = await provider.GenerateAsync(request, budgetCts.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                reply = ProviderReply.Failed(ProviderFailureKind.Timeout, "Presupuesto agotado.", 0, 1);
            }
            catch (Exception ex)
            {
                // Un proveedor que lanza en vez de devolver un fallo clasificado no debe
                // tumbar la cadena entera.
                _logger.LogError(ex, "El proveedor {Provider} lanzó una excepción inesperada", provider.Name);
                reply = ProviderReply.Failed(ProviderFailureKind.Permanent, ex.GetType().Name, 0, 1);
            }

            attempts += reply.Attempts;

            if (reply.Success)
            {
                _circuitBreaker.RecordSuccess(provider.Name);
                stopwatch.Stop();

                return new TextGenerationResult(
                    Text: reply.Text!,
                    Provider: provider.Name,
                    LatencyMs: stopwatch.ElapsedMilliseconds,
                    Degraded: IsTerminal(provider),
                    Model: reply.Model,
                    Attempts: attempts,
                    FailureReason: lastFailure,
                    ProvidersTried: tried);
            }

            lastFailure = $"{provider.Name}:{reply.Failure}";
            _circuitBreaker.RecordFailure(provider.Name);

            if (reply.DisablesProvider)
            {
                // Clave inválida o sin saldo: insistir en los turnos siguientes solo gastaría
                // el presupuesto de todos ellos.
                _circuitBreaker.Disable(provider.Name);
                _logger.LogError(
                    "El proveedor {Provider} queda fuera de la cadena: {Failure}. {Detail}",
                    provider.Name, reply.Failure, reply.Detail);
            }
            else
            {
                _logger.LogWarning(
                    "El proveedor {Provider} falló con {Failure}; se pasa al siguiente. {Detail}",
                    provider.Name, reply.Failure, reply.Detail);
            }
        }

        // Ningún proveedor respondió, ni siquiera el terminal: no debería pasar, pero la
        // conversación tiene que seguir de todos modos.
        stopwatch.Stop();
        _logger.LogError("Ningún proveedor pudo generar respuesta. Recorridos: {Providers}", string.Join(", ", tried));

        return new TextGenerationResult(
            Text: string.Empty,
            Provider: "none",
            LatencyMs: stopwatch.ElapsedMilliseconds,
            Degraded: true,
            Model: null,
            Attempts: attempts,
            FailureReason: lastFailure ?? "NoProviderAvailable",
            ProvidersTried: tried);
    }

    private static bool IsTerminal(ITextGenerationProvider provider) =>
        string.Equals(provider.Name, AiOptions.RuleBasedProviderName, StringComparison.OrdinalIgnoreCase);
}
