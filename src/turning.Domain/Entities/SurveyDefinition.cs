using Turning.Domain.Common;
namespace Turning.Domain.Entities;
public sealed class SurveyDefinition : BaseEntity
{
    private SurveyDefinition(){}
    public string Code { get; private set; } = string.Empty;
    public string Version { get; private set; } = "1.0";
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public List<SurveyQuestion> Questions { get; private set; } = [];
    public static SurveyDefinition Create(string code, string name) => new(){ Id=Guid.NewGuid(), Code=code, Name=name, IsActive=true, CreatedAt=DateTime.UtcNow };
}
public sealed class SurveyQuestion : BaseEntity
{
    private SurveyQuestion(){}
    public Guid SurveyDefinitionId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;
    public string Type { get; private set; } = "text";
    public bool Required { get; private set; }
    public int Order { get; private set; }
}
public sealed class SurveyResponse : BaseEntity
{
    private SurveyResponse(){}
    public Guid SessionId { get; private set; }
    public Guid SurveyDefinitionId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public List<SurveyAnswer> Answers { get; private set; } = [];
}
public sealed class SurveyAnswer : BaseEntity
{
    private SurveyAnswer(){}
    public Guid SurveyResponseId { get; private set; }
    public Guid SurveyQuestionId { get; private set; }
    public string Value { get; private set; } = string.Empty;
}
