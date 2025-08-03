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
    public class HealthCheckTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<HttpRequest> _mockRequest;

        public HealthCheckTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockRequest = new Mock<HttpRequest>();
        }

        [Fact]
        public async Task Run_ShouldReturnResult()
        {
            // Arrange
            // Note: Since HealthCheck is a static class with environment variable dependencies,
            // this is a basic structure test. In a real scenario, we would refactor to use DI.

            // Act
            var result = await HealthCheck.Run(_mockRequest.Object, _mockLogger.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task Run_ShouldLogInformation()
        {
            // Arrange & Act
            await HealthCheck.Run(_mockRequest.Object, _mockLogger.Object);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Performing health check")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.Once);
        }

        [Fact]
        public void HealthCheck_ShouldBeStaticClass()
        {
            // Assert
            typeof(HealthCheck).Should().BeStatic();
        }
    }
}