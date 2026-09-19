using Turning.Application.Interfaces;

namespace Turning.Infrastructure.AI.Providers;

/// <summary>
/// Clasificación de un fallo de proveedor. Determina si vale la pena reintentar o si hay
/// que pasar al siguiente eslabón de la cadena.
/// </summary>
public enum ProviderFailureKind
{
    /// <summary>Sin fallo.</summary>
    None = 0,

    /// <summary>Fallo pasajero: merece reintento.</summary>
    Transient,

    /// <summary>Límite de peticiones por tiempo: se puede esperar y reintentar.</summary>
    RateLimited,

    /// <summary>Sin saldo o cuota agotada: reintentar no arregla nada.</summary>
    Quota,

    /// <summary>Clave inválida o ausente: reintentar no arregla nada.</summary>
    Credentials,

    /// <summary>Se agotó el tiempo de la petición.</summary>
    Timeout,

    /// <summary>La petición fue rechazada por políticas de contenido.</summary>
    ContentPolicy,

    /// <summary>Error definitivo del que no se vuelve.</summary>
    Permanent
}

/// <summary>
/// Respuesta de un proveedor concreto.
/// </summary>
/// <param name="Success">La generación produjo texto.</param>
/// <param name="Text">Texto generado.</param>
/// <param name="Model">Modelo que atendió la petición.</param>
/// <param name="Failure">Clasificación del fallo, si lo hubo.</param>
/// <param name="Detail">Detalle legible del fallo, para registro.</param>
/// <param name="LatencyMs">Latencia medida.</param>
/// <param name="Attempts">Intentos consumidos.</param>
public sealed record ProviderReply(
    bool Success,
    string? Text,
    string? Model,
    ProviderFailureKind Failure,
    string? Detail,
    long LatencyMs,
    int Attempts)
{
    /// <summary>
    /// Construye una respuesta correcta.
    /// </summary>
    public static ProviderReply Ok(string text, string? model, long latencyMs, int attempts) =>
        new(true, text, model, ProviderFailureKind.None, null, latencyMs, attempts);

    /// <summary>
    /// Construye una respuesta fallida.
    /// </summary>
    public static ProviderReply Failed(ProviderFailureKind kind, string? detail, long latencyMs, int attempts) =>
        new(false, null, null, kind, detail, latencyMs, attempts);

    /// <summary>
    /// Indica si el fallo desaconseja volver a usar este proveedor durante el proceso.
    /// </summary>
    public bool DisablesProvider => Failure is ProviderFailureKind.Credentials or ProviderFailureKind.Quota;
}

/// <summary>
/// Un eslabón de la cadena de generación de texto.
/// </summary>
/// <remarks>
/// Es una abstracción interna de infraestructura: la capa de aplicación solo conoce
/// <see cref="ITextGenerationPort"/>, y de que haya uno o cinco proveedores detrás no se
/// entera.
/// </remarks>
public interface ITextGenerationProvider
{
    /// <summary>
    /// Nombre del proveedor, tal como aparece en el contrato y en los eventos.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Indica si el proveedor tiene configuración suficiente para intentarlo. Uno sin clave
    /// no es un error: simplemente se salta.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Intenta generar una respuesta. No lanza por fallos del proveedor: los devuelve
    /// clasificados para que el router decida.
    /// </summary>
    Task<ProviderReply> GenerateAsync(TextGenerationRequest request, CancellationToken cancellationToken);
}
