namespace turning.Web.ExperimentSessions;

public sealed class CreateExperimentSessionRequestDto
{
    // Sin valor se omite del JSON y el backend asigna la condición.
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? PreferredCondition { get; init; }
}

public sealed class ExperimentSessionSnapshotDto
{
    public Guid Id { get; init; }

    public required string SessionCode { get; init; }

    public required string Condition { get; init; }

    public required string Status { get; init; }

    public required string AvatarState { get; init; }

    public Guid? InterlocutorUserId { get; init; }

    public bool HasInterlocutor { get; init; }

    public bool IsAwaitingInterlocutor { get; init; }

    public int ConversationTurnCount { get; init; }

    public int EmotionSampleCount { get; init; }

    public string? LastDetectedEmotion { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public required string ConversationStage { get; init; }

    public required string EmotionStage { get; init; }

    public required string AvatarStage { get; init; }
}

public sealed class SessionListDto
{
    public List<ExperimentSessionSnapshotDto> Items { get; init; } = [];

    public int Total { get; init; }
}

public sealed class EmotionReadingDto
{
    public required string Emotion { get; init; }

    public double Score { get; init; }

    public bool IsDegraded { get; init; }

    public DateTime CapturedAtUtc { get; init; }
}

public sealed class SessionResultDto
{
    public required ExperimentSessionSnapshotDto Session { get; init; }

    public List<EmotionReadingDto> EmotionReadings { get; init; } = [];
}

public sealed class AddConversationTurnRequestDto
{
    public string Sender { get; init; } = "Participant";

    public string Message { get; init; } = string.Empty;
}

public sealed class ConversationTurnSnapshotDto
{
    public Guid Id { get; init; }

    public Guid SessionId { get; init; }

    public int SequenceNumber { get; init; }

    public required string Sender { get; init; }

    public required string Message { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}