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
    public class TelemetryIntervalTests
    {
        private readonly Mock<ILogger> _mockLogger;

        public TelemetryIntervalTests()
        {
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void TelemetryInterval_ShouldBeStaticClass()
        {
            // Assert
            typeof(TelemetryInterval).Should().BeStatic();
        }

        [Fact]
        public async Task Run_WithNullSmokerId_ShouldReturnBadRequest()
        {
            // Arrange
            string? nullSmokerId = null;
            var testValue = "5";

            // Act
            var result = await TelemetryInterval.Run(testValue, nullSmokerId!, _mockLogger.Object);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)result;
            badRequestResult.Value.Should().BeEquivalentTo(new { error = "Missing required property 'smokerId'." });
        }

        [Fact]
        public async Task Run_WithEmptySmokerId_ShouldReturnBadRequest()
        {
            // Arrange
            var emptySmokerId = "";
            var testValue = "5";

            // Act
            var result = await TelemetryInterval.Run(testValue, emptySmokerId, _mockLogger.Object);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)result;
            badRequestResult.Value.Should().BeEquivalentTo(new { error = "Missing required property 'smokerId'." });
        }

        [Fact]
        public async Task Run_WithNullValue_ShouldReturnBadRequest()
        {
            // Arrange
            var validSmokerId = "test-smoker";
            string? nullValue = null;

            // Act
            var result = await TelemetryInterval.Run(nullValue!, validSmokerId, _mockLogger.Object);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)result;
            badRequestResult.Value.Should().Be("Missing body value. Body should be a single integer.");
        }

        [Fact]
        public async Task Run_WithEmptyValue_ShouldReturnBadRequest()
        {
            // Arrange
            var validSmokerId = "test-smoker";
            var emptyValue = "";

            // Act
            var result = await TelemetryInterval.Run(emptyValue, validSmokerId, _mockLogger.Object);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)result;
            badRequestResult.Value.Should().Be("Missing body value. Body should be a single integer.");
        }

        [Theory]
        [InlineData("not-a-number")]
        [InlineData("abc")]
        [InlineData("12.5")]
        [InlineData("")]
        [InlineData(" ")]
        public async Task Run_WithInvalidIntegerValue_ShouldReturnBadRequest(string invalidValue)
        {
            // Arrange
            var validSmokerId = "test-smoker";

            // Act
            var result = await TelemetryInterval.Run(invalidValue, validSmokerId, _mockLogger.Object);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)result;
            badRequestResult.Value.Should().Be("Could not parse body value to integer. Body should be a single integer.");
        }

        [Theory]
        [InlineData("0")]   // Below minimum
        [InlineData("61")]  // Above maximum
        [InlineData("-1")]  // Negative
        [InlineData("100")] // Far above maximum
        public async Task Run_WithValueOutOfRange_ShouldReturnBadRequest(string outOfRangeValue)
        {
            // Arrange
            var validSmokerId = "test-smoker";

            // Act
            var result = await TelemetryInterval.Run(outOfRangeValue, validSmokerId, _mockLogger.Object);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = (BadRequestObjectResult)result;
            badRequestResult.Value.Should().Be("Value out of range. Body should be a single integer 1-60.");
        }

        [Theory]
        [InlineData("1")]   // Minimum valid
        [InlineData("60")]  // Maximum valid
        [InlineData("30")]  // Common value
        [InlineData("15")]  // Another common value
        public async Task Run_WithValidValue_WithoutServiceConnection_ShouldThrowException(string validValue)
        {
            // Arrange
            var validSmokerId = "test-smoker";

            // Act & Assert
            var exception = await Record.ExceptionAsync(async () =>
                await TelemetryInterval.Run(validValue, validSmokerId, _mockLogger.Object));
            
            exception.Should().NotBeNull();
        }

        [Fact]
        public async Task Run_ShouldLogErrorForMissingSmokerId()
        {
            // Arrange
            string? nullSmokerId = null;
            var testValue = "5";

            // Act
            await TelemetryInterval.Run(testValue, nullSmokerId!, _mockLogger.Object);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("TelemetryInterval: Missing smokerId")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.Once);
        }

        [Fact]
        public async Task Run_ShouldLogWarningForMissingValue()
        {
            // Arrange
            var validSmokerId = "test-smoker";
            string? nullValue = null;

            // Act
            await TelemetryInterval.Run(nullValue!, validSmokerId, _mockLogger.Object);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("telemetryinterval : missing body value")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.Once);
        }

        [Fact]
        public async Task Run_ShouldLogWarningForInvalidInteger()
        {
            // Arrange
            var validSmokerId = "test-smoker";
            var invalidValue = "not-a-number";

            // Act
            await TelemetryInterval.Run(invalidValue, validSmokerId, _mockLogger.Object);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("telemetryinterval : could not parse body value to integer")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.Once);
        }

        [Fact]
        public async Task Run_ShouldLogWarningForOutOfRangeValue()
        {
            // Arrange
            var validSmokerId = "test-smoker";
            var outOfRangeValue = "100";

            // Act
            await TelemetryInterval.Run(outOfRangeValue, validSmokerId, _mockLogger.Object);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("telemetryinterval : interval out of range (1-60)")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.Once);
        }

        [Fact]
        public async Task Run_WithValidInputs_ShouldLogInformationMessages()
        {
            // Arrange
            var validSmokerId = "test-smoker";
            var validValue = "15";

            // Act
            try
            {
                await TelemetryInterval.Run(validValue, validSmokerId, _mockLogger.Object);
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
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("TelemetryInterval called")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("value = " + validValue)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
                Times.Once);
        }

        [Fact]
        public void TelemetryInterval_ShouldHaveCorrectFunctionName()
        {
            // Arrange
            var method = typeof(TelemetryInterval).GetMethod("Run");
            
            // Act & Assert
            method.Should().NotBeNull();
            var functionNameAttribute = method!.GetCustomAttributes(typeof(Microsoft.Azure.WebJobs.FunctionNameAttribute), false);
            functionNameAttribute.Should().HaveCount(1);
            
            var attribute = (Microsoft.Azure.WebJobs.FunctionNameAttribute)functionNameAttribute[0];
            attribute.Name.Should().Be("telemetryinterval");
        }

        [Fact]
        public void TelemetryInterval_ShouldHaveCorrectParameterConfiguration()
        {
            // Arrange
            var method = typeof(TelemetryInterval).GetMethod("Run");
            var parameters = method!.GetParameters();
            
            // Act & Assert
            parameters.Should().HaveCount(3);
            parameters[0].Name.Should().Be("value");
            parameters[1].Name.Should().Be("smokerId");
            parameters[2].Name.Should().Be("log");
            
            // Verify parameter types
            parameters[0].ParameterType.Name.Should().Be("String");
            parameters[1].ParameterType.Name.Should().Be("String");
            parameters[2].ParameterType.Name.Should().Be("ILogger");
        }
    }
}