using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MeatGeek.Shared.Tests
{
    public class EventGridPublisherServiceTests
    {
        private readonly Mock<ILogger<EventGridPublisherService>> _mockLogger;
        private readonly EventGridPublisherService _service;

        public EventGridPublisherServiceTests()
        {
            _mockLogger = new Mock<ILogger<EventGridPublisherService>>();
            _service = new EventGridPublisherService(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithValidLogger_ShouldCreateInstance()
        {
            // Act & Assert
            _service.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldStillCreateInstance()
        {
            // Act & Assert
            var serviceWithNullLogger = new EventGridPublisherService(null);
            serviceWithNullLogger.Should().NotBeNull();
        }

        [Fact]
        public async Task PostEventGridEventAsync_WithMissingEnvironmentVariables_ShouldThrowException()
        {
            // Arrange
            // Ensure environment variables are not set
            Environment.SetEnvironmentVariable("EventGridTopicEndpoint", null);
            Environment.SetEnvironmentVariable("EventGridTopicKey", null);

            var eventType = "TestEvent";
            var subject = "test/subject";
            var payload = new { message = "test" };

            // Act & Assert
            Func<Task> act = async () => await _service.PostEventGridEventAsync(eventType, subject, payload);
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task PostEventGridEventAsync_WithInvalidTopicEndpoint_ShouldThrowException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("EventGridTopicEndpoint", "invalid-url");
            Environment.SetEnvironmentVariable("EventGridTopicKey", "test-key");

            var eventType = "TestEvent";
            var subject = "test/subject";
            var payload = new { message = "test" };

            // Act & Assert
            Func<Task> act = async () => await _service.PostEventGridEventAsync(eventType, subject, payload);
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public void PostEventGridEventAsync_ShouldLogStartingMessage()
        {
            // Arrange
            Environment.SetEnvironmentVariable("EventGridTopicEndpoint", "https://test.eventgrid.azure.net/api/events");
            Environment.SetEnvironmentVariable("EventGridTopicKey", "test-key");

            var eventType = "TestEvent";
            var subject = "test/subject";
            var payload = new { message = "test" };

            // Act
            try
            {
                _service.PostEventGridEventAsync(eventType, subject, payload);
            }
            catch
            {
                // Expected to fail due to invalid credentials, but we want to verify logging
            }

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("PostEventGridEventAsync starting")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void PostEventGridEventAsync_ShouldLogTopicEndpointUri()
        {
            // Arrange
            var testEndpoint = "https://test.eventgrid.azure.net/api/events";
            Environment.SetEnvironmentVariable("EventGridTopicEndpoint", testEndpoint);
            Environment.SetEnvironmentVariable("EventGridTopicKey", "test-key");

            var eventType = "TestEvent";
            var subject = "test/subject";
            var payload = new { message = "test" };

            // Act
            try
            {
                _service.PostEventGridEventAsync(eventType, subject, payload);
            }
            catch
            {
                // Expected to fail due to invalid credentials, but we want to verify logging
            }

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"topicEndpointUri ={testEndpoint}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void PostEventGridEventAsync_ShouldLogEventType()
        {
            // Arrange
            Environment.SetEnvironmentVariable("EventGridTopicEndpoint", "https://test.eventgrid.azure.net/api/events");
            Environment.SetEnvironmentVariable("EventGridTopicKey", "test-key");

            var eventType = "SessionCreated";
            var subject = "test/subject";
            var payload = new { message = "test" };

            // Act
            try
            {
                _service.PostEventGridEventAsync(eventType, subject, payload);
            }
            catch
            {
                // Expected to fail due to invalid credentials, but we want to verify logging
            }

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"EventType ={eventType}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void PostEventGridEventAsync_ShouldLogSubject()
        {
            // Arrange
            Environment.SetEnvironmentVariable("EventGridTopicEndpoint", "https://test.eventgrid.azure.net/api/events");
            Environment.SetEnvironmentVariable("EventGridTopicKey", "test-key");

            var eventType = "TestEvent";
            var subject = "smoker123/session456";
            var payload = new { message = "test" };

            // Act
            try
            {
                _service.PostEventGridEventAsync(eventType, subject, payload);
            }
            catch
            {
                // Expected to fail due to invalid credentials, but we want to verify logging
            }

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Subject ={subject}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData("SessionCreated", "smoker1/session1")]
        [InlineData("SessionUpdated", "smoker2/session2")]
        [InlineData("SessionDeleted", "smoker3/session3")]
        public void PostEventGridEventAsync_WithDifferentEventTypes_ShouldLogCorrectly(string eventType, string subject)
        {
            // Arrange
            Environment.SetEnvironmentVariable("EventGridTopicEndpoint", "https://test.eventgrid.azure.net/api/events");
            Environment.SetEnvironmentVariable("EventGridTopicKey", "test-key");

            var payload = new { id = "123", title = "Test Session" };

            // Act
            try
            {
                _service.PostEventGridEventAsync(eventType, subject, payload);
            }
            catch
            {
                // Expected to fail due to invalid credentials, but we want to verify logging
            }

            // Assert - Verify all required log messages are present
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("PostEventGridEventAsync starting")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"EventType ={eventType}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Subject ={subject}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}