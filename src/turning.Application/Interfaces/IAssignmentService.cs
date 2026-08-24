using Turning.Domain.Entities;
namespace Turning.Application.Interfaces;
public interface IAssignmentService
{
    Task<ConditionAssignment> AssignAsync(Guid sessionId, ExperimentalCondition? preferred, CancellationToken ct = default);
}
