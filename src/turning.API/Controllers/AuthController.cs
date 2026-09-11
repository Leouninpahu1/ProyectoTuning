using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turning.Application.Features.Auth;

namespace Turning.API.Controllers;

/// <summary>
/// Endpoints de autenticación y sesión de usuario.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Constructor del controller de autenticación.
    /// </summary>
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registra un nuevo usuario persistido en base de datos.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResult>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Inicia sesión con credenciales persistidas.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResult>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Devuelve el usuario autenticado actual.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthenticatedUser), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticatedUser>> Me(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        return Ok(user);
    }
}