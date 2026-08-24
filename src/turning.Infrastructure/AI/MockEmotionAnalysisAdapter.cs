using Turning.Application.Interfaces;
namespace Turning.Infrastructure.AI;
public sealed class MockEmotionAnalysisAdapter : IEmotionAnalysisPort
{
    public Task<EmotionAnalysisResult> AnalyzeAsync(EmotionAnalysisRequest req, CancellationToken ct = default)
        => Task.FromResult(new EmotionAnalysisResult { Emotion = "neutral", Score = 0.5, Provider = "mock" });
}
