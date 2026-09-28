using System.Reflection;
using System.Text.Json;
using Turning.Domain.Emotions;

namespace Turning.Infrastructure.AI.Lexicon;

/// <summary>
/// Léxico emocional cargado desde un JSON embebido en el ensamblado.
/// </summary>
/// <remarks>
/// Es la pieza de entrada/salida del análisis de texto, y por eso vive aquí y no en el
/// dominio: cambiar el origen de las palabras a una tabla de base de datos (el
/// <c>source: database</c> que contempla RF-AES-01) no debería tocar el algoritmo.
///
/// Se registra como singleton: el archivo se lee y se indexa una vez.
/// </remarks>
public sealed class EmbeddedJsonEmotionLexicon : IEmotionLexicon
{
    private const string ResourceFileName = "emotion-lexicon-es.json";

    private readonly Dictionary<string, (string Emotion, double Weight)> _terms;
    private readonly Dictionary<string, double> _modifiers;
    private readonly HashSet<string> _negators;
    private readonly Dictionary<string, double> _polarity;

    /// <inheritdoc />
    public string Version { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> MultiWordTerms { get; }

    /// <summary>
    /// Carga el léxico embebido.
    /// </summary>
    public EmbeddedJsonEmotionLexicon()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Se busca por sufijo en vez de por nombre completo: el nombre del recurso depende
        // del namespace raiz del ensamblado, y un renombrado del proyecto lo cambiaria sin
        // que nada avisara hasta la primera peticion en produccion.
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(ResourceFileName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"No se encontró el recurso '{ResourceFileName}' entre los {assembly.GetManifestResourceNames().Length} " +
                "recursos del ensamblado. Debe estar declarado como EmbeddedResource en turning.Infrastructure.csproj. " +
                "Ojo: un nombre de archivo con un punto y un codigo de idioma (por ejemplo '.es.json') hace que MSBuild " +
                "lo trate como recurso satelite de cultura y lo saque del ensamblado principal.");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"No se pudo abrir el recurso '{resourceName}'.");

        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;

        Version = root.GetProperty("version").GetString() ?? "lexicon-unknown";

        _terms = new Dictionary<string, (string, double)>(StringComparer.Ordinal);
        foreach (var emotion in root.GetProperty("emotions").EnumerateObject())
        {
            foreach (var term in emotion.Value.EnumerateObject())
            {
                // Si un término aparece en dos emociones gana el de mayor peso: la
                // alternativa (que gane el último leído) dependería del orden del archivo.
                var weight = term.Value.GetDouble();
                if (_terms.TryGetValue(term.Name, out var existing) && existing.Weight >= weight)
                    continue;

                _terms[term.Name] = (emotion.Name, weight);
            }
        }

        _modifiers = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var section in new[] { "intensifiers", "diminishers" })
        {
            if (!root.TryGetProperty(section, out var element)) continue;

            foreach (var modifier in element.EnumerateObject())
                _modifiers[modifier.Name] = modifier.Value.GetDouble();
        }

        _negators = root.TryGetProperty("negators", out var negators)
            ? negators.EnumerateArray().Select(n => n.GetString() ?? string.Empty).ToHashSet(StringComparer.Ordinal)
            : [];

        _polarity = new Dictionary<string, double>(StringComparer.Ordinal);
        if (root.TryGetProperty("polarity", out var polarity))
        {
            foreach (var entry in polarity.EnumerateObject())
                _polarity[entry.Name] = entry.Value.GetDouble();
        }

        MultiWordTerms = _terms.Keys
            .Where(term => term.Contains(' ', StringComparison.Ordinal))
            .OrderByDescending(term => term.Count(c => c == ' '))
            .ToArray();
    }

    /// <inheritdoc />
    public (string Emotion, double Weight)? Lookup(string term) =>
        _terms.TryGetValue(term, out var hit) ? hit : null;

    /// <inheritdoc />
    public double? Modifier(string term) =>
        _modifiers.TryGetValue(term, out var factor) ? factor : null;

    /// <inheritdoc />
    public bool IsNegator(string term) => _negators.Contains(term);

    /// <inheritdoc />
    public double Polarity(string emotion) =>
        _polarity.TryGetValue(emotion, out var value) ? value : 0;
}
