namespace Turning.Application.Features.ExperimentSessions;

/// <summary>
/// Qué puede hacer un usuario sobre una sesión.
/// </summary>
/// <remarks>
/// Existe porque el acceso dejó de ser un sí o un no en cuanto la sesión pasó a tener dos
/// personas. La tentación era añadir el interlocutor a la comprobación de propietario en una
/// línea, pero esa comprobación la usan también <c>activate</c>, <c>complete</c> y
/// <c>cancel</c>: el interlocutor habría podido cancelar la sesión del participante, y leer
/// sus resultados y su encuesta.
///
/// Los valores están ordenados: comparar con <c>&gt;=</c> es la forma de exigir un mínimo.
/// </remarks>
public enum SessionAccessLevel
{
    /// <summary>
    /// Sin relación con la sesión. Indistinguible de que no exista.
    /// </summary>
    None = 0,

    /// <summary>
    /// Interlocutor asignado: puede leer la sesión y conversar en ella, nada más.
    /// </summary>
    Interlocutor = 1,

    /// <summary>
    /// Dueño de la sesión: además gobierna su ciclo de vida.
    /// </summary>
    Owner = 2,

    /// <summary>
    /// Rol privilegiado (Researcher / Administrator): acceso a sesiones ajenas.
    /// </summary>
    Privileged = 3
}
