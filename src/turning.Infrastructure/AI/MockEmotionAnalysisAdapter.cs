using Turning.Application.Interfaces;

namespace Turning.Infrastructure.AI;

/// <summary>
/// Adaptador de marcador de posicion para el analisis emocional por <b>video</b>.
/// Devuelve siempre una lectura neutral: no hay proveedor real conectado.
/// </summary>
/// <remarks>
/// Este puerto es el canal de video/audio (por ejemplo HumeAI) y esta fuera del alcance
/// actual. El analisis emocional del <b>texto</b> de la conversacion NO pasa por aqui:
/// RF-AES-01 lo define como interno y tiene su propio puerto. Este contrato recibe
/// <c>byte[] Payload</c> y no tendria donde recibir el texto.
/// </remarks>
public sealed class MockEmotionAnalysisAdapter : IEmotionAnalysisPort
{
    /// <inheritdoc />
    public Task<EmotionAnalysisResult> AnalyzeAsync(EmotionAnalysisRequest req, CancellationToken ct = default)
        => Task.FromResult(new EmotionAnalysisResult { Emotion = "neutral", Score = 0.5, Provider = "mock" });
}
