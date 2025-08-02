using System;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace MeatGeek.Shared.Tests
{
    public class EventGridEventTests
    {
        [Fact]
        public void EventGridEvent_ShouldInheritFromGenericEventGridEvent()
        {
            // Act & Assert
            typeof(EventGridEvent).BaseType.Should().Be(typeof(EventGridEvent<object>));
        }

        [Fact]
        public void EventGridEvent_ShouldBeInstantiable()
        {
            // Act
            var eventGridEvent = new EventGridEvent();

            // Assert
            eventGridEvent.Should().NotBeNull();
        }

        [Fact]
        public void EventGridEvent_Generic_ShouldHaveAllRequiredProperties()
        {
            // Arrange
            var eventTime = DateTime.UtcNow;
            var testData = new { message = "test", id = 123 };

            // Act
            var eventGridEvent = new EventGridEvent<object>
            {
                Topic = "test-topic",
                Id = "event-123",
                EventType = "TestEvent",
                Subject = "test/subject",
                EventTime = eventTime,
                Data = testData
            };

            // Assert
            eventGridEvent.Topic.Should().Be("test-topic");
            eventGridEvent.Id.Should().Be("event-123");
            eventGridEvent.EventType.Should().Be("TestEvent");
            eventGridEvent.Subject.Should().Be("test/subject");
            eventGridEvent.EventTime.Should().Be(eventTime);
            eventGridEvent.Data.Should().Be(testData);
        }

        [Fact]
        public void EventGridEvent_Generic_WithStringData_ShouldWork()
        {
            // Act
            var eventGridEvent = new EventGridEvent<string>
            {
                Topic = "string-topic",
                Id = "string-event-123",
                EventType = "StringEvent",
                Subject = "string/subject",
                EventTime = DateTime.UtcNow,
                Data = "This is string data"
            };

            // Assert
            eventGridEvent.Data.Should().Be("This is string data");
            eventGridEvent.Data.Should().BeOfType<string>();
        }

        [Fact]
        public void EventGridEvent_Generic_WithComplexTypeData_ShouldWork()
        {
            // Arrange
            var complexData = new
            {
                Id = "session-123",
                Title = "BBQ Session",
                StartTime = DateTime.UtcNow,
                IsActive = true,
                Temperature = 225.5
            };

            // Act
            var eventGridEvent = new EventGridEvent<object>
            {
                Topic = "session-topic",
                Id = "complex-event-123",
                EventType = "SessionCreated",
                Subject = "smoker123/session456",
                EventTime = DateTime.UtcNow,
                Data = complexData
            };

            // Assert
            eventGridEvent.Data.Should().BeEquivalentTo(complexData);
        }

        [Fact]
        public void EventGridEvent_ShouldSerializeToJson()
        {
            // Arrange
            var eventTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var eventGridEvent = new EventGridEvent
            {
                Topic = "test-topic",
                Id = "event-123",
                EventType = "TestEvent",
                Subject = "test/subject",
                EventTime = eventTime,
                Data = new { message = "test data" }
            };

            // Act
            var json = JsonConvert.SerializeObject(eventGridEvent);
            var deserialized = JsonConvert.DeserializeObject<EventGridEvent>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.Topic.Should().Be("test-topic");
            deserialized.Id.Should().Be("event-123");
            deserialized.EventType.Should().Be("TestEvent");
            deserialized.Subject.Should().Be("test/subject");
            deserialized.EventTime.Should().Be(eventTime);
        }

        [Fact]
        public void EventGridEvent_WithNullValues_ShouldHandleGracefully()
        {
            // Act
            var eventGridEvent = new EventGridEvent
            {
                Topic = null,
                Id = null,
                EventType = null,
                Subject = null,
                EventTime = default,
                Data = null
            };

            // Assert
            eventGridEvent.Should().NotBeNull();
            eventGridEvent.Topic.Should().BeNull();
            eventGridEvent.Id.Should().BeNull();
            eventGridEvent.EventType.Should().BeNull();
            eventGridEvent.Subject.Should().BeNull();
            eventGridEvent.Data.Should().BeNull();
        }

        [Fact]
        public void EventGridEvent_Properties_ShouldBeSettableAndGettable()
        {
            // Arrange
            var eventGridEvent = new EventGridEvent<string>();
            var eventTime = DateTime.UtcNow;

            // Act
            eventGridEvent.Topic = "settable-topic";
            eventGridEvent.Id = "settable-id";
            eventGridEvent.EventType = "SettableEvent";
            eventGridEvent.Subject = "settable/subject";
            eventGridEvent.EventTime = eventTime;
            eventGridEvent.Data = "settable data";

            // Assert
            eventGridEvent.Topic.Should().Be("settable-topic");
            eventGridEvent.Id.Should().Be("settable-id");
            eventGridEvent.EventType.Should().Be("SettableEvent");
            eventGridEvent.Subject.Should().Be("settable/subject");
            eventGridEvent.EventTime.Should().Be(eventTime);
            eventGridEvent.Data.Should().Be("settable data");
        }

        [Theory]
        [InlineData("topic1", "id1", "EventType1", "subject1")]
        [InlineData("", "", "", "")]
        [InlineData("very-long-topic-name-with-dashes", "guid-like-id-12345", "SessionCreated", "smoker123/session456")]
        public void EventGridEvent_WithVariousStringValues_ShouldWork(string topic, string id, string eventType, string subject)
        {
            // Act
            var eventGridEvent = new EventGridEvent
            {
                Topic = topic,
                Id = id,
                EventType = eventType,
                Subject = subject,
                EventTime = DateTime.UtcNow,
                Data = new { test = "data" }
            };

            // Assert
            eventGridEvent.Topic.Should().Be(topic);
            eventGridEvent.Id.Should().Be(id);
            eventGridEvent.EventType.Should().Be(eventType);
            eventGridEvent.Subject.Should().Be(subject);
        }
    }
}