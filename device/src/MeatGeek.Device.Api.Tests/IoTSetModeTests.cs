using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using Inferno.Functions;

namespace MeatGeek.Device.Api.Tests
{
    public class IoTSetModeTests
    {
        private readonly Mock<ILogger> _mockLogger;

        public IoTSetModeTests()
        {
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void IoTSetMode_ShouldBeStaticClass()
        {
            // Assert
            typeof(IoTSetMode).Should().BeStatic();
        }

        [Fact]
        public async Task Run_WithNullValue_ShouldReturnBadRequest()
        {
            // Arrange
            string? nullValue = null;

            // Act
            var result = await IoTSetMode.Run(nullValue!, _mockLogger.Object);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)result;
            badRequestResult.Value.Should().Be("Missing body value. Body should be a single integer.");
        }

        [Fact]
        public async Task Run_WithEmptyValue_ShouldReturnBadRequest()
        {
            // Arrange
            var emptyValue = "";

            // Act
            var result = await IoTSetMode.Run(emptyValue, _mockLogger.Object);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)result;
            badRequestResult.Value.Should().Be("Missing body value. Body should be a single integer.");
        }

        [Fact]
        public async Task Run_WithValidValue_WithoutServiceConnection_ShouldThrowException()
        {
            // Arrange
            var validValue = "1";

            // Act & Assert
            // The function will throw an exception due to missing environment variables
            var exception = await Record.ExceptionAsync(async () =>
                await IoTSetMode.Run(validValue, _mockLogger.Object));
            
            exception.Should().NotBeNull();
        }

        [Fact]
        public async Task Run_ShouldLogInformationMessages()
        {
            // Arrange
            var testValue = "test-mode";

            // Act
            try
            {
                await IoTSetMode.Run(testValue, _mockLogger.Object);
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
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("C# HTTP trigger function processed a request")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("value = " + testValue)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.Once);
        }

        [Fact]
        public void IoTSetMode_ShouldHaveCorrectFunctionName()
        {
            // Arrange
            var method = typeof(IoTSetMode).GetMethod("Run");
            
            // Act & Assert
            method.Should().NotBeNull();
            var functionNameAttribute = method!.GetCustomAttributes(typeof(Microsoft.Azure.WebJobs.FunctionNameAttribute), false);
            functionNameAttribute.Should().HaveCount(1);
            
            var attribute = (Microsoft.Azure.WebJobs.FunctionNameAttribute)functionNameAttribute[0];
            attribute.Name.Should().Be("mode");
        }

        [Fact]
        public void IoTSetMode_ShouldHaveCorrectParameterConfiguration()
        {
            // Arrange
            var method = typeof(IoTSetMode).GetMethod("Run");
            var parameters = method!.GetParameters();
            
            // Act & Assert
            parameters.Should().HaveCount(2);
            parameters[0].Name.Should().Be("value");
            parameters[1].Name.Should().Be("log");
            
            // Verify parameter types
            parameters[0].ParameterType.Name.Should().Be("String");
            parameters[1].ParameterType.Name.Should().Be("ILogger");
        }

        [Theory]
        [InlineData("0")]
        [InlineData("1")]
        [InlineData("2")]
        [InlineData("smoking")]
        [InlineData("hold")]
        public async Task Run_WithVariousValidValues_ShouldLogValue(string value)
        {
            // Arrange & Act
            try
            {
                await IoTSetMode.Run(value, _mockLogger.Object);
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
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("value = " + value)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.Once);
        }
    }
}