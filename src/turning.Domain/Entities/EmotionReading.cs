using Turning.Domain.Common;

namespace Turning.Domain.Entities;

/// <summary>
/// Lectura emocional asociada a una sesión. Puede provenir del análisis del texto
/// de un turno, de una captura de video/audio, o entrar directamente desde el cliente.
/// </summary>
public sealed class EmotionReading : BaseEntity
{
    private EmotionReading() { }

    /// <summary>
    /// Sesión a la que pertenece la lectura.
    /// </summary>
    public Guid SessionId { get; private set; }

    /// <summary>
    /// Turno del que se derivó la lectura, cuando proviene del análisis de texto.
    /// </summary>
    public Guid? ConversationTurnId { get; private set; }

    /// <summary>
    /// Origen de la lectura: <c>text_analysis</c>, <c>video</c>, <c>direct</c>, <c>fallback</c>…
    /// </summary>
    public string Source { get; private set; } = string.Empty;

    /// <summary>
    /// Etiqueta emocional. Ver <see cref="EmotionLabel"/> para el vocabulario conocido.
    /// </summary>
    public string Emotion { get; private set; } = string.Empty;

    /// <summary>
    /// Intensidad de la emoción, 0..1. Es el valor que el avatar usa como intensidad
    /// de la expresión; no confundir con <see cref="Confidence"/>.
    /// </summary>
    public double Score { get; private set; }

    /// <summary>
    /// Cuán seguro está el clasificador de la etiqueta elegida, 0..1.
    /// </summary>
    /// <remarks>
    /// Es una magnitud distinta de <see cref="Score"/>: un texto puede traer una emoción
    /// intensa y ambigua a la vez ("no sé si reír o llorar"), o una emoción leve e
    /// inequívoca. Antes solo existía <c>Score</c> y las dos cosas se confundían.
    /// </remarks>
    public double Confidence { get; private set; }

    /// <summary>
    /// Valencia del texto, de -1 (negativa) a 1 (positiva). Nulo si no se calculó.
    /// </summary>
    public double? Polarity { get; private set; }

    /// <summary>
    /// Versión del modelo o léxico que produjo la lectura, para poder reproducirla.
    /// </summary>
    public string? ModelVersion { get; private set; }

    /// <summary>
    /// Distribución de puntajes por etiqueta, serializada en JSON.
    /// </summary>
    /// <remarks>
    /// Va denormalizado a propósito: hoy nadie consulta por etiqueta individual, así que una
    /// tabla hija añadiría un join a cambio de nada. Si algún día se filtra por etiqueta,
    /// esto se normaliza.
    /// </remarks>
    public string? ScoresJson { get; private set; }

    /// <summary>
    /// Tiempo que tardó el análisis, en milisegundos. Es la evidencia del presupuesto
    /// de RF-AES-01.
    /// </summary>
    public int? AnalysisLatencyMs { get; private set; }

    /// <summary>
    /// Momento de captura.
    /// </summary>
    public DateTime CapturedAtUtc { get; private set; }

    /// <summary>
    /// Proveedor que produjo la lectura.
    /// </summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>
    /// Indica que la lectura no se obtuvo de un análisis real, sino de un fallback.
    /// </summary>
    /// <remarks>
    /// Es también el <c>fallbackUsed</c> de RF-AES-01: no se añade un segundo booleano con
    /// el mismo significado.
    /// </remarks>
    public bool IsDegraded { get; private set; }

    /// <summary>
    /// Crea una lectura emocional genérica.
    /// </summary>
    public static EmotionReading Create(Guid sid, string emo, double score, string source = "simulated", string provider = "mock")
    {
        if (score < 0 || score > 1) throw new ArgumentException("Score 0-1");
        return new()
        {
            Id = Guid.NewGuid(),
            SessionId = sid,
            Emotion = emo,
            Score = score,
            Source = source,
            Provider = provider,
            CapturedAtUtc = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Crea una lectura derivada del análisis del texto de un turno (RF-AES-01).
    /// </summary>
    /// <remarks>
    /// Es el único camino que rellena <see cref="ConversationTurnId"/>, la columna que
    /// enlaza el texto con su lectura. Existe en la tabla y en un índice desde la migración
    /// inicial, pero hasta ahora ningún código la escribía.
    ///
    /// La etiqueta se valida contra <see cref="EmotionLabel"/> solo por esta vía: el canal
    /// directo y el de video aceptan texto libre y tienen filas persistidas.
    /// </remarks>
    public static EmotionReading CreateFromText(
        Guid sessionId,
        Guid conversationTurnId,
        string emotionId,
        double intensity,
        double confidence,
        double polarity,
        string modelVersion,
        string? scoresJson,
        int analysisLatencyMs,
        bool fallbackUsed)
    {
        if (sessionId == Guid.Empty)
            throw new ArgumentException("La sesión es obligatoria.", nameof(sessionId));

        if (conversationTurnId == Guid.Empty)
            throw new ArgumentException("El turno de origen es obligatorio.", nameof(conversationTurnId));

        if (intensity < 0 || intensity > 1)
            throw new ArgumentOutOfRangeException(nameof(intensity), "La intensidad debe estar entre 0 y 1.");

        if (confidence < 0 || confidence > 1)
            throw new ArgumentOutOfRangeException(nameof(confidence), "La confianza debe estar entre 0 y 1.");

        if (polarity < -1 || polarity > 1)
            throw new ArgumentOutOfRangeException(nameof(polarity), "La polaridad debe estar entre -1 y 1.");

        var reading = new EmotionReading
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            ConversationTurnId = conversationTurnId,
            Emotion = EmotionLabel.Normalize(emotionId),
            Score = intensity,
            Confidence = confidence,
            Polarity = polarity,
            ModelVersion = modelVersion,
            ScoresJson = scoresJson,
            AnalysisLatencyMs = analysisLatencyMs,
            Source = fallbackUsed ? "fallback" : "text_analysis",
            Provider = modelVersion,
            CapturedAtUtc = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        if (fallbackUsed)
            reading.MarkDegraded();

        return reading;
    }

    /// <summary>
    /// Marca la lectura como degradada: no vino de un análisis real.
    /// </summary>
    /// <remarks>
    /// Existe para que la capa de servicios no tenga que mutar la propiedad por
    /// reflexión, que es como se hacía antes.
    /// </remarks>
    public void MarkDegraded()
    {
        IsDegraded = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
