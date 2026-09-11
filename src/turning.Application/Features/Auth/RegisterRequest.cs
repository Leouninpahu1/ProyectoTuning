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
    /// Rol solicitado. Es obligatorio y desde el registro público solo se
    /// acepta <see cref="Turning.Domain.Common.UserRoles.Participant"/>:
    /// los roles privilegiados se asignan por seed o por administración.
    /// </summary>
    public required string Role { get; init; }
}