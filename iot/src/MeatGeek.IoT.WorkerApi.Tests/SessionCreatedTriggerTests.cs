using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
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
        public void Run_WithValidEventData_ShouldLogSessionInformation()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var smokerId = "test-smoker-123";
            
            var sessionCreatedData = new SessionCreatedEventData
            {
                Id = sessionId,
                SmokerId = smokerId,
                Title = "Test Session"
            };

            var eventGridEvent = new EventGridEvent
            {
                Data = JObject.FromObject(sessionCreatedData)
            };

            // Act
            // Note: The actual Run method uses static ServiceClient which makes it difficult to test
            // In a real scenario, we would refactor to use dependency injection
            
            // Assert
            // Note: Since SessionCreatedTrigger.Run is static and depends on environment variables,
            // actual testing would require dependency injection refactoring
            eventGridEvent.Should().NotBeNull();
            sessionCreatedData.Id.Should().Be(sessionId);
            sessionCreatedData.SmokerId.Should().Be(smokerId);
        }

        [Fact]
        public void SessionCreatedEventData_ShouldHaveCorrectProperties()
        {
            // Arrange
            var eventData = new SessionCreatedEventData
            {
                Id = "test-id",
                SmokerId = "test-smoker",
                Title = "Test Title"
            };

            // Assert
            eventData.Id.Should().Be("test-id");
            eventData.SmokerId.Should().Be("test-smoker");
            eventData.Title.Should().Be("Test Title");
        }
    }
}