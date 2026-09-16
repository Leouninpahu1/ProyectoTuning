using Turning.Application.Features.AI;
using Turning.Application.Interfaces;
using Turning.Infrastructure.AI;
using Turning.Infrastructure.AI.Providers;

namespace Turning.API.Extensions;

/// <summary>
/// Configura la cadena de proveedores de generación de texto, tomando las claves de API
/// desde el entorno o user-secrets, nunca desde appsettings*.json.
/// </summary>
public static class AiConfigurationExtensions
{
    private const string HowToSetTheKeys = """
        Configura las claves fuera del repositorio, por ejemplo:

          # desarrollo (recomendado, queda fuera del arbol de trabajo)
          dotnet user-secrets --project src/turning.API set "Ai:OpenAi:ApiKey" "sk-..."
          dotnet user-secrets --project src/turning.API set "Ai:OpenRouter:ApiKey" "sk-or-..."

          # o por variable de entorno (PowerShell)
          $env:Ai__OpenAi__ApiKey = "sk-..."
          $env:Ai__OpenRouter__ApiKey = "sk-or-..."

        Sin ninguna clave la API arranca igual: la cadena cae al adaptador por reglas y las
        respuestas salen marcadas como degradadas.
        """;

    /// <summary>
    /// Registra el router de generación de texto y sus proveedores.
    /// </summary>
    /// <remarks>
    /// A diferencia de la clave de firma del JWT, la falta de claves de IA <b>no</b> impide
    /// arrancar en ningún entorno: la cadena está diseñada para degradarse hasta el
    /// adaptador por reglas. Lo que sí rompe el arranque fuera de Development es una sección
    /// mal formada, porque eso es un error de configuración y no una carencia prevista.
    /// </remarks>
    public static IServiceCollection AddAiTextGeneration(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger? logger = null)
    {
        var options = new AiOptions();
        configuration.GetSection(AiOptions.SectionName).Bind(options);

        var errors = options.Validate();
        if (errors.Count > 0)
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    $"Configuracion de IA invalida en el entorno '{environment.EnvironmentName}':{Environment.NewLine}" +
                    string.Join(Environment.NewLine, errors.Select(e => $"  - {e}")) +
                    Environment.NewLine + Environment.NewLine + HowToSetTheKeys);
            }

            logger?.LogWarning(
                "Configuracion de IA invalida; se usan los valores por defecto. Problemas: {Errors}",
                string.Join(" | ", errors));

            options = new AiOptions();
        }

        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));

        services.AddSingleton(new ProviderCircuitBreaker(
            options.CircuitFailureThreshold,
            options.CircuitWindowSeconds,
            options.CircuitBreakSeconds));

        RegisterHttpProvider(services, options, options.OpenAi, "OpenAi", logger);
        RegisterHttpProvider(services, options, options.OpenRouter, "OpenRouter", logger);

        // Eslabon final: siempre presente, nunca falla.
        services.AddScoped<ITextGenerationProvider, RuleBasedTextGenerationProvider>();

        // El router sustituye al adaptador por reglas como unica implementacion del puerto.
        services.AddScoped<ITextGenerationPort, TextGenerationRouter>();

        LogChain(logger, options);

        return services;
    }

    private static void RegisterHttpProvider(
        IServiceCollection services,
        AiOptions root,
        AiProviderOptions provider,
        string configurationKey,
        ILogger? logger)
    {
        if (!provider.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(provider.ApiKey))
        {
            // No es un error: ese eslabon simplemente no esta disponible.
            // La ruta de configuracion usa el nombre de la propiedad ("OpenAi"), no el del
            // proveedor ("openai"): sugerir la equivocada manda a configurar una clave que
            // el binder nunca lee.
            logger?.LogWarning(
                "El proveedor de IA '{Provider}' no tiene clave configurada y queda fuera de la cadena. "
                + "Se configura con '{SectionName}:{ConfigurationKey}:ApiKey'.",
                provider.Name, AiOptions.SectionName, configurationKey);
            return;
        }

        if (!Uri.TryCreate(provider.BaseUrl, UriKind.Absolute, out var baseUri))
            return;

        var clientName = $"ai-{provider.Name}";

        services.AddHttpClient(clientName, client =>
        {
            client.BaseAddress = EnsureTrailingSlash(baseUri);

            // El timeout fino lo lleva el propio proveedor por intento; este es solo un
            // techo de seguridad para que ningun socket quede colgado indefinidamente.
            client.Timeout = TimeSpan.FromMilliseconds(root.TotalBudgetMs * 2);
        });

        services.AddScoped<ITextGenerationProvider>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var providerLogger = sp.GetRequiredService<ILoggerFactory>()
                .CreateLogger($"Turning.Infrastructure.AI.Providers.{provider.Name}");

            return new OpenAiCompatibleTextGenerationProvider(
                factory.CreateClient(clientName),
                provider,
                root.SystemPrompt,
                root.MaxRetriesPerProvider,
                providerLogger);
        });
    }

    private static Uri EnsureTrailingSlash(Uri uri) =>
        uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + "/");

    private static void LogChain(ILogger? logger, AiOptions options)
    {
        var configured = new List<string>();

        if (options.OpenAi.IsUsable) configured.Add(options.OpenAi.Name);
        if (options.OpenRouter.IsUsable) configured.Add(options.OpenRouter.Name);
        configured.Add(AiOptions.RuleBasedProviderName);

        logger?.LogInformation(
            "Cadena de generacion de texto: {Chain} (presupuesto total {BudgetMs} ms)",
            string.Join(" -> ", configured), options.TotalBudgetMs);
    }
}
