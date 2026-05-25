using System.Text.Json;
using FluentAssertions;
using Turning.Application.DTOs;
using Xunit;

namespace Turning.Application.Tests;

public class EnrichedDtoSerializationTests
{
    [Fact]
    public void JsonSerialization_ShouldContainExpectedPropertyNames()
    {
        var dto = new EnrichedConversationMessageDto
        {
            SessionId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            MessageText = "hello",
            EmotionId = "neutral",
            EmotionConfidence = 0.5,
            Timestamp = DateTime.Parse("2026-05-25T12:00:00Z"),
            Source = "ai",
            MessageType = "text_response"
        };

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.Should().Contain("\"sessionId\"");
        json.Should().Contain("\"messageText\"");
        json.Should().Contain("\"emotionId\"");
        json.Should().Contain("\"emotionConfidence\"");
        json.Should().Contain("\"timestamp\"");
        json.Should().Contain("\"source\"");
        json.Should().Contain("\"messageType\"");
    }
}
