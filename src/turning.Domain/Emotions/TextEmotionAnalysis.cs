using Turning.Domain.Entities;

namespace Turning.Domain.Emotions;

/// <summary>
/// Resultado del análisis emocional de un texto (RF-AES-01).
/// </summary>
/// <param name="EmotionId">Etiqueta ganadora, del vocabulario de <see cref="EmotionLabel"/>.</param>
/// <param name="Confidence">
/// Cuán seguro está el clasificador de haber elegido bien, 0..1. Es el margen entre la
/// etiqueta ganadora y la siguiente: no confundir con la intensidad.
/// </param>
/// <param name="Intensity">
/// Cuánta carga emocional trae el texto, 0..1. Es lo que el avatar usa como intensidad
/// de la expresión.
/// </param>
/// <param name="Polarity">Valencia del texto, de -1 (negativa) a 1 (positiva).</param>
/// <param name="Scores">Distribución normalizada por etiqueta.</param>
/// <param name="FallbackUsed">
/// El texto no tenía señal emocional reconocible y se aplicó el fallback obligatorio.
/// </param>
/// <param name="ModelVersion">Versión del léxico que produjo el resultado.</param>
public sealed record TextEmotionAnalysis(
    string EmotionId,
    double Confidence,
    double Intensity,
    double Polarity,
    IReadOnlyDictionary<string, double> Scores,
    bool FallbackUsed,
    string ModelVersion)
{
    /// <summary>
    /// Construye el resultado neutral que RF-AES-01 exige cuando no hay señal o algo falla.
    /// </summary>
    public static TextEmotionAnalysis Fallback(string modelVersion) =>
        new(
            EmotionId: EmotionLabel.Neutral,
            Confidence: 0,
            Intensity: EmotionLabel.FallbackIntensity,
            Polarity: 0,
            Scores: new Dictionary<string, double>(),
            FallbackUsed: true,
            ModelVersion: modelVersion);
}
