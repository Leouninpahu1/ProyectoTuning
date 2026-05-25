using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Turning.Application.Interfaces;
using Turning.Infrastructure.AI;
using Turning.Infrastructure.Persistence;
using Turning.Infrastructure.Repositories;
using Turning.Infrastructure.Security;

namespace Turning.Infrastructure.DependencyInjection;

/// <summary>
/// Extensiones para registrar servicios de Infrastructure en el contenedor de inyección de dependencias.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Añade los servicios de la capa Infrastructure al contenedor de servicios.
    /// </summary>
    /// <param name="services">Colección de servicios.</param>
    /// <param name="configuration">Configuración de la aplicación para registrar persistencia y seguridad.</param>
    /// <returns>La colección de servicios para permitir encadenamiento.</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=turning.db";

        services.AddDbContext<TurningDbContext>(options =>
            options.UseSqlite(connectionString));

        // Registra los repositorios
        // IMPORTANTE: En producción, reemplaza InMemorySampleRepository con una implementación real
        services.AddScoped<IConversationTurnRepository, ConversationTurnRepository>();
        services.AddScoped<ISampleRepository, InMemorySampleRepository>();
        services.AddScoped<IExperimentSessionRepository, ExperimentSessionRepository>();
        services.AddScoped<ITextGenerationPort, RuleBasedTextGenerationAdapter>();
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<ITokenService, JwtTokenService>();

        // WebSocket infrastructure for enriched-response feature
        services.AddSingleton<Turning.Infrastructure.WebSockets.WebSocketConnectionManager>();
        services.AddScoped<Turning.Application.Interfaces.IWebSocketTransport, Turning.Infrastructure.WebSockets.WebSocketTransport>();

        // Aquí se registrarían otros servicios de infraestructura:
        // - DbContext
        // - Unit of Work
        // - External API clients
        // - Email services
        // - etc.

        return services;
    }
}
