namespace Turning.Application.Features.Surveys;
public sealed record SurveyDefinitionDto(Guid Id, string Code, string Version, IReadOnlyList<SurveyQuestionDto> Questions);
public sealed record SurveyQuestionDto(Guid Id, string Code, string Text, string Type, bool Required, int Order);
public sealed record SurveySubmitRequest(Guid SurveyDefinitionId, Dictionary<string,string>? Answers);
public interface ISurveyService
{
    Task<SurveyDefinitionDto> GetForSessionAsync(Guid sessionId, CancellationToken ct);
    Task<Guid> SubmitAsync(Guid sessionId, SurveySubmitRequest req, CancellationToken ct);
}
