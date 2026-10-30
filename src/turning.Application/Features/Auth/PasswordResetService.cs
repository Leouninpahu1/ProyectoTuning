using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Turning.Application.Interfaces;
using Turning.Domain.Entities;
using TurningApplicationException = Turning.Application.Exceptions.ApplicationException;

namespace Turning.Application.Features.Auth;

/// <summary>
/// Restablecimiento de contraseña con un enlace enviado por correo.
/// </summary>
/// <remarks>
/// El token no se guarda en ningún lado: firma con <c>Jwt:SigningKey</c> el usuario, el
/// vencimiento y una huella del hash actual de la contraseña. En cuanto la contraseña cambia,
/// la huella cambia, así que el token sirve una sola vez sin tabla ni migración.
/// </remarks>
public sealed class PasswordResetService : IPasswordResetService
{
    private const string InvalidTokenCode = "AUTH_RESET_TOKEN_INVALID";
    private const string Subject = "SUE: restablece tu contraseña";
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);

    // shortcut: el enfriamiento vive en la memoria del proceso y sirve con una sola instancia de la
    // API, igual que SessionOperationLock; si la API escala a varias réplicas, moverlo a la base o a
    // un caché compartido.
    private static readonly ConcurrentDictionary<string, DateTimeOffset> LastSentByEmail = new(StringComparer.Ordinal);

    private readonly IUserAccountRepository _users;
    private readonly IPasswordHasherService _hasher;
    private readonly IEmailSender _email;
    private readonly IOptions<JwtOptions> _jwt;
    private readonly IOptions<PasswordResetOptions> _options;
    private readonly TimeProvider _time;
    private readonly ILogger<PasswordResetService> _logger;

    /// <summary>
    /// Constructor del servicio de restablecimiento.
    /// </summary>
    public PasswordResetService(
        IUserAccountRepository users,
        IPasswordHasherService hasher,
        IEmailSender email,
        IOptions<JwtOptions> jwt,
        IOptions<PasswordResetOptions> options,
        TimeProvider time,
        ILogger<PasswordResetService> logger)
    {
        _users = users;
        _hasher = hasher;
        _email = email;
        _jwt = jwt;
        _options = options;
        _time = time;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RequestAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || !MailAddress.TryCreate(email.Trim(), out _))
            throw new TurningApplicationException("Escribe un correo válido.", "AUTH_INVALID_REQUEST");

        var user = await _users.GetByEmailAsync(email.Trim(), cancellationToken);
        if (user is null)
            return;

        var options = _options.Value;
        if (options.Validate().Count > 0)
        {
            _logger.LogWarning("No se envió el correo de restablecimiento: falta configurar la sección PasswordReset.");
            return;
        }

        var now = _time.GetUtcNow();
        if (!TryReserve(user.NormalizedEmail, now))
            return;

        var token = CreateToken(user, now.AddMinutes(options.TokenMinutes));
        var link = $"{options.BaseUrl!.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";

        try
        {
            await _email.SendAsync(user.Email, Subject, Body(user.FullName, link, options.TokenMinutes), cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Nunca se registra el enlace: quien lea los logs podría cambiar la contraseña ajena.
            _logger.LogWarning("No se pudo enviar el correo de restablecimiento ({Error}: {Detalle}).", ex.GetType().Name, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task ResetAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < AuthService.MinimumPasswordLength)
            throw new TurningApplicationException(
                $"La contraseña debe tener al menos {AuthService.MinimumPasswordLength} caracteres.", "AUTH_INVALID_REQUEST");

        var user = await FindUserForTokenAsync(token, cancellationToken)
            ?? throw new TurningApplicationException("El enlace de restablecimiento no es válido o ya venció. Pide uno nuevo.", InvalidTokenCode);

        user.UpdatePasswordHash(_hasher.HashPassword(user, newPassword));
        await _users.SaveChangesAsync(cancellationToken);
    }

    private static bool TryReserve(string normalizedEmail, DateTimeOffset now)
    {
        while (true)
        {
            if (!LastSentByEmail.TryGetValue(normalizedEmail, out var last))
            {
                if (LastSentByEmail.TryAdd(normalizedEmail, now)) return true;
                continue;
            }

            if (now - last < Cooldown) return false;
            if (LastSentByEmail.TryUpdate(normalizedEmail, now, last)) return true;
        }
    }

    private string CreateToken(UserAccount user, DateTimeOffset expiresAt)
    {
        var payload = $"{user.Id:N}|{expiresAt.ToUnixTimeSeconds()}";
        return Base64Url.EncodeToString(Encoding.UTF8.GetBytes($"{payload}|{Convert.ToHexString(Sign(user, payload))}"));
    }

    // Todo token no válido termina en null, para que la respuesta sea la misma en todos los casos.
    private async Task<UserAccount?> FindUserForTokenAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || !Base64Url.IsValid(token))
            return null;

        var parts = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(token)).Split('|');
        if (parts.Length != 3
            || !Guid.TryParseExact(parts[0], "N", out var userId)
            || !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var expiresAtUnix)
            || _time.GetUtcNow().ToUnixTimeSeconds() >= expiresAtUnix)
            return null;

        byte[] signature;
        try { signature = Convert.FromHexString(parts[2]); }
        catch (FormatException) { return null; }

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return null;

        return CryptographicOperations.FixedTimeEquals(signature, Sign(user, $"{parts[0]}|{parts[1]}")) ? user : null;
    }

    private byte[] Sign(UserAccount user, string payload)
    {
        var key = _jwt.Value.SigningKey ?? throw new InvalidOperationException("Falta Jwt:SigningKey para firmar el enlace de restablecimiento.");
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(user.PasswordHash)));
        return HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes($"pwreset|{payload}|{fingerprint}"));
    }

    private static string Body(string fullName, string link, int minutes) => $"""
        Hola, {fullName}:

        Recibimos una solicitud para restablecer la contraseña de tu cuenta de SUE.
        Abre este enlace para elegir una contraseña nueva. Vence en {minutes} minutos y solo sirve una vez:

        {link}

        Si no pediste el cambio, ignora este correo: tu contraseña actual sigue funcionando.
        """;
}
