namespace Turning.Application.Features.Auth;

/// <summary>
/// Configuración de emisión y validación de tokens JWT.
/// La firma (<see cref="SigningKey"/>) es un secreto: no debe vivir en
/// appsettings*.json, sino en variables de entorno (Jwt__SigningKey) o en
/// user-secrets durante el desarrollo.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Nombre de la sección de configuración.</summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Longitud mínima de la clave de firma. HMAC-SHA256 exige 256 bits, así que
    /// una clave más corta hace fallar la emisión del token en tiempo de ejecución.
    /// </summary>
    public const int MinimumSigningKeyLength = 32;

    /// <summary>Emisor esperado del token. No es secreto.</summary>
    public string Issuer { get; set; } = "Turning.API";

    /// <summary>Audiencia esperada del token. No es secreto.</summary>
    public string Audience { get; set; } = "Turning.Web";

    /// <summary>Clave simétrica de firma. Secreto: solo por entorno o user-secrets.</summary>
    public string? SigningKey { get; set; }

    /// <summary>Vigencia del token en minutos.</summary>
    public int ExpirationMinutes { get; set; } = 120;

    /// <summary>
    /// Claves que estuvieron versionadas en el repositorio y por lo tanto son públicas.
    /// Se rechazan aunque lleguen por entorno: cualquiera con acceso al historial de
    /// git puede firmar tokens válidos con ellas.
    /// </summary>
    public static readonly IReadOnlyList<string> RevokedSigningKeys =
    [
        "please-change-this-development-key-with-at-least-32-chars",
        "development-signing-key-change-before-production-2026"
    ];

    /// <summary>
    /// Indica si la clave dada estuvo publicada en el repositorio.
    /// </summary>
    public static bool IsRevokedSigningKey(string? signingKey) =>
        signingKey is not null && RevokedSigningKeys.Contains(signingKey.Trim());

    /// <summary>
    /// Valida la configuración y devuelve los problemas encontrados.
    /// Lista vacía significa configuración utilizable.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(SigningKey))
            errors.Add("Falta 'Jwt:SigningKey'. Es un secreto y debe venir por entorno (Jwt__SigningKey) o user-secrets, nunca de appsettings.json.");
        else if (SigningKey.Trim().Length < MinimumSigningKeyLength)
            errors.Add($"'Jwt:SigningKey' debe tener al menos {MinimumSigningKeyLength} caracteres (HMAC-SHA256 exige 256 bits); recibidos: {SigningKey.Trim().Length}.");
        else if (IsRevokedSigningKey(SigningKey))
            errors.Add("'Jwt:SigningKey' es una de las claves que estuvieron versionadas en el repositorio y quedaron revocadas. Genera una nueva.");

        if (string.IsNullOrWhiteSpace(Issuer))
            errors.Add("Falta 'Jwt:Issuer'.");

        if (string.IsNullOrWhiteSpace(Audience))
            errors.Add("Falta 'Jwt:Audience'.");

        if (ExpirationMinutes <= 0)
            errors.Add($"'Jwt:ExpirationMinutes' debe ser mayor que cero; recibido: {ExpirationMinutes}.");

        return errors;
    }
}
