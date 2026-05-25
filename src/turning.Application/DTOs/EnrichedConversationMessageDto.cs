using System.Text.Json.Serialization;

namespace Turning.Application.DTOs;

public sealed class EnrichedConversationMessageDto
{
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; set; }

    [JsonPropertyName("messageText")]
    public required string MessageText { get; set; }

    [JsonPropertyName("emotionId")]
    public required string EmotionId { get; set; }

    [JsonPropertyName("emotionConfidence")]
    public double EmotionConfidence { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("source")]
    public required string Source { get; set; }

    [JsonPropertyName("messageType")]
    public required string MessageType { get; set; }
}
