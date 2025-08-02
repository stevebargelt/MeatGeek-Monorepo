using System;
using FluentAssertions;
using Xunit;

namespace MeatGeek.Shared.Tests
{
    public class EventGridSubscriberServiceTests
    {
        private readonly EventGridSubscriberService _service;

        public EventGridSubscriberServiceTests()
        {
            _service = new EventGridSubscriberService();
        }

        [Fact]
        public void DeconstructEventGridMessage_WithValidSubject_ShouldReturnSmokerIdAndSessionId()
        {
            // Arrange
            var eventGridEvent = new EventGridEvent
            {
                Subject = "smoker123/session456"
            };

            // Act
            var (smokerId, sessionId) = _service.DeconstructEventGridMessage(eventGridEvent);

            // Assert
            smokerId.Should().Be("smoker123");
            sessionId.Should().Be("session456");
        }

        [Fact]
        public void DeconstructEventGridMessage_WithComplexIds_ShouldReturnCorrectValues()
        {
            // Arrange
            var eventGridEvent = new EventGridEvent
            {
                Subject = "smoker-abc-123/session-def-456"
            };

            // Act
            var (smokerId, sessionId) = _service.DeconstructEventGridMessage(eventGridEvent);

            // Assert
            smokerId.Should().Be("smoker-abc-123");
            sessionId.Should().Be("session-def-456");
        }

        [Fact]
        public void DeconstructEventGridMessage_WithGuidIds_ShouldReturnCorrectValues()
        {
            // Arrange
            var smokerGuid = Guid.NewGuid().ToString();
            var sessionGuid = Guid.NewGuid().ToString();
            var eventGridEvent = new EventGridEvent
            {
                Subject = $"{smokerGuid}/{sessionGuid}"
            };

            // Act
            var (smokerId, sessionId) = _service.DeconstructEventGridMessage(eventGridEvent);

            // Assert
            smokerId.Should().Be(smokerGuid);
            sessionId.Should().Be(sessionGuid);
        }

        [Fact]
        public void DeconstructEventGridMessage_WithEmptySubject_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var eventGridEvent = new EventGridEvent
            {
                Subject = ""
            };

            // Act & Assert
            Action act = () => _service.DeconstructEventGridMessage(eventGridEvent);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Event Grid event subject is not in expected format.");
        }

        [Fact]
        public void DeconstructEventGridMessage_WithNullSubject_ShouldThrowException()
        {
            // Arrange
            var eventGridEvent = new EventGridEvent
            {
                Subject = null
            };

            // Act & Assert
            Action act = () => _service.DeconstructEventGridMessage(eventGridEvent);
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void DeconstructEventGridMessage_WithSingleComponent_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var eventGridEvent = new EventGridEvent
            {
                Subject = "smoker123"
            };

            // Act & Assert
            Action act = () => _service.DeconstructEventGridMessage(eventGridEvent);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Event Grid event subject is not in expected format.");
        }

        [Fact]
        public void DeconstructEventGridMessage_WithThreeComponents_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var eventGridEvent = new EventGridEvent
            {
                Subject = "smoker123/session456/extra"
            };

            // Act & Assert
            Action act = () => _service.DeconstructEventGridMessage(eventGridEvent);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Event Grid event subject is not in expected format.");
        }

        [Fact]
        public void DeconstructEventGridMessage_WithForwardSlashesInIds_ShouldHandleFirstTwoComponents()
        {
            // Arrange
            var eventGridEvent = new EventGridEvent
            {
                Subject = "smoker/123/session/456"
            };

            // Act & Assert
            // This should throw because there are more than 2 components
            Action act = () => _service.DeconstructEventGridMessage(eventGridEvent);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Event Grid event subject is not in expected format.");
        }

        [Fact]
        public void EventGridSubscriptionValidationHeaderKey_ShouldHaveCorrectValue()
        {
            // Assert
            EventGridSubscriberService.EventGridSubscriptionValidationHeaderKey
                .Should().Be("Aeg-Event-Type");
        }
    }
}