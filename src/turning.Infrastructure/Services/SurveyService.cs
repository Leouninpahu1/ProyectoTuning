using Microsoft.EntityFrameworkCore;
using Turning.Application.Features.Surveys;
using Turning.Domain.Entities;
using Turning.Infrastructure.Persistence;

namespace Turning.Infrastructure.Services;

public sealed class SurveyAppService : ISurveyService
{
    private readonly TurningDbContext _db;
    public SurveyAppService(TurningDbContext db) => _db = db;
    public async Task<SurveyDefinitionDto> GetForSessionAsync(Guid sessionId, CancellationToken ct)
    {
        var s = await _db.ExperimentSessions.FindAsync(new object[]{sessionId}, ct) ?? throw new InvalidOperationException("SESSION_NOT_FOUND");
        if (s.Status is ExperimentSessionStatus.Created or ExperimentSessionStatus.Active) throw new InvalidOperationException("SURVEY_NOT_AVAILABLE");
        var def = await _db.SurveyDefinitions.Include(d=>d.Questions).FirstOrDefaultAsync(d=>d.IsActive, ct);
        if (def is null) { def = SurveyDefinition.Create("default-v1","Encuesta Default"); _db.SurveyDefinitions.Add(def); await _db.SaveChangesAsync(ct); def = await _db.SurveyDefinitions.Include(d=>d.Questions).FirstAsync(d=>d.Id==def.Id, ct); }
        return new SurveyDefinitionDto(def.Id, def.Code, def.Version, def.Questions.OrderBy(q=>q.Order).Select(q=> new SurveyQuestionDto(q.Id,q.Code,q.Text,q.Type,q.Required,q.Order)).ToArray());
    }
    public async Task<Guid> SubmitAsync(Guid sessionId, SurveySubmitRequest req, CancellationToken ct)
    {
        var s = await _db.ExperimentSessions.FindAsync(new object[]{sessionId}, ct) ?? throw new InvalidOperationException("SESSION_NOT_FOUND");
        var def = await _db.SurveyDefinitions.FirstOrDefaultAsync(d=>d.Id==req.SurveyDefinitionId, ct) ?? throw new InvalidOperationException("SURVEY_NOT_FOUND");
        var existing = await _db.SurveyResponses.FirstOrDefaultAsync(r=>r.SessionId==sessionId && r.SurveyDefinitionId==def.Id, ct);
        if (existing != null && existing.SubmittedAtUtc.HasValue) throw new InvalidOperationException("ALREADY_SUBMITTED");
        if (existing is null)
        {
            var resp = (SurveyResponse)Activator.CreateInstance(typeof(SurveyResponse), true)!;
            typeof(SurveyResponse).GetProperty("Id")!.SetValue(resp, Guid.NewGuid());
            typeof(SurveyResponse).GetProperty("SessionId")!.SetValue(resp, sessionId);
            typeof(SurveyResponse).GetProperty("SurveyDefinitionId")!.SetValue(resp, def.Id);
            typeof(SurveyResponse).GetProperty("OwnerUserId")!.SetValue(resp, s.OwnerUserId);
            typeof(SurveyResponse).GetProperty("StartedAtUtc")!.SetValue(resp, DateTime.UtcNow);
            _db.SurveyResponses.Add(resp); await _db.SaveChangesAsync(ct);
            return (Guid)typeof(SurveyResponse).GetProperty("Id")!.GetValue(resp)!;
        }
        typeof(SurveyResponse).GetProperty("SubmittedAtUtc")!.SetValue(existing, DateTime.UtcNow);
        await _db.SaveChangesAsync(ct);
        return existing.Id;
    }
}
