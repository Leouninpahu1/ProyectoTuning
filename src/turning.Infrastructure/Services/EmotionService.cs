using Turning.Application.Features.Emotions;
using Turning.Application.Interfaces;
using Turning.Domain.Entities;
using Turning.Infrastructure.Persistence;

namespace Turning.Infrastructure.Services;

public sealed class EmotionService : IEmotionService
{
    private readonly TurningDbContext _db;
    private readonly IEmotionAnalysisPort _port;
    public EmotionService(TurningDbContext db, IEmotionAnalysisPort port) { _db = db; _port = port; }
    public async Task<(EmotionDto emotion, AvatarDto avatar)> AddAsync(Guid sessionId, EmotionCreateRequest req, CancellationToken ct)
    {
        var session = await _db.ExperimentSessions.FindAsync(new object[]{sessionId}, ct) ?? throw new InvalidOperationException("SESSION_NOT_FOUND");
        if (req.Score < 0 || req.Score > 1) throw new ArgumentException("SCORE_RANGE");
        EmotionReading reading;
        if (!string.IsNullOrWhiteSpace(req.Emotion))
            reading = EmotionReading.Create(sessionId, req.Emotion, req.Score, req.Source ?? "video", "direct");
        else
        {
            try
            {
                var res = await _port.AnalyzeAsync(new EmotionAnalysisRequest{SessionId=sessionId, Source=req.Source??"video"}, ct);
                reading = EmotionReading.Create(sessionId, res.Emotion, Math.Clamp(res.Score,0,1), req.Source??"video", res.Provider);
            }
            catch
            {
                reading = EmotionReading.Create(sessionId, "neutral", 0.3, req.Source??"video", "fallback");
                typeof(EmotionReading).GetProperty("IsDegraded")!.SetValue(reading, true);
                _db.ExperimentEvents.Add(ExperimentEvent.Create(sessionId, "DegradedOperation", $"{{\"operation\":\"EmotionAnalysis\",\"provider\":\"{_port.GetType().Name}\"}}"));
            }
        }
        _db.EmotionReadings.Add(reading);
        session.IncrementEmotionSample();
        await _db.SaveChangesAsync(ct);
        var avatar = AvatarExpression.FromReading(reading);
        _db.AvatarExpressions.Add(avatar);
        await _db.SaveChangesAsync(ct);
        return (new EmotionDto(reading.Id, reading.Emotion, reading.Score, reading.Source, reading.Provider, reading.CapturedAtUtc),
                new AvatarDto(avatar.ExpressionName, avatar.Intensity, avatar.IsFallback, avatar.ParametersJson));
    }
    public async Task<IReadOnlyList<EmotionDto>> ListAsync(Guid sessionId, CancellationToken ct)
    {
        var list = _db.EmotionReadings.Where(e=>e.SessionId==sessionId).OrderBy(e=>e.CapturedAtUtc).ToList();
        return list.Select(e=> new EmotionDto(e.Id,e.Emotion,e.Score,e.Source,e.Provider,e.CapturedAtUtc)).ToArray();
    }
}
public sealed class AvatarService : IAvatarService
{
    private readonly TurningDbContext _db;
    public AvatarService(TurningDbContext db) => _db = db;
    public Task<AvatarDto> GetCurrentAsync(Guid sessionId, CancellationToken ct)
    {
        var cur = _db.AvatarExpressions.Where(a=>a.SessionId==sessionId).OrderByDescending(a=>a.CreatedAt).FirstOrDefault();
        return Task.FromResult(cur is null ? new AvatarDto("Neutral",0.3,true,"{}") : new AvatarDto(cur.ExpressionName,cur.Intensity,cur.IsFallback,cur.ParametersJson));
    }
}
