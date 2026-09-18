namespace Turning.Application.Features.ExperimentSessions;

using Turning.Domain.Entities;

/// <summary>
/// Traduce la entidad de dominio al contrato público de sesión.
///
/// Es el único sitio donde se decide qué campos de <see cref="ExperimentSession"/>
/// salen por la API: <c>OwnerUserId</c>, <c>RowVersion</c> e <c>IsDeleted</c> son
/// internos y no aparecen, y los enums salen como texto y no como número.
/// Cualquier endpoint que devuelva sesiones debe pasar por aquí en vez de
/// serializar la entidad.
/// </summary>
public static class ExperimentSessionSnapshotMapper
{
    /// <summary>
    /// Construye el contrato público de una sesión.
    /// </summary>
    public static ExperimentSessionSnapshot ToSnapshot(ExperimentSession s) => new()
    {
        Id = s.Id,
        SessionCode = s.SessionCode,
        Condition = s.Condition.ToString(),
        Status = s.Status.ToString(),
        AvatarState = s.AvatarState,
        ConversationTurnCount = s.ConversationTurnCount,
        EmotionSampleCount = s.EmotionSampleCount,
        LastDetectedEmotion = s.LastDetectedEmotion,
        CreatedAtUtc = s.CreatedAt,
        ActivatedAtUtc = s.ActivatedAtUtc,
        ExpiresAtUtc = s.ExpiresAtUtc,
        LastActivityAtUtc = s.LastActivityAtUtc,
        CompletedAtUtc = s.CompletedAtUtc,
        CancelledAtUtc = s.CancelledAtUtc,
        CancellationReason = s.CancellationReason,
        ConversationStage = s.ConversationTurnCount == 0 ? "ready-for-first-turn" : "in-progress",
        EmotionStage = s.EmotionSampleCount == 0 ? "ready-for-first-signal" : "monitoring",
        AvatarStage = s.AvatarState
    };
}
