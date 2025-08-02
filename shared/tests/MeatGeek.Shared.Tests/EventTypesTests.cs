using FluentAssertions;
using Xunit;

namespace MeatGeek.Shared.Tests
{
    public class EventTypesTests
    {
        [Fact]
        public void SessionCreated_ShouldHaveCorrectValue()
        {
            // Act & Assert
            EventTypes.Sessions.SessionCreated.Should().Be("SessionCreated");
        }

        [Fact]
        public void SessionDeleted_ShouldHaveCorrectValue()
        {
            // Act & Assert
            EventTypes.Sessions.SessionDeleted.Should().Be("SessionDeleted");
        }

        [Fact]
        public void SessionUpdated_ShouldHaveCorrectValue()
        {
            // Act & Assert
            EventTypes.Sessions.SessionUpdated.Should().Be("SessionUpdated");
        }

        [Fact]
        public void SessionEnded_ShouldHaveCorrectValue()
        {
            // Act & Assert
            EventTypes.Sessions.SessionEnded.Should().Be("SessionEnded");
        }

        [Fact]
        public void AllSessionEventTypes_ShouldBeUnique()
        {
            // Arrange
            var eventTypes = new[]
            {
                EventTypes.Sessions.SessionCreated,
                EventTypes.Sessions.SessionDeleted,
                EventTypes.Sessions.SessionUpdated,
                EventTypes.Sessions.SessionEnded
            };

            // Act & Assert
            eventTypes.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void AllSessionEventTypes_ShouldNotBeNullOrEmpty()
        {
            // Act & Assert
            EventTypes.Sessions.SessionCreated.Should().NotBeNullOrEmpty();
            EventTypes.Sessions.SessionDeleted.Should().NotBeNullOrEmpty();
            EventTypes.Sessions.SessionUpdated.Should().NotBeNullOrEmpty();
            EventTypes.Sessions.SessionEnded.Should().NotBeNullOrEmpty();
        }

        [Theory]
        [InlineData("SessionCreated")]
        [InlineData("SessionDeleted")]
        [InlineData("SessionUpdated")]
        [InlineData("SessionEnded")]
        public void SessionEventTypes_ShouldMatchExpectedValues(string expectedValue)
        {
            // Arrange
            var eventTypes = new[]
            {
                EventTypes.Sessions.SessionCreated,
                EventTypes.Sessions.SessionDeleted,
                EventTypes.Sessions.SessionUpdated,
                EventTypes.Sessions.SessionEnded
            };

            // Act & Assert
            eventTypes.Should().Contain(expectedValue);
        }

        [Fact]
        public void EventTypes_ShouldBeStaticClass()
        {
            // Act & Assert
            typeof(EventTypes).Should().BeStatic();
        }

        [Fact]
        public void EventTypes_Sessions_ShouldBeStaticClass()
        {
            // Act & Assert
            typeof(EventTypes.Sessions).Should().BeStatic();
        }

        [Fact]
        public void EventTypes_Sessions_ShouldBeNestedClass()
        {
            // Act & Assert
            typeof(EventTypes.Sessions).IsNested.Should().BeTrue();
            typeof(EventTypes.Sessions).DeclaringType.Should().Be(typeof(EventTypes));
        }
    }
}