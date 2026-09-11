namespace Turning.API.Extensions;

/// <summary>
/// Extensiones para configuración de la API.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Orígenes permitidos cuando no hay nada configurado en <c>Cors:AllowedOrigins</c>.
    ///
    /// Incluye los del cliente Blazor interno y los puertos habituales de los
    /// servidores de desarrollo de frontend (Vite, React, Angular), en HTTP y HTTPS.
    /// Sin ellos, un front en <c>http://localhost:5173</c> recibe un preflight 204
    /// sin cabecera <c>Access-Control-Allow-Origin</c>: el navegador lo rechaza y en
    /// consola aparece un error de CORS genérico, sin rastro del lado del servidor.
    /// </summary>
    private static readonly string[] OrigenesPorDefecto =
    [
        // Cliente Blazor del propio proyecto
        "https://localhost:7028",
        "http://localhost:5251",
        "https://localhost:7001",

        // Servidores de desarrollo de frontend
        "http://localhost:5173",  "https://localhost:5173",  // Vite
        "http://localhost:3000",  "https://localhost:3000",  // React / Next
        "http://localhost:4200",  "https://localhost:4200",  // Angular
        "http://127.0.0.1:5173",  "http://127.0.0.1:3000",   // mismo host por IP
    ];

    /// <summary>
    /// Añade configuraciones de CORS a la aplicación.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">
    /// Configuración de la que se leen los orígenes permitidos, en
    /// <c>Cors:AllowedOrigins</c>. Si trae valores, reemplazan por completo a los
    /// de por defecto; por entorno se pasa como
    /// <c>Cors__AllowedOrigins__0</c>, <c>Cors__AllowedOrigins__1</c>, …
    /// </param>
    /// <returns>La colección de servicios para encadenamiento.</returns>
    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var origenes = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        if (origenes is null || origenes.Length == 0)
        {
            origenes = OrigenesPorDefecto;
        }

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });

            options.AddPolicy("AllowSpecific", builder =>
            {
                builder
                    .WithOrigins(origenes)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Devuelve los orígenes que quedaron activos, para dejarlos en el log de
    /// arranque: cuando un front no conecta, lo primero que hay que poder mirar es
    /// si su origen está en esta lista.
    /// </summary>
    public static IReadOnlyList<string> GetConfiguredCorsOrigins(IConfiguration configuration)
    {
        var origenes = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        return origenes is null || origenes.Length == 0 ? OrigenesPorDefecto : origenes;
    }

    /// <summary>
    /// Configura la compresión de respuestas.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <returns>La colección de servicios para encadenamiento.</returns>
    public static IServiceCollection AddCompressionConfiguration(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        return services;
    }
}
