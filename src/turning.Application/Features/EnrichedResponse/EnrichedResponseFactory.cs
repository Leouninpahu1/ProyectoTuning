using Turning.Application.DTOs;

namespace Turning.Application.Features.EnrichedResponse;

public sealed class EnrichedResponseFactory : IEnrichedResponseFactory
{
    public EnrichedConversationMessageDto Create(Guid sessionId, string messageText, string emotionId, double emotionConfidence, string source, string messageType)
    {
        return new EnrichedConversationMessageDto
        {
            SessionId = sessionId,
            MessageText = messageText,
            EmotionId = emotionId,
            EmotionConfidence = emotionConfidence,
            Timestamp = DateTime.UtcNow,
            Source = source,
            MessageType = messageType
        };
    }
}
