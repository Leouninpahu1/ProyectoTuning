using Turning.Domain.Common;
namespace Turning.Domain.Entities;
public sealed class ConditionAssignment : BaseEntity
{
    private ConditionAssignment() {}
    public Guid SessionId { get; private set; }
    public ExperimentalCondition Condition { get; private set; }
    public string Strategy { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public static ConditionAssignment Create(Guid sessionId, ExperimentalCondition c, string strategy, string reason) => new(){ Id=Guid.NewGuid(), SessionId=sessionId, Condition=c, Strategy=strategy, Reason=reason, CreatedAt=DateTime.UtcNow };
}
