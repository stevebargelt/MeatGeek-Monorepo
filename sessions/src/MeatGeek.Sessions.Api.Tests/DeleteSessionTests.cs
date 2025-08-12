using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MeatGeek.Sessions;
using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Results;
using MeatGeek.Shared;
using MeatGeek.Sessions.Api.Tests.Helpers;

namespace MeatGeek.Sessions.Api.Tests
{
    public class DeleteSessionTests
    {
        private readonly Mock<ISessionsService> _mockSessionsService;
        private readonly Mock<ILogger<DeleteSession>> _mockLogger;
        private readonly DeleteSession _deleteSession;

        public DeleteSessionTests()
        {
            _mockSessionsService = new Mock<ISessionsService>();
            _mockLogger = new Mock<ILogger<DeleteSession>>();
            _deleteSession = new DeleteSession(_mockLogger.Object, _mockSessionsService.Object);
        }

        #region Valid Request Tests

        [Fact]
        public async Task DeleteSession_ValidRequest_ReturnsNoContent()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var request = TestFactory.CreateHttpRequestData(method: "DELETE");

            _mockSessionsService
                .Setup(s => s.DeleteSessionAsync(sessionId, smokerId))
                .ReturnsAsync(DeleteSessionResult.Success);

            // Act
            var result = await _deleteSession.Run(request, smokerId, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
            _mockSessionsService.Verify(s => s.DeleteSessionAsync(sessionId, smokerId), Times.Once);
        }

        [Fact]
        public async Task DeleteSession_SessionNotFound_ReturnsNotFound()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "nonexistent-session";
            var request = TestFactory.CreateHttpRequestData(method: "DELETE");

            _mockSessionsService
                .Setup(s => s.DeleteSessionAsync(sessionId, smokerId))
                .ReturnsAsync(DeleteSessionResult.NotFound);

            // Act
            var result = await _deleteSession.Run(request, smokerId, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            _mockSessionsService.Verify(s => s.DeleteSessionAsync(sessionId, smokerId), Times.Once);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task DeleteSession_MissingSmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-456";
            var request = TestFactory.CreateHttpRequestData(method: "DELETE");

            // Act
            var result = await _deleteSession.Run(request, null, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.DeleteSessionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteSession_EmptySmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-456";
            var request = TestFactory.CreateHttpRequestData(method: "DELETE");

            // Act
            var result = await _deleteSession.Run(request, "", sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.DeleteSessionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteSession_MissingSessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var request = TestFactory.CreateHttpRequestData(method: "DELETE");

            // Act
            var result = await _deleteSession.Run(request, smokerId, null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.DeleteSessionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteSession_EmptySessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var request = TestFactory.CreateHttpRequestData(method: "DELETE");

            // Act
            var result = await _deleteSession.Run(request, smokerId, "");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.DeleteSessionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region Exception Tests

        [Fact]
        public async Task DeleteSession_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var request = TestFactory.CreateHttpRequestData(method: "DELETE");

            var expectedException = new InvalidOperationException("Database error");
            
            _mockSessionsService
                .Setup(s => s.DeleteSessionAsync(sessionId, smokerId))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _deleteSession.Run(request, smokerId, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
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
            var request = TestFactory.CreateHttpRequestData(method: "DELETE");

            _mockSessionsService
                .Setup(s => s.DeleteSessionAsync(sessionId, smokerId))
                .ReturnsAsync(DeleteSessionResult.Success);

            // Act
            var result = await _deleteSession.Run(request, smokerId, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
            _mockSessionsService.Verify(s => s.DeleteSessionAsync(sessionId, smokerId), Times.Once);
        }

        #endregion
    }
}