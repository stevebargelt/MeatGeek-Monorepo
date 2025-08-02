using System;
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
    public class DeleteSessionTests
    {
        private readonly Mock<ISessionsService> _mockSessionsService;
        private readonly Mock<ILogger> _mockLogger;
        private readonly DeleteSession _deleteSession;

        public DeleteSessionTests()
        {
            _mockSessionsService = new Mock<ISessionsService>();
            _mockLogger = new Mock<ILogger>();
            _deleteSession = new DeleteSession(_mockSessionsService.Object);
        }

        #region Valid Request Tests

        [Fact]
        public async Task DeleteSession_ValidRequest_ReturnsNoContent()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var mockRequest = new Mock<HttpRequest>();

            _mockSessionsService
                .Setup(s => s.DeleteSessionAsync(sessionId, smokerId))
                .ReturnsAsync(DeleteSessionResult.Success);

            // Act
            var result = await _deleteSession.Run(mockRequest.Object, _mockLogger.Object, smokerId, sessionId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSessionsService.Verify(s => s.DeleteSessionAsync(sessionId, smokerId), Times.Once);
        }

        [Fact]
        public async Task DeleteSession_SessionNotFound_ReturnsNotFound()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "nonexistent-session";
            var mockRequest = new Mock<HttpRequest>();

            _mockSessionsService
                .Setup(s => s.DeleteSessionAsync(sessionId, smokerId))
                .ReturnsAsync(DeleteSessionResult.NotFound);

            // Act
            var result = await _deleteSession.Run(mockRequest.Object, _mockLogger.Object, smokerId, sessionId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockSessionsService.Verify(s => s.DeleteSessionAsync(sessionId, smokerId), Times.Once);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task DeleteSession_MissingSmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-id-456";
            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _deleteSession.Run(mockRequest.Object, _mockLogger.Object, null, sessionId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'smokerId'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.DeleteSessionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteSession_EmptySmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-id-456";
            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _deleteSession.Run(mockRequest.Object, _mockLogger.Object, "", sessionId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'smokerId'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.DeleteSessionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteSession_MissingSessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _deleteSession.Run(mockRequest.Object, _mockLogger.Object, smokerId, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'id'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.DeleteSessionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteSession_EmptySessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _deleteSession.Run(mockRequest.Object, _mockLogger.Object, smokerId, "");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'id'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.DeleteSessionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region Exception Tests

        [Fact]
        public async Task DeleteSession_ServiceThrowsException_ReturnsExceptionResult()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var mockRequest = new Mock<HttpRequest>();

            var expectedException = new InvalidOperationException("Database error");
            
            _mockSessionsService
                .Setup(s => s.DeleteSessionAsync(sessionId, smokerId))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _deleteSession.Run(mockRequest.Object, _mockLogger.Object, smokerId, sessionId);

            // Assert
            Assert.IsType<ExceptionResult>(result);
            _mockSessionsService.Verify(s => s.DeleteSessionAsync(sessionId, smokerId), Times.Once);
        }

        #endregion

        #region Integration Tests

        [Theory]
        [InlineData("guid-format-session-id")]
        [InlineData("12345")]
        [InlineData("session-with-dashes")]
        public async Task DeleteSession_VariousSessionIdFormats_CallsServiceCorrectly(string sessionId)
        {
            // Arrange
            var smokerId = "smoker-123";
            var mockRequest = new Mock<HttpRequest>();

            _mockSessionsService
                .Setup(s => s.DeleteSessionAsync(sessionId, smokerId))
                .ReturnsAsync(DeleteSessionResult.Success);

            // Act
            var result = await _deleteSession.Run(mockRequest.Object, _mockLogger.Object, smokerId, sessionId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSessionsService.Verify(s => s.DeleteSessionAsync(sessionId, smokerId), Times.Once);
        }

        #endregion
    }
}