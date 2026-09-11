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
    /// <summary>
    /// Obtiene una sesion por id. Solo el propietario o un solicitante privilegiado
    /// (Researcher/Administrator) puede verla; para el resto la sesion no existe.
    /// </summary>
    Task<ExperimentSessionSnapshot> GetByIdAsync(Guid id, Guid requestingUserId, bool isPrivilegedRequester, CancellationToken ct = default);
    Task<PagedSessionsResult> ListByParticipantAsync(Guid participantId, Guid requestingUserId, bool isPrivilegedRequester, int page, int pageSize, CancellationToken ct = default);
    /// <summary>
    /// Activa una sesion propia (o cualquiera si el solicitante es privilegiado).
    /// </summary>
    Task<ExperimentSessionSnapshot> ActivateAsync(Guid id, Guid requestingUserId, bool isPrivilegedRequester, CancellationToken ct = default);
    /// <summary>
    /// Completa una sesion propia (o cualquiera si el solicitante es privilegiado).
    /// </summary>
    Task<ExperimentSessionSnapshot> CompleteAsync(Guid id, Guid requestingUserId, bool isPrivilegedRequester, CancellationToken ct = default);
    Task<ExperimentSessionSnapshot> CancelAsync(Guid id, string reason, Guid actorId, CancellationToken ct = default);
}