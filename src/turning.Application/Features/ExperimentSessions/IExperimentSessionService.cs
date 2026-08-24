namespace Turning.Application.Features.ExperimentSessions;

/// <summary>
/// Casos de uso de bootstrap y consulta de sesiones experimentales.
/// </summary>
public interface IExperimentSessionService
{
    /// <summary>
    /// Crea una sesión experimental inicial para el usuario autenticado.
    /// </summary>
    Task<ExperimentSessionSnapshot> CreateBootstrapSessionAsync(Guid ownerUserId, CreateExperimentSessionRequest? request = null, CancellationToken cancellationToken = default);

    Task<ExperimentSessionSnapshot?> GetLatestSessionAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<ExperimentSessionSnapshot> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedSessionsResult> ListByParticipantAsync(Guid participantId, int page, int pageSize, CancellationToken ct = default);
    Task<ExperimentSessionSnapshot> ActivateAsync(Guid id, CancellationToken ct = default);
    Task<ExperimentSessionSnapshot> CompleteAsync(Guid id, CancellationToken ct = default);
    Task<ExperimentSessionSnapshot> CancelAsync(Guid id, string reason, Guid actorId, CancellationToken ct = default);
}