using Turning.Domain.Common;
namespace Turning.Domain.Entities;
public sealed class EmotionReading : BaseEntity
{
    private EmotionReading(){}
    public Guid SessionId { get; private set; }
    public Guid? ConversationTurnId { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public string Emotion { get; private set; } = string.Empty;
    public double Score { get; private set; }
    public DateTime CapturedAtUtc { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public bool IsDegraded { get; private set; }
    public static EmotionReading Create(Guid sid, string emo, double score, string source="simulated", string provider="mock") {
        if(score<0||score>1) throw new ArgumentException("Score 0-1");
        return new(){ Id=Guid.NewGuid(), SessionId=sid, Emotion=emo, Score=score, Source=source, Provider=provider, CapturedAtUtc=DateTime.UtcNow, CreatedAt=DateTime.UtcNow };
    }
}
