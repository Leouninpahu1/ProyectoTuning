namespace Turning.Domain.Emotions;

/// <summary>
/// Diccionario emocional que alimenta al analizador de texto.
/// </summary>
/// <remarks>
/// El dominio define qué necesita saber; de dónde salen las palabras (un JSON embebido, una
/// tabla, un servicio) es cosa de infraestructura. Esa separación es lo que permite que
/// RF-AES-01 pueda pasar de <c>source: text_analysis</c> a <c>source: database</c> sin
/// tocar el algoritmo.
/// </remarks>
public interface IEmotionLexicon
{
    /// <summary>
    /// Versión del léxico, para poder reproducir un análisis pasado.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Devuelve la carga emocional de un término, o <c>null</c> si no está en el léxico.
    /// </summary>
    /// <returns>Etiqueta emocional y peso 0..1.</returns>
    (string Emotion, double Weight)? Lookup(string term);

    /// <summary>
    /// Factor multiplicador de un intensificador o atenuador ("muy", "poco"),
    /// o <c>null</c> si el término no lo es.
    /// </summary>
    double? Modifier(string term);

    /// <summary>
    /// Indica si el término invierte la carga emocional que lo sigue ("no", "nunca").
    /// </summary>
    bool IsNegator(string term);

    /// <summary>
    /// Polaridad de una etiqueta, de -1 (negativa) a 1 (positiva).
    /// </summary>
    double Polarity(string emotion);

    /// <summary>
    /// Expresiones de más de una palabra presentes en el léxico ("no entiendo"), en número
    /// de palabras descendente, para poder detectarlas antes que sus partes sueltas.
    /// </summary>
    IReadOnlyList<string> MultiWordTerms { get; }
}
