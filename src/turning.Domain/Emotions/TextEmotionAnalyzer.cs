using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Turning.Domain.Entities;

namespace Turning.Domain.Emotions;

/// <summary>
/// Analiza la carga emocional de un texto a partir de un léxico con pesos (RF-AES-01).
/// </summary>
/// <remarks>
/// Es determinista y no hace entrada/salida: por eso vive en el dominio y se puede probar
/// sin dobles. RF-AES-01 lo define como análisis <b>interno</b>, no como una llamada a un
/// servicio externo; el canal de video es otro puerto distinto.
///
/// La estrategia es la que pide el requisito: léxico con pesos, intensificadores,
/// negación y tipo de oración. No pretende ser un clasificador entrenado: es un baseline
/// explicable, que es lo que un experimento necesita para poder justificar sus datos.
/// </remarks>
public sealed partial class TextEmotionAnalyzer
{
    private readonly IEmotionLexicon _lexicon;

    /// <summary>
    /// Ventana de tokens sobre la que actúa una negación o un intensificador.
    /// </summary>
    private const int ModifierWindow = 3;

    /// <summary>
    /// Factor que aplica una exclamación a la intensidad final.
    /// </summary>
    private const double ExclamationBoost = 1.25;

    /// <summary>
    /// Empuje hacia <c>confusion</c> cuando el texto es una pregunta.
    /// </summary>
    private const double QuestionConfusionWeight = 0.35;

    /// <summary>
    /// Constructor del analizador.
    /// </summary>
    public TextEmotionAnalyzer(IEmotionLexicon lexicon) => _lexicon = lexicon;

    /// <summary>
    /// Analiza un texto y devuelve su emoción dominante.
    /// </summary>
    public TextEmotionAnalysis Analyze(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return TextEmotionAnalysis.Fallback(_lexicon.Version);

        var isQuestion = text.Contains('?', StringComparison.Ordinal) || text.Contains('¿', StringComparison.Ordinal);
        var isExclamation = text.Contains('!', StringComparison.Ordinal) || text.Contains('¡', StringComparison.Ordinal);

        var normalized = Normalize(text);
        var scores = new Dictionary<string, double>(StringComparer.Ordinal);

        // Las expresiones de varias palabras se consumen primero: si "no entiendo" se
        // tratara token a token, la negación invertiría "entiendo" y el sentido se perdería.
        foreach (var phrase in _lexicon.MultiWordTerms)
        {
            var hit = _lexicon.Lookup(phrase);
            if (hit is null) continue;

            var occurrences = CountOccurrences(normalized, phrase);
            if (occurrences == 0) continue;

            Accumulate(scores, hit.Value.Emotion, hit.Value.Weight * occurrences);
            normalized = normalized.Replace(phrase, " ", StringComparison.Ordinal);
        }

        var tokens = TokenPattern().Split(normalized).Where(t => t.Length > 0).ToArray();

        for (var i = 0; i < tokens.Length; i++)
        {
            var hit = _lexicon.Lookup(tokens[i]);
            if (hit is null) continue;

            var weight = hit.Value.Weight;
            var emotion = hit.Value.Emotion;
            var negated = false;

            // Un modificador afecta a lo que viene después, así que se mira hacia atrás.
            for (var back = 1; back <= ModifierWindow && i - back >= 0; back++)
            {
                var previous = tokens[i - back];

                if (_lexicon.IsNegator(previous))
                {
                    negated = true;
                    break;
                }

                var modifier = _lexicon.Modifier(previous);
                if (modifier is not null)
                    weight *= modifier.Value;
            }

            if (negated)
            {
                // Negar una emoción no afirma la contraria ("no estoy feliz" no es tristeza):
                // se descuenta la señal en vez de invertirla hacia otra etiqueta.
                Accumulate(scores, emotion, -weight * 0.75);
                continue;
            }

            Accumulate(scores, emotion, weight);
        }

        if (isQuestion)
            Accumulate(scores, EmotionLabel.Confusion, QuestionConfusionWeight);

        // Las aportaciones negativas (por negación) no deben convertirse en señal positiva.
        var positive = scores
            .Where(pair => pair.Value > 0)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

        if (positive.Count == 0)
            return TextEmotionAnalysis.Fallback(_lexicon.Version);

        var total = positive.Values.Sum();
        var distribution = positive.ToDictionary(
            pair => pair.Key,
            pair => Math.Round(pair.Value / total, 4),
            StringComparer.Ordinal);

        var ordered = positive.OrderByDescending(pair => pair.Value).ToArray();
        var winner = ordered[0];
        var runnerUp = ordered.Length > 1 ? ordered[1].Value : 0d;

        // Confianza = cuánto destaca la ganadora sobre la siguiente.
        var confidence = Math.Clamp((winner.Value - runnerUp) / winner.Value, 0, 1);

        var intensity = winner.Value;
        if (isExclamation)
            intensity *= ExclamationBoost;

        intensity = Math.Clamp(intensity, 0, 1);

        var polarity = Math.Clamp(
            distribution.Sum(pair => pair.Value * _lexicon.Polarity(pair.Key)),
            -1,
            1);

        return new TextEmotionAnalysis(
            EmotionId: EmotionLabel.Normalize(winner.Key),
            Confidence: Math.Round(confidence, 4),
            Intensity: Math.Round(intensity, 4),
            Polarity: Math.Round(polarity, 4),
            Scores: distribution,
            FallbackUsed: false,
            ModelVersion: _lexicon.Version);
    }

    private static void Accumulate(IDictionary<string, double> scores, string emotion, double weight) =>
        scores[emotion] = scores.TryGetValue(emotion, out var current) ? current + weight : weight;

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = haystack.IndexOf(needle, StringComparison.Ordinal);

        while (index >= 0)
        {
            count++;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }

    /// <summary>
    /// Pasa el texto a minúsculas y le quita las tildes, para que "está" y "esta" no sean
    /// términos distintos frente al léxico.
    /// </summary>
    private static string Normalize(string text)
    {
        var lowered = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(lowered.Length);

        foreach (var character in lowered)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            builder.Append(character == 'ñ' ? 'n' : character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex TokenPattern();
}
