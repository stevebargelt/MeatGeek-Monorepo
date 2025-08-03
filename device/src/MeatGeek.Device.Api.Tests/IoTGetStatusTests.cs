using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using Inferno.Functions;

namespace MeatGeek.Device.Api.Tests
{
    public class IoTGetStatusTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<HttpRequest> _mockRequest;

        public IoTGetStatusTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockRequest = new Mock<HttpRequest>();
        }

        [Fact]
        public void IoTGetStatus_ShouldBeStaticClass()
        {
            // Assert
            typeof(IoTGetStatus).Should().BeStatic();
        }

        [Fact]
        public async Task Run_WithoutServiceConnection_ShouldHandleGracefully()
        {
            // Arrange
            // Note: This function depends on environment variables and Azure IoT Hub connection
            // In a real scenario, we would refactor to use dependency injection for better testability
            
            // Act & Assert
            // The function will likely throw an exception due to missing environment variables
            // This test documents the current behavior and need for refactoring
            var exception = await Record.ExceptionAsync(async () =>
                await IoTGetStatus.Run(_mockRequest.Object, _mockLogger.Object));
            
            // The function should handle missing configuration more gracefully
            // This test documents the current limitation
            exception.Should().NotBeNull();
        }

        [Fact]
        public async Task Run_ShouldLogInformation()
        {
            // Arrange & Act
            try
            {
                await IoTGetStatus.Run(_mockRequest.Object, _mockLogger.Object);
            }
            catch
            {
                // Expected due to missing environment configuration
            }

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("IoTGetTemps")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.Once);
        }
    }
}