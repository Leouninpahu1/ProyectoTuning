using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Turning.Application.DTOs;

/// <summary>
/// Data Transfer Object that represents an enriched conversation message
/// sent to frontend clients over realtime transport.
/// This DTO is immutable and annotated for JSON serialization and basic validation.
/// </summary>
public sealed record EnrichedMessageDto
{
    /// <summary>
    /// Experiment session identifier.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; init; }

    /// <summary>
    /// Plain text message content.
    /// </summary>
    [JsonPropertyName("messageText")]
    [Required]
    public string MessageText { get; init; } = string.Empty;

    /// <summary>
    /// Emotion identifier (e.g. "joy", "sadness", "anger", "neutral").
    /// </summary>
    [JsonPropertyName("emotionId")]
    [Required]
    [MaxLength(64)]
    public string EmotionId { get; init; } = string.Empty;

    /// <summary>
    /// Confidence score for the detected emotion in the range [0.0, 1.0].
    /// </summary>
    [JsonPropertyName("emotionConfidence")]
    [Range(0.0, 1.0)]
    public double EmotionConfidence { get; init; }

    /// <summary>
    /// Event timestamp in UTC (ISO-8601 when serialized).
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Source of the message: "human" or "ai".
    /// </summary>
    [JsonPropertyName("source")]
    [Required]
    [MaxLength(32)]
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// Message type, e.g. "text_response".
    /// </summary>
    [JsonPropertyName("messageType")]
    [Required]
    [MaxLength(64)]
    public string MessageType { get; init; } = string.Empty;
}
