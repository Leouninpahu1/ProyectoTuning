using Turning.Application.DTOs;

namespace Turning.Application.Features.EnrichedResponse;

public interface IEnrichedResponseFactory
{
    EnrichedConversationMessageDto Create(Guid sessionId, string messageText, string emotionId, double emotionConfidence, string source, string messageType);
}
