using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Turning.Domain.Emotions;

namespace Turning.Application.Features.Emotions;

/// <summary>
/// Aplica la política de RF-AES-01 alrededor del analizador del dominio: mide la latencia,
/// garantiza el fallback y nunca propaga excepciones.
/// </summary>
public sealed class TextEmotionAnalysisService : ITextEmotionAnalysisService
{
    /// <summary>
    /// Presupuesto de RF-AES-01: el análisis no puede notarse en la conversación.
    /// Superarlo no invalida el resultado, pero sí deja constancia.
    /// </summary>
    public const int LatencyBudgetMs = 100;

    private readonly TextEmotionAnalyzer _analyzer;
    private readonly ILogger<TextEmotionAnalysisService> _logger;

    /// <summary>
    /// Constructor del servicio de análisis emocional de texto.
    /// </summary>
    public TextEmotionAnalysisService(TextEmotionAnalyzer analyzer, ILogger<TextEmotionAnalysisService> logger)
    {
        _analyzer = analyzer;
        _logger = logger;
    }

    /// <inheritdoc />
    public TextEmotionDto Analyze(string? text)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var analysis = _analyzer.Analyze(text);
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > LatencyBudgetMs)
            {
                _logger.LogWarning(
                    "El análisis emocional tardó {ElapsedMs} ms, por encima del presupuesto de {BudgetMs} ms",
                    stopwatch.ElapsedMilliseconds, LatencyBudgetMs);
            }

            return Map(analysis, (int)stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            // RF-AES-01 exige fallback a neutral: la conversación no se detiene porque no
            // hayamos sabido leer una emoción, pero el fallo queda registrado.
            stopwatch.Stop();
            _logger.LogError(ex, "Falló el análisis emocional del texto; se aplica el resultado neutral");

            return Map(TextEmotionAnalysis.Fallback("unavailable"), (int)stopwatch.ElapsedMilliseconds);
        }
    }

    private static TextEmotionDto Map(TextEmotionAnalysis analysis, int latencyMs) =>
        new(
            EmotionId: analysis.EmotionId,
            Confidence: analysis.Confidence,
            Intensity: analysis.Intensity,
            Polarity: analysis.Polarity,
            Source: analysis.FallbackUsed ? TextEmotionDto.SourceFallback : TextEmotionDto.SourceTextAnalysis,
            FallbackUsed: analysis.FallbackUsed,
            ModelVersion: analysis.ModelVersion,
            LatencyMs: latencyMs,
            Scores: analysis.Scores);
}
