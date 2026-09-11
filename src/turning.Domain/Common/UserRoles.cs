namespace Turning.Domain.Common;

/// <summary>
/// Roles conocidos del sistema y reglas sobre cuáles puede pedir un usuario
/// al registrarse por su cuenta.
/// </summary>
public static class UserRoles
{
    /// <summary>
    /// Participante del experimento. Solo accede a sus propias sesiones.
    /// </summary>
    public const string Participant = "Participant";

    /// <summary>
    /// Investigador. Accede a sesiones ajenas para análisis.
    /// </summary>
    public const string Researcher = "Researcher";

    /// <summary>
    /// Administrador. Además puede cancelar sesiones de cualquier participante.
    /// </summary>
    public const string Administrator = "Administrator";

    /// <summary>
    /// Todos los roles que el sistema reconoce.
    /// </summary>
    public static readonly IReadOnlyList<string> All =
    [
        Participant,
        Researcher,
        Administrator
    ];

    /// <summary>
    /// Roles que un usuario puede solicitar en el registro público.
    /// Researcher y Administrator quedan fuera a propósito: conceden acceso a
    /// sesiones ajenas, así que se asignan por seed o por administración
    /// autenticada, nunca por autoservicio.
    /// </summary>
    public static readonly IReadOnlyList<string> SelfAssignable =
    [
        Participant
    ];

    /// <summary>
    /// Indica si el rol es uno de los reconocidos por el sistema.
    /// </summary>
    public static bool IsKnown(string? role) =>
        role is not null && All.Contains(role.Trim(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Indica si el rol puede solicitarse desde el registro público.
    /// </summary>
    public static bool IsSelfAssignable(string? role) =>
        role is not null && SelfAssignable.Contains(role.Trim(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Devuelve el rol con la grafía canónica del sistema.
    /// </summary>
    public static string Normalize(string role) =>
        All.FirstOrDefault(known => known.Equals(role.Trim(), StringComparison.OrdinalIgnoreCase))
        ?? role.Trim();
}
