using System;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MeatGeek.Sessions.WorkerApi;
using MeatGeek.Sessions.WorkerApi.Models;

namespace MeatGeek.Sessions.WorkerApi.Tests
{
    public class SessionTelemetryEventGridTriggerTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IAsyncCollector<SmokerStatus>> _mockSmokerStatusOut;

        public SessionTelemetryEventGridTriggerTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockSmokerStatusOut = new Mock<IAsyncCollector<SmokerStatus>>();
        }

        #region Valid Event Processing Tests

        [Fact]
        public async Task Run_ValidEventGridEvent_LogsCorrectly()
        {
            // Arrange
            var eventGridEvent = new EventGridEvent
            {
                Id = "test-event-id",
                EventType = "Microsoft.EventGrid.SubscriptionValidationEvent",
                Subject = "test-subject",
                Data = new { message = "Test telemetry data" }
            };

            var smokerStatus = new SmokerStatus
            {
                SmokerId = "smoker-123",
                ttl = 86400 // 24 hours
            };

            var deliveryCount = 1;
            var enqueuedTimeUtc = DateTime.UtcNow;
            var messageId = "message-123";

            // Act
            await SessionTelemetryEventGridTrigger.Run(
                eventGridEvent,
                smokerStatus,
                deliveryCount,
                enqueuedTimeUtc,
                messageId,
                _mockSmokerStatusOut.Object,
                _mockLogger.Object);

            // Assert - Verify that the function completed without throwing
            // The function primarily logs information and returns Task.CompletedTask
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Message ID = {messageId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Run_EventWithDifferentData_LogsEventData()
        {
            // Arrange
            var testData = new { 
                smokerId = "smoker-456", 
                temperature = 225.5, 
                timestamp = DateTime.UtcNow 
            };

            var eventGridEvent = new EventGridEvent
            {
                Id = "test-event-id-2",
                EventType = "Custom.Telemetry.Event",
                Subject = "smoker/456/telemetry",
                Data = testData
            };

            var smokerStatus = new SmokerStatus { SmokerId = "smoker-456" };
            var deliveryCount = 1;
            var enqueuedTimeUtc = DateTime.UtcNow;
            var messageId = "message-456";

            // Act
            await SessionTelemetryEventGridTrigger.Run(
                eventGridEvent,
                smokerStatus,
                deliveryCount,
                enqueuedTimeUtc,
                messageId,
                _mockSmokerStatusOut.Object,
                _mockLogger.Object);

            // Assert - Verify that the event data is logged
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(testData.ToString())),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public async Task Run_NullEventData_ThrowsNullReferenceException()
        {
            // Arrange
            var eventGridEvent = new EventGridEvent
            {
                Id = "test-event-id-null",
                EventType = "Test.Event",
                Subject = "test-subject",
                Data = null
            };

            var smokerStatus = new SmokerStatus { SmokerId = "smoker-null" };
            var deliveryCount = 1;
            var enqueuedTimeUtc = DateTime.UtcNow;
            var messageId = "message-null";

            // Act & Assert - Should throw NullReferenceException due to null event data
            var exception = await Record.ExceptionAsync(() => 
                SessionTelemetryEventGridTrigger.Run(
                    eventGridEvent,
                    smokerStatus,
                    deliveryCount,
                    enqueuedTimeUtc,
                    messageId,
                    _mockSmokerStatusOut.Object,
                    _mockLogger.Object));

            Assert.IsType<NullReferenceException>(exception);
        }

        [Fact]
        public async Task Run_HighDeliveryCount_ProcessesNormally()
        {
            // Arrange
            var eventGridEvent = new EventGridEvent
            {
                Id = "test-event-high-delivery",
                EventType = "Test.Event",
                Subject = "test-subject",
                Data = new { message = "High delivery count test" }
            };

            var smokerStatus = new SmokerStatus { SmokerId = "smoker-high-delivery" };
            var deliveryCount = 10; // High delivery count
            var enqueuedTimeUtc = DateTime.UtcNow.AddMinutes(-30); // 30 minutes ago
            var messageId = "message-high-delivery";

            // Act
            await SessionTelemetryEventGridTrigger.Run(
                eventGridEvent,
                smokerStatus,
                deliveryCount,
                enqueuedTimeUtc,
                messageId,
                _mockSmokerStatusOut.Object,
                _mockLogger.Object);

            // Assert - Function should still log the message ID
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Message ID = {messageId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion

        #region Parameter Validation Tests

        [Theory]
        [InlineData("")]
        [InlineData("message-empty")]
        [InlineData("very-long-message-id-that-exceeds-normal-length-expectations-for-testing")]
        public async Task Run_VariousMessageIds_LogsCorrectly(string messageId)
        {
            // Arrange
            var eventGridEvent = new EventGridEvent
            {
                Id = "test-event-id",
                EventType = "Test.Event",
                Subject = "test-subject",
                Data = new { test = "data" }
            };

            var smokerStatus = new SmokerStatus { SmokerId = "smoker-test" };
            var deliveryCount = 1;
            var enqueuedTimeUtc = DateTime.UtcNow;

            // Act
            await SessionTelemetryEventGridTrigger.Run(
                eventGridEvent,
                smokerStatus,
                deliveryCount,
                enqueuedTimeUtc,
                messageId,
                _mockSmokerStatusOut.Object,
                _mockLogger.Object);

            // Assert
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Message ID = {messageId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Run_FutureEnqueuedTime_ProcessesNormally()
        {
            // Arrange
            var eventGridEvent = new EventGridEvent
            {
                Id = "test-event-future",
                EventType = "Test.Event",
                Subject = "test-subject",
                Data = new { message = "Future enqueued time test" }
            };

            var smokerStatus = new SmokerStatus { SmokerId = "smoker-future" };
            var deliveryCount = 1;
            var enqueuedTimeUtc = DateTime.UtcNow.AddMinutes(10); // Future time
            var messageId = "message-future";

            // Act & Assert - Should not throw an exception
            var exception = await Record.ExceptionAsync(() => 
                SessionTelemetryEventGridTrigger.Run(
                    eventGridEvent,
                    smokerStatus,
                    deliveryCount,
                    enqueuedTimeUtc,
                    messageId,
                    _mockSmokerStatusOut.Object,
                    _mockLogger.Object));

            Assert.Null(exception);
        }

        #endregion

        #region Commented Code Verification Tests

        [Fact]
        public async Task Run_DoesNotCallSmokerStatusOut_VerifyCommentedBehavior()
        {
            // Arrange
            var eventGridEvent = new EventGridEvent
            {
                Id = "test-event-no-output",
                EventType = "Test.Event",
                Subject = "test-subject",
                Data = new { message = "No output test" }
            };

            var smokerStatus = new SmokerStatus 
            { 
                SmokerId = "smoker-no-output",
                ttl = null // Testing null ttl as mentioned in commented code
            };
            var deliveryCount = 1;
            var enqueuedTimeUtc = DateTime.UtcNow;
            var messageId = "message-no-output";

            // Act
            await SessionTelemetryEventGridTrigger.Run(
                eventGridEvent,
                smokerStatus,
                deliveryCount,
                enqueuedTimeUtc,
                messageId,
                _mockSmokerStatusOut.Object,
                _mockLogger.Object);

            // Assert - Verify that AddAsync is never called (since it's commented out)
            _mockSmokerStatusOut.Verify(
                o => o.AddAsync(It.IsAny<SmokerStatus>(), It.IsAny<System.Threading.CancellationToken>()),
                Times.Never);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task Run_CompleteEventProcessing_ReturnsCompletedTask()
        {
            // Arrange
            var eventGridEvent = new EventGridEvent
            {
                Id = "integration-test-event",
                EventType = "Microsoft.EventGrid.SubscriptionValidationEvent",
                Subject = "smoker/integration-test/telemetry",
                Data = new 
                { 
                    smokerId = "integration-smoker",
                    temperature = 250.0,
                    humidity = 60.5,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }
            };

            var smokerStatus = new SmokerStatus
            {
                SmokerId = "integration-smoker",
                ttl = 86400
            };

            var deliveryCount = 1;
            var enqueuedTimeUtc = DateTime.UtcNow;
            var messageId = "integration-message-123";

            // Act
            await SessionTelemetryEventGridTrigger.Run(
                eventGridEvent,
                smokerStatus,
                deliveryCount,
                enqueuedTimeUtc,
                messageId,
                _mockSmokerStatusOut.Object,
                _mockLogger.Object);

            // Assert - Method should complete without throwing
            
            // Verify both expected log calls were made
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Message ID = {messageId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(eventGridEvent.Data.ToString())),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        #endregion
    }
}