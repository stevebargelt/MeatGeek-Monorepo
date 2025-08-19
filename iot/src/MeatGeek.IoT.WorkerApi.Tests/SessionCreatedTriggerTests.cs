using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Devices;
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
        private readonly Mock<ServiceClient> _mockServiceClient;
        private readonly Mock<ILogger<SessionCreatedTrigger>> _mockLogger;
        private readonly Mock<FunctionContext> _mockContext;
        private readonly SessionCreatedTrigger _trigger;

        public SessionCreatedTriggerTests()
        {
            _mockServiceClient = new Mock<ServiceClient>();
            _mockLogger = new Mock<ILogger<SessionCreatedTrigger>>();
            
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

            _trigger = new SessionCreatedTrigger(_mockServiceClient.Object, _mockLogger.Object);
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
                Title = "Test Session"
            };

            var eventGridEvent = new EventGridEvent
            {
                Data = JObject.FromObject(sessionCreatedData)
            };

            var mockResult = new CloudToDeviceMethodResult
            {
                Status = 200
            };
            _mockServiceClient.Setup(s => s.InvokeDeviceMethodAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<CloudToDeviceMethod>()))
                .ReturnsAsync(mockResult);

            // Act
            var exception = await Record.ExceptionAsync(() =>
                _trigger.Run(eventGridEvent, _mockContext.Object));

            // Assert
            Assert.Null(exception);
            _mockServiceClient.Verify(s => s.InvokeDeviceMethodAsync(
                smokerId, 
                "Telemetry", 
                It.IsAny<CloudToDeviceMethod>()), Times.Once);
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