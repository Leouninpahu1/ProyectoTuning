using FluentAssertions;
using Turning.Application.Features.EnrichedResponse;
using Xunit;

namespace Turning.Application.Tests;

public class EnrichedResponseFactoryTests
{
    [Fact]
    public void Create_ShouldPopulateDtoFields()
    {
        var factory = new EnrichedResponseFactory();
        var sessionId = Guid.NewGuid();

        var dto = factory.Create(sessionId, "hi", "joy", 0.8, "human", "text_response");

        dto.SessionId.Should().Be(sessionId);
        dto.MessageText.Should().Be("hi");
        dto.EmotionId.Should().Be("joy");
        dto.EmotionConfidence.Should().BeApproximately(0.8, 0.0001);
        dto.Source.Should().Be("human");
        dto.MessageType.Should().Be("text_response");
        dto.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
