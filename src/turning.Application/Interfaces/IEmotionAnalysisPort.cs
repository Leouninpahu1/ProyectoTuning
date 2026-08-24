using Turning.Domain.Entities;
namespace Turning.Application.Interfaces;
public sealed class EmotionAnalysisRequest { public Guid SessionId { get; set; } public string Source { get; set; } = "video"; public byte[]? Payload { get; set; } }
public sealed class EmotionAnalysisResult { public string Emotion { get; set; } = "neutral"; public double Score { get; set; } = 0.5; public string Provider { get; set; } = "mock"; }
public interface IEmotionAnalysisPort { Task<EmotionAnalysisResult> AnalyzeAsync(EmotionAnalysisRequest req, CancellationToken ct = default); }
