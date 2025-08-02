using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MeatGeek.Sessions;
using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Results;
using MeatGeek.Shared;

namespace MeatGeek.Sessions.Api.Tests
{
    public class EndSessionTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<ISessionsService> _mockSessionsService;
        private readonly EndSession _endSession;

        public EndSessionTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockSessionsService = new Mock<ISessionsService>();
            _endSession = new EndSession(_mockSessionsService.Object);
        }

        #region Valid Request Tests

        [Fact]
        public async Task EndSession_ValidRequestNoBody_ReturnsNoContentWithCurrentTime()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var mockRequest = CreateMockHttpRequest("");
            var beforeTime = DateTime.UtcNow;

            _mockSessionsService
                .Setup(s => s.EndSessionAsync(sessionId, smokerId, It.IsAny<DateTime>()))
                .ReturnsAsync(EndSessionResult.Success);

            // Act
            var result = await _endSession.Run(mockRequest.Object, _mockLogger.Object, smokerId, sessionId);
            var afterTime = DateTime.UtcNow;

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSessionsService.Verify(s => s.EndSessionAsync(
                sessionId, 
                smokerId, 
                It.Is<DateTime>(dt => dt >= beforeTime && dt <= afterTime)), Times.Once);
        }

        [Fact]
        public async Task EndSession_ValidRequestEmptyJsonBody_ReturnsNoContentWithCurrentTime()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var requestBody = "{}";
            var mockRequest = CreateMockHttpRequest(requestBody);
            var beforeTime = DateTime.UtcNow;

            _mockSessionsService
                .Setup(s => s.EndSessionAsync(sessionId, smokerId, It.IsAny<DateTime>()))
                .ReturnsAsync(EndSessionResult.Success);

            // Act
            var result = await _endSession.Run(mockRequest.Object, _mockLogger.Object, smokerId, sessionId);
            var afterTime = DateTime.UtcNow;

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSessionsService.Verify(s => s.EndSessionAsync(
                sessionId, 
                smokerId, 
                It.Is<DateTime>(dt => dt >= beforeTime && dt <= afterTime)), Times.Once);
        }

        [Fact]
        public async Task EndSession_ValidRequestWithEndTime_ReturnsNoContentWithProvidedTime()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var endTime = new DateTime(2024, 1, 15, 16, 30, 0, DateTimeKind.Utc);
            var requestBody = $@"{{""endTime"":""{endTime:yyyy-MM-ddTHH:mm:ss.fffZ}""}}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            _mockSessionsService
                .Setup(s => s.EndSessionAsync(sessionId, smokerId, It.IsAny<DateTime>()))
                .ReturnsAsync(EndSessionResult.Success);

            // Act
            var result = await _endSession.Run(mockRequest.Object, _mockLogger.Object, smokerId, sessionId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSessionsService.Verify(s => s.EndSessionAsync(
                sessionId, 
                smokerId, 
                endTime), Times.Once);
        }

        [Fact]
        public async Task EndSession_SessionNotFound_ReturnsNotFound()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "nonexistent-session";
            var mockRequest = CreateMockHttpRequest("");

            _mockSessionsService
                .Setup(s => s.EndSessionAsync(sessionId, smokerId, It.IsAny<DateTime>()))
                .ReturnsAsync(EndSessionResult.NotFound);

            // Act
            var result = await _endSession.Run(mockRequest.Object, _mockLogger.Object, smokerId, sessionId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockSessionsService.Verify(s => s.EndSessionAsync(sessionId, smokerId, It.IsAny<DateTime>()), Times.Once);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task EndSession_MissingSmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-456";
            var mockRequest = CreateMockHttpRequest("");

            // Act
            var result = await _endSession.Run(mockRequest.Object, _mockLogger.Object, null, sessionId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'smokerId'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.EndSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task EndSession_EmptySmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-456";
            var mockRequest = CreateMockHttpRequest("");

            // Act
            var result = await _endSession.Run(mockRequest.Object, _mockLogger.Object, "", sessionId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'smokerId'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.EndSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task EndSession_MissingSessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var mockRequest = CreateMockHttpRequest("");

            // Act
            var result = await _endSession.Run(mockRequest.Object, _mockLogger.Object, smokerId, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'id'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.EndSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task EndSession_EmptySessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var mockRequest = CreateMockHttpRequest("");

            // Act
            var result = await _endSession.Run(mockRequest.Object, _mockLogger.Object, smokerId, "");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'id'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.EndSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task EndSession_InvalidJson_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var requestBody = @"{""endTime"":""2024-01-15T16:30:00.000Z"","; // Invalid JSON - missing closing brace
            var mockRequest = CreateMockHttpRequest(requestBody);

            // Act
            var result = await _endSession.Run(mockRequest.Object, _mockLogger.Object, smokerId, sessionId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Body should be provided in JSON format.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.EndSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        #endregion

        #region Exception Tests

        [Fact]
        public async Task EndSession_ServiceThrowsException_ReturnsExceptionResult()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var mockRequest = CreateMockHttpRequest("");

            var expectedException = new InvalidOperationException("Database error");
            
            _mockSessionsService
                .Setup(s => s.EndSessionAsync(sessionId, smokerId, It.IsAny<DateTime>()))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _endSession.Run(mockRequest.Object, _mockLogger.Object, smokerId, sessionId);

            // Assert
            Assert.IsType<ExceptionResult>(result);
            _mockSessionsService.Verify(s => s.EndSessionAsync(sessionId, smokerId, It.IsAny<DateTime>()), Times.Once);
        }

        #endregion

        #region Date Parsing Tests

        [Theory]
        [InlineData("2024-01-15T16:30:00.000Z")]
        [InlineData("2024-12-31T23:59:59.999Z")]
        [InlineData("2023-06-15T12:00:00.000Z")]
        public async Task EndSession_VariousDateFormats_ParsesCorrectly(string endTimeString)
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var requestBody = $@"{{""endTime"":""{endTimeString}""}}";
            var mockRequest = CreateMockHttpRequest(requestBody);
            var expectedDateTime = DateTimeOffset.Parse(endTimeString).UtcDateTime;

            _mockSessionsService
                .Setup(s => s.EndSessionAsync(sessionId, smokerId, It.IsAny<DateTime>()))
                .ReturnsAsync(EndSessionResult.Success);

            // Act
            var result = await _endSession.Run(mockRequest.Object, _mockLogger.Object, smokerId, sessionId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSessionsService.Verify(s => s.EndSessionAsync(
                sessionId, 
                smokerId, 
                It.Is<DateTime>(dt => Math.Abs((dt - expectedDateTime).TotalSeconds) < 1)), Times.Once);
        }

        [Fact]
        public async Task EndSession_InvalidEndTimeValue_UsesCurrentTime()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var requestBody = @"{""endTime"":""invalid-date""}";
            var mockRequest = CreateMockHttpRequest(requestBody);
            var beforeTime = DateTime.UtcNow;

            _mockSessionsService
                .Setup(s => s.EndSessionAsync(sessionId, smokerId, It.IsAny<DateTime>()))
                .ReturnsAsync(EndSessionResult.Success);

            // Act
            var result = await _endSession.Run(mockRequest.Object, _mockLogger.Object, smokerId, sessionId);
            var afterTime = DateTime.UtcNow;

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSessionsService.Verify(s => s.EndSessionAsync(
                sessionId, 
                smokerId, 
                It.Is<DateTime>(dt => dt >= beforeTime && dt <= afterTime)), Times.Once);
        }

        [Fact]
        public async Task EndSession_NullEndTimeValue_UsesCurrentTime()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var requestBody = @"{""endTime"":null}";
            var mockRequest = CreateMockHttpRequest(requestBody);
            var beforeTime = DateTime.UtcNow;

            _mockSessionsService
                .Setup(s => s.EndSessionAsync(sessionId, smokerId, It.IsAny<DateTime>()))
                .ReturnsAsync(EndSessionResult.Success);

            // Act
            var result = await _endSession.Run(mockRequest.Object, _mockLogger.Object, smokerId, sessionId);
            var afterTime = DateTime.UtcNow;

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSessionsService.Verify(s => s.EndSessionAsync(
                sessionId, 
                smokerId, 
                It.Is<DateTime>(dt => dt >= beforeTime && dt <= afterTime)), Times.Once);
        }

        #endregion

        #region Helper Methods

        private Mock<HttpRequest> CreateMockHttpRequest(string body)
        {
            var mockRequest = new Mock<HttpRequest>();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            mockRequest.Setup(r => r.Body).Returns(stream);
            return mockRequest;
        }

        #endregion
    }
}