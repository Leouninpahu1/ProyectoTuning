using Turning.Application.Exceptions;
using Turning.Application.Interfaces;
using Turning.Domain.Common;
using Turning.Domain.Entities;
using TurningApplicationException = Turning.Application.Exceptions.ApplicationException;

namespace Turning.Application.Features.Auth;

/// <summary>
/// Implementa los casos de uso de registro e inicio de sesión.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly ITokenService _tokenService;

    /// <summary>
    /// Constructor del servicio de autenticación.
    /// </summary>
    public AuthService(
        IUserAccountRepository userAccountRepository,
        IPasswordHasherService passwordHasherService,
        ITokenService tokenService)
    {
        _userAccountRepository = userAccountRepository;
        _passwordHasherService = passwordHasherService;
        _tokenService = tokenService;
    }

    /// <inheritdoc />
    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRegisterRequest(request);

        var exists = await _userAccountRepository.ExistsByEmailAsync(request.Email, cancellationToken);
        if (exists)
        {
            throw new TurningApplicationException("Ya existe un usuario registrado con ese correo.", "AUTH_USER_ALREADY_EXISTS");
        }

        var provisionalUser = UserAccount.Create(request.Email, request.FullName, "pending", request.Role);
        var passwordHash = _passwordHasherService.HashPassword(provisionalUser, request.Password);
        provisionalUser.UpdatePasswordHash(passwordHash);
        provisionalUser.RecordSuccessfulLogin();

        await _userAccountRepository.AddAsync(provisionalUser, cancellationToken);
        await _userAccountRepository.SaveChangesAsync(cancellationToken);

        return _tokenService.CreateToken(provisionalUser);
    }

    /// <inheritdoc />
    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ValidateLoginRequest(request);

        var userAccount = await _userAccountRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (userAccount is null || !_passwordHasherService.VerifyPassword(userAccount, request.Password))
        {
            throw new TurningApplicationException("Las credenciales proporcionadas no son válidas.", "AUTH_INVALID_CREDENTIALS");
        }

        userAccount.RecordSuccessfulLogin();
        await _userAccountRepository.SaveChangesAsync(cancellationToken);

        return _tokenService.CreateToken(userAccount);
    }

    /// <inheritdoc />
    public async Task<AuthenticatedUser> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userAccount = await _userAccountRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("UserAccount", userId.ToString());

        return new AuthenticatedUser
        {
            Id = userAccount.Id,
            FullName = userAccount.FullName,
            Email = userAccount.Email,
            Role = userAccount.Role
        };
    }

    private static void ValidateRegisterRequest(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new TurningApplicationException("El nombre completo es obligatorio.", "AUTH_INVALID_REQUEST");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new TurningApplicationException("El correo es obligatorio.", "AUTH_INVALID_REQUEST");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new TurningApplicationException("La contraseña debe tener al menos 8 caracteres.", "AUTH_INVALID_REQUEST");

        if (string.IsNullOrWhiteSpace(request.Role))
            throw new TurningApplicationException(
                $"El rol es obligatorio. Roles válidos: {string.Join(", ", UserRoles.All)}.",
                "AUTH_ROLE_REQUIRED");

        // El registro público acepta por ahora cualquier rol conocido, incluidos
        // los privilegiados. Restringirlo a Participant está pendiente de acordar
        // con el equipo: hoy no existe otra vía de crear Researchers para pruebas.
        if (!UserRoles.IsKnown(request.Role))
            throw new TurningApplicationException(
                $"El rol '{request.Role}' no es un rol reconocido. Roles válidos: {string.Join(", ", UserRoles.All)}.",
                "AUTH_ROLE_UNKNOWN");
    }

    private static void ValidateLoginRequest(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new TurningApplicationException("Correo y contraseña son obligatorios.", "AUTH_INVALID_REQUEST");
    }
}