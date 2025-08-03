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
    public class IoTGetTempsTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<HttpRequest> _mockRequest;

        public IoTGetTempsTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockRequest = new Mock<HttpRequest>();
        }

        [Fact]
        public void IoTGetTemps_ShouldBeStaticClass()
        {
            // Assert
            typeof(IoTGetTemps).Should().BeStatic();
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
                await IoTGetTemps.Run(_mockRequest.Object, _mockLogger.Object));
            
            // The function should handle missing configuration more gracefully
            // This test documents the current limitation
            exception.Should().NotBeNull();
        }

        [Fact]
        public async Task Run_ShouldLogMultipleInformationMessages()
        {
            // Arrange & Act
            try
            {
                await IoTGetTemps.Run(_mockRequest.Object, _mockLogger.Object);
            }
            catch
            {
                // Expected due to missing environment configuration
            }

            // Assert - Verify all the log messages are called
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("IoTGetTemps")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("START: Inferno IoT")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Yes this is calling a C2D endpoint")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.Never); // This won't be called due to exception

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("END: Inferno IoT")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.Never); // This won't be called due to exception
        }

        [Fact]
        public void IoTGetTemps_ShouldHaveCorrectFunctionName()
        {
            // Arrange
            var method = typeof(IoTGetTemps).GetMethod("Run");
            
            // Act & Assert
            method.Should().NotBeNull();
            var functionNameAttribute = method!.GetCustomAttributes(typeof(Microsoft.Azure.WebJobs.FunctionNameAttribute), false);
            functionNameAttribute.Should().HaveCount(1);
            
            var attribute = (Microsoft.Azure.WebJobs.FunctionNameAttribute)functionNameAttribute[0];
            attribute.Name.Should().Be("temps");
        }

        [Fact]
        public void IoTGetTemps_ShouldHaveCorrectParameterConfiguration()
        {
            // Arrange
            var method = typeof(IoTGetTemps).GetMethod("Run");
            var parameters = method!.GetParameters();
            
            // Act & Assert
            parameters.Should().HaveCount(2);
            parameters[0].Name.Should().Be("req");
            parameters[1].Name.Should().Be("log");
            
            // Verify parameter types
            parameters[0].ParameterType.Name.Should().Be("HttpRequest");
            parameters[1].ParameterType.Name.Should().Be("ILogger");
        }
    }
}