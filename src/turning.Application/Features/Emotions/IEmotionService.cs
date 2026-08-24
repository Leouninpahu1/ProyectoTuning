namespace Turning.Application.Features.Emotions;
public sealed record EmotionCreateRequest(string? Emotion, double Score, string? Source);
public sealed record EmotionDto(Guid Id, string Emotion, double Score, string Source, string Provider, DateTime CapturedAtUtc);
public sealed record AvatarDto(string ExpressionName, double Intensity, bool IsFallback, string ParametersJson);
public interface IEmotionService
{
    Task<(EmotionDto emotion, AvatarDto avatar)> AddAsync(Guid sessionId, EmotionCreateRequest req, CancellationToken ct);
    Task<IReadOnlyList<EmotionDto>> ListAsync(Guid sessionId, CancellationToken ct);
}
public interface IAvatarService { Task<AvatarDto> GetCurrentAsync(Guid sessionId, CancellationToken ct); }
