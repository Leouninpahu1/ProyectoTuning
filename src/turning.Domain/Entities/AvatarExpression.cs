using Turning.Domain.Common;
namespace Turning.Domain.Entities;
public sealed class AvatarExpression : BaseEntity
{
    private AvatarExpression(){}
    public Guid SessionId { get; private set; }
    public Guid EmotionReadingId { get; private set; }
    public string ExpressionName { get; private set; } = "Neutral";
    public double Intensity { get; private set; }
    public string ParametersJson { get; private set; } = "{}";
    public bool IsFallback { get; private set; }
    public static AvatarExpression FromReading(EmotionReading r){
        var map = r.Emotion.ToLowerInvariant() switch { "joy"=>"Joy","sadness"=>"Sadness","anger"=>"Anger","surprise"=>"Surprise", _=>"Neutral" };
        var fallback = map=="Neutral" && r.Emotion.ToLowerInvariant()!="neutral";
        return new(){ Id=Guid.NewGuid(), SessionId=r.SessionId, EmotionReadingId=r.Id, ExpressionName=map, Intensity=Math.Clamp(r.Score,0,1), IsFallback=fallback, CreatedAt=DateTime.UtcNow };
    }
}
