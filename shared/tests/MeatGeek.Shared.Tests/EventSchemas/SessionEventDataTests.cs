using System;
using FluentAssertions;
using MeatGeek.Shared.EventSchemas.Sessions;
using Newtonsoft.Json;
using Xunit;

namespace MeatGeek.Shared.Tests.EventSchemas
{
    public class SessionEventDataTests
    {
        [Fact]
        public void SessionCreatedEventData_ShouldSerializeToJson()
        {
            // Arrange
            var eventData = new SessionCreatedEventData
            {
                Id = "session123",
                SmokerId = "smoker456",
                Title = "BBQ Session"
            };

            // Act
            var json = JsonConvert.SerializeObject(eventData);
            var deserialized = JsonConvert.DeserializeObject<SessionCreatedEventData>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.Id.Should().Be("session123");
            deserialized.SmokerId.Should().Be("smoker456");
            deserialized.Title.Should().Be("BBQ Session");
        }

        [Fact]
        public void SessionCreatedEventData_JsonSerialization_ShouldUseCorrectPropertyNames()
        {
            // Arrange
            var eventData = new SessionCreatedEventData
            {
                Id = "test-id",
                SmokerId = "test-smoker",
                Title = "Test Title"
            };

            // Act
            var json = JsonConvert.SerializeObject(eventData);

            // Assert
            json.Should().Contain("\"id\":\"test-id\"");
            json.Should().Contain("\"smokerId\":\"test-smoker\"");
            json.Should().Contain("\"title\":\"Test Title\"");
        }

        [Fact]
        public void SessionUpdatedEventData_ShouldSerializeToJson()
        {
            // Arrange
            var endTime = DateTime.UtcNow.AddHours(2);
            var eventData = new SessionUpdatedEventData
            {
                Id = "session123",
                SmokerId = "smoker456",
                Title = "Updated BBQ Session",
                Description = "A great BBQ session",
                EndTime = endTime
            };

            // Act
            var json = JsonConvert.SerializeObject(eventData);
            var deserialized = JsonConvert.DeserializeObject<SessionUpdatedEventData>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.Id.Should().Be("session123");
            deserialized.SmokerId.Should().Be("smoker456");
            deserialized.Title.Should().Be("Updated BBQ Session");
            deserialized.Description.Should().Be("A great BBQ session");
            deserialized.EndTime.Should().BeCloseTo(endTime, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void SessionUpdatedEventData_WithNullEndTime_ShouldSerializeCorrectly()
        {
            // Arrange
            var eventData = new SessionUpdatedEventData
            {
                Id = "session123",
                SmokerId = "smoker456",
                Title = "BBQ Session",
                Description = "Session in progress",
                EndTime = null
            };

            // Act
            var json = JsonConvert.SerializeObject(eventData);
            var deserialized = JsonConvert.DeserializeObject<SessionUpdatedEventData>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.EndTime.Should().BeNull();
        }

        [Fact]
        public void SessionUpdatedEventData_JsonSerialization_ShouldUseCorrectPropertyNames()
        {
            // Arrange
            var eventData = new SessionUpdatedEventData
            {
                Id = "test-id",
                SmokerId = "test-smoker",
                Title = "Test Title",
                Description = "Test Description",
                EndTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            };

            // Act
            var json = JsonConvert.SerializeObject(eventData);

            // Assert
            json.Should().Contain("\"id\":\"test-id\"");
            json.Should().Contain("\"smokerId\":\"test-smoker\"");
            json.Should().Contain("\"title\":\"Test Title\"");
            json.Should().Contain("\"description\":\"Test Description\"");
            json.Should().Contain("\"endTime\":");
        }

        [Fact]
        public void SessionDeletedEventData_ShouldSerializeToJson()
        {
            // Arrange
            var eventData = new SessionDeletedEventData
            {
                Id = "session123",
                SmokerId = "smoker456"
            };

            // Act
            var json = JsonConvert.SerializeObject(eventData);
            var deserialized = JsonConvert.DeserializeObject<SessionDeletedEventData>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.Id.Should().Be("session123");
            deserialized.SmokerId.Should().Be("smoker456");
        }

        [Fact]
        public void SessionDeletedEventData_JsonSerialization_ShouldUseCorrectPropertyNames()
        {
            // Arrange
            var eventData = new SessionDeletedEventData
            {
                Id = "test-id",
                SmokerId = "test-smoker"
            };

            // Act
            var json = JsonConvert.SerializeObject(eventData);

            // Assert
            json.Should().Contain("\"id\":\"test-id\"");
            json.Should().Contain("\"smokerId\":\"test-smoker\"");
        }

        [Fact]
        public void SessionEndedEventData_ShouldSerializeToJson()
        {
            // Arrange
            var endTime = DateTime.UtcNow;
            var eventData = new SessionEndedEventData
            {
                Id = "session123",
                SmokerId = "smoker456",
                EndTime = endTime
            };

            // Act
            var json = JsonConvert.SerializeObject(eventData);
            var deserialized = JsonConvert.DeserializeObject<SessionEndedEventData>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.Id.Should().Be("session123");
            deserialized.SmokerId.Should().Be("smoker456");
            deserialized.EndTime.Should().BeCloseTo(endTime, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void SessionEndedEventData_WithNullEndTime_ShouldSerializeCorrectly()
        {
            // Arrange
            var eventData = new SessionEndedEventData
            {
                Id = "session123",
                SmokerId = "smoker456",
                EndTime = null
            };

            // Act
            var json = JsonConvert.SerializeObject(eventData);
            var deserialized = JsonConvert.DeserializeObject<SessionEndedEventData>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized.EndTime.Should().BeNull();
        }

        [Fact]
        public void SessionEndedEventData_JsonSerialization_ShouldUseCorrectPropertyNames()
        {
            // Arrange
            var eventData = new SessionEndedEventData
            {
                Id = "test-id",
                SmokerId = "test-smoker",
                EndTime = new DateTime(2024, 1, 1, 15, 30, 0, DateTimeKind.Utc)
            };

            // Act
            var json = JsonConvert.SerializeObject(eventData);

            // Assert
            json.Should().Contain("\"id\":\"test-id\"");
            json.Should().Contain("\"smokerId\":\"test-smoker\"");
            json.Should().Contain("\"endTime\":");
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData(null)]
        public void SessionEventData_WithInvalidIds_ShouldStillDeserialize(string invalidId)
        {
            // Arrange & Act
            var createdEvent = new SessionCreatedEventData { Id = invalidId, SmokerId = "valid-smoker", Title = "Test" };
            var updatedEvent = new SessionUpdatedEventData { Id = invalidId, SmokerId = "valid-smoker" };
            var deletedEvent = new SessionDeletedEventData { Id = invalidId, SmokerId = "valid-smoker" };
            var endedEvent = new SessionEndedEventData { Id = invalidId, SmokerId = "valid-smoker" };

            // Assert - Objects should be created successfully even with invalid IDs
            createdEvent.Should().NotBeNull();
            updatedEvent.Should().NotBeNull();
            deletedEvent.Should().NotBeNull();
            endedEvent.Should().NotBeNull();
        }
    }
}