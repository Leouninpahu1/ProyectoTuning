namespace Turning.Application.Features.Auth;

/// <summary>
/// Solicitud para registrar un nuevo usuario.
/// </summary>
public sealed class RegisterRequest
{
    /// <summary>
    /// Nombre completo del usuario.
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// Correo electrónico del usuario.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Contraseña en texto plano recibida desde la API.
    /// </summary>
    public required string Password { get; init; }

    /// <summary>
    /// Rol solicitado. Es obligatorio y debe ser uno de los roles conocidos del
    /// sistema (<see cref="Turning.Domain.Common.UserRoles"/>). Que el registro
    /// público acepte roles privilegiados es un pendiente conocido.
    /// </summary>
    public required string Role { get; init; }
}