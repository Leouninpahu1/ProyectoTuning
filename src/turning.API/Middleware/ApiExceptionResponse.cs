namespace Turning.API.Middleware;

/// <summary>
/// Cuerpo que devuelve <see cref="ExceptionHandlingMiddleware"/> cuando una
/// excepción llega sin que el controller la haya traducido.
///
/// Ojo al integrar: no tiene la misma forma que <c>ApiErrorResponse</c>, que es
/// lo que devuelven los endpoints de sesión al capturar el error ellos mismos.
/// Un cliente que maneje errores de toda la API tiene que contemplar las dos
/// formas mientras no se unifiquen.
/// </summary>
public sealed class ApiExceptionResponse
{
    /// <summary>
    /// Mensaje legible del error.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Nombre del tipo de excepción que lo originó, por ejemplo
    /// <c>NotFoundException</c>.
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Detalle del error. Hoy coincide con <see cref="Message"/>.
    /// </summary>
    public string? Details { get; init; }
}
