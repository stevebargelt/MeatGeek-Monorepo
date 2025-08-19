using System;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using MeatGeek.Sessions.WorkerApi;

namespace MeatGeek.Sessions.WorkerApi.Tests
{
    public class SessionTelemetryEventGridTriggerTests
    {
        private readonly Mock<FunctionContext> _mockContext;
        private readonly Mock<ILogger> _mockLogger;
        private readonly SessionTelemetryEventGridTrigger _trigger;

        public SessionTelemetryEventGridTriggerTests()
        {
            _mockLogger = new Mock<ILogger>();
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(sp =>
            {
                var factory = new Mock<ILoggerFactory>();
                factory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
                return factory.Object;
            });

            var serviceProvider = services.BuildServiceProvider();

            _mockContext = new Mock<FunctionContext>();
            _mockContext.Setup(c => c.InstanceServices).Returns(serviceProvider);

            _trigger = new SessionTelemetryEventGridTrigger();
        }

        [Fact]
        public async Task Run_ValidEventGridEvent_CompletesSuccessfully()
        {
            // Arrange
            var eventGridEvent = new EventGridEvent
            {
                Id = "test-event-id",
                EventType = "Microsoft.EventGrid.SubscriptionValidationEvent",
                Subject = "test-subject",
                Data = new { message = "Test telemetry data" }
            };

            // Act & Assert
            var exception = await Record.ExceptionAsync(() =>
                _trigger.Run(eventGridEvent, _mockContext.Object));

            Assert.Null(exception);
        }

        [Fact]
        public async Task Run_EventWithDifferentData_CompletesSuccessfully()
        {
            // Arrange
            var testData = new
            {
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

            // Act & Assert
            var exception = await Record.ExceptionAsync(() =>
                _trigger.Run(eventGridEvent, _mockContext.Object));

            Assert.Null(exception);
        }

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

            // Act & Assert
            var exception = await Record.ExceptionAsync(() =>
                _trigger.Run(eventGridEvent, _mockContext.Object));

            Assert.IsType<NullReferenceException>(exception);
        }

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

            // Act & Assert
            var exception = await Record.ExceptionAsync(() =>
                _trigger.Run(eventGridEvent, _mockContext.Object));

            Assert.Null(exception);
        }
    }
}