using System.Collections.Concurrent;

namespace Turning.Infrastructure.AI.Providers;

/// <summary>
/// Estado del circuito de un proveedor (RF-EXP-07).
/// </summary>
public enum CircuitState
{
    /// <summary>Operación normal.</summary>
    Closed,

    /// <summary>Bloqueado: el proveedor se salta sin intentarlo.</summary>
    Open,

    /// <summary>Probando si el proveedor se recuperó.</summary>
    HalfOpen
}

/// <summary>
/// Corta el tráfico hacia un proveedor que está fallando de forma sostenida.
/// </summary>
/// <remarks>
/// Implementación propia, y a propósito: RF-EXP-07 pide "N fallos consecutivos en una
/// ventana de tiempo", que es un conteo exacto. Las librerías de resiliencia habituales
/// abren por <i>proporción</i> de fallos sobre una ventana de muestreo, así que cumplir el
/// requisito al pie de la letra con ellas exige aproximarlo y explicar la diferencia. Esto
/// son unas pocas decenas de líneas, se prueba sin esperar en tiempo real y dice exactamente
/// lo que el requisito pide.
///
/// Es singleton y seguro para varios hilos: el estado del circuito es del proceso, no de la
/// petición.
/// </remarks>
public sealed class ProviderCircuitBreaker
{
    private sealed class CircuitEntry
    {
        public readonly Queue<DateTimeOffset> Failures = new();
        public DateTimeOffset? OpenedAt;
        public bool Disabled;
    }

    private readonly ConcurrentDictionary<string, CircuitEntry> _circuits = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _failureThreshold;
    private readonly TimeSpan _window;
    private readonly TimeSpan _breakDuration;
    private readonly TimeProvider _timeProvider;
    private readonly Lock _gate = new();

    /// <summary>
    /// Constructor del cortacircuitos.
    /// </summary>
    /// <param name="failureThreshold">Fallos que abren el circuito.</param>
    /// <param name="windowSeconds">Ventana en la que se cuentan esos fallos.</param>
    /// <param name="breakSeconds">Tiempo que permanece abierto.</param>
    /// <param name="timeProvider">Reloj, inyectable para poder probar sin esperas reales.</param>
    public ProviderCircuitBreaker(int failureThreshold, int windowSeconds, int breakSeconds, TimeProvider? timeProvider = null)
    {
        _failureThreshold = Math.Max(1, failureThreshold);
        _window = TimeSpan.FromSeconds(Math.Max(1, windowSeconds));
        _breakDuration = TimeSpan.FromSeconds(Math.Max(1, breakSeconds));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Estado actual del circuito de un proveedor.
    /// </summary>
    public CircuitState GetState(string provider)
    {
        lock (_gate)
        {
            var entry = _circuits.GetOrAdd(provider, _ => new CircuitEntry());

            if (entry.Disabled)
                return CircuitState.Open;

            if (entry.OpenedAt is null)
                return CircuitState.Closed;

            return _timeProvider.GetUtcNow() - entry.OpenedAt.Value >= _breakDuration
                ? CircuitState.HalfOpen
                : CircuitState.Open;
        }
    }

    /// <summary>
    /// Indica si se puede intentar una llamada al proveedor.
    /// </summary>
    public bool CanAttempt(string provider) => GetState(provider) != CircuitState.Open;

    /// <summary>
    /// Registra una llamada correcta: cierra el circuito y limpia el historial.
    /// </summary>
    public void RecordSuccess(string provider)
    {
        lock (_gate)
        {
            var entry = _circuits.GetOrAdd(provider, _ => new CircuitEntry());
            entry.Failures.Clear();
            entry.OpenedAt = null;
        }
    }

    /// <summary>
    /// Registra un fallo y abre el circuito si se alcanzó el umbral dentro de la ventana.
    /// </summary>
    public void RecordFailure(string provider)
    {
        lock (_gate)
        {
            var entry = _circuits.GetOrAdd(provider, _ => new CircuitEntry());
            var now = _timeProvider.GetUtcNow();

            entry.Failures.Enqueue(now);

            while (entry.Failures.Count > 0 && now - entry.Failures.Peek() > _window)
                entry.Failures.Dequeue();

            if (entry.Failures.Count >= _failureThreshold)
            {
                entry.OpenedAt = now;
                entry.Failures.Clear();
            }
        }
    }

    /// <summary>
    /// Saca a un proveedor de la cadena durante el resto del proceso.
    /// </summary>
    /// <remarks>
    /// Para claves inválidas y cuotas agotadas: reintentar cada turno contra una cuenta sin
    /// saldo solo gasta el presupuesto de todos los mensajes siguientes.
    /// </remarks>
    public void Disable(string provider)
    {
        lock (_gate)
        {
            var entry = _circuits.GetOrAdd(provider, _ => new CircuitEntry());
            entry.Disabled = true;
            entry.OpenedAt = _timeProvider.GetUtcNow();
        }
    }

    /// <summary>
    /// Vuelve a habilitar un proveedor y cierra su circuito.
    /// </summary>
    public void Reset(string provider)
    {
        lock (_gate)
        {
            _circuits[provider] = new CircuitEntry();
        }
    }
}
