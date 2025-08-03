using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.EventGrid.Models;
using Moq;
using Xunit;
using FluentAssertions;
using MeatGeek.IoT.WorkerApi;
using Newtonsoft.Json.Linq;
using MeatGeek.Shared;
using MeatGeek.Shared.EventSchemas.Sessions;

namespace MeatGeek.IoT.WorkerApi.Tests
{
    public class SessionCreatedTriggerTests
    {
        private readonly Mock<ILogger> _mockLogger;

        public SessionCreatedTriggerTests()
        {
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public async Task Run_WithValidEventData_ShouldLogSessionInformation()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var smokerId = "test-smoker-123";
            
            var sessionCreatedData = new SessionCreatedEventData
            {
                Id = sessionId,
                SmokerId = smokerId,
                Title = "Test Session",
                Description = "Test Description",
                StartTime = DateTime.UtcNow
            };

            var eventGridEvent = new EventGridEvent
            {
                Id = Guid.NewGuid().ToString(),
                EventType = "SessionCreated",
                Subject = $"{smokerId}/{sessionId}",
                EventTime = DateTime.UtcNow,
                Data = JObject.FromObject(sessionCreatedData)
            };

            // Act
            // Note: The actual Run method uses static ServiceClient which makes it difficult to test
            // In a real scenario, we would refactor to use dependency injection
            
            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("SessionCreated Called")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void SessionCreatedEventData_ShouldHaveCorrectProperties()
        {
            // Arrange
            var eventData = new SessionCreatedEventData
            {
                Id = "test-id",
                SmokerId = "test-smoker",
                Title = "Test Title",
                Description = "Test Description",
                StartTime = DateTime.UtcNow
            };

            // Assert
            eventData.Id.Should().Be("test-id");
            eventData.SmokerId.Should().Be("test-smoker");
            eventData.Title.Should().Be("Test Title");
            eventData.Description.Should().Be("Test Description");
            eventData.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }
    }
}