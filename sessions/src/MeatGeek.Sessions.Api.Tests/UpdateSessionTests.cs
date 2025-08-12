using System;
using System.IO;
using System.Net;
using System.Text;
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
    public class UpdateSessionTests
    {
        private readonly Mock<ILogger<UpdateSession>> _mockLogger;
        private readonly Mock<ISessionsService> _mockSessionsService;
        private readonly UpdateSession _updateSession;

        public UpdateSessionTests()
        {
            _mockLogger = new Mock<ILogger<UpdateSession>>();
            _mockSessionsService = new Mock<ISessionsService>();
            _updateSession = new UpdateSession(_mockLogger.Object, _mockSessionsService.Object);
        }

        #region Valid Request Tests

        [Fact]
        public async Task UpdateSession_ValidRequest_ReturnsNoContent()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var requestBody = @"{""title"":""Updated Session"",""description"":""Updated Description""}";
            var request = TestFactory.CreateHttpRequestData(requestBody, "PUT");

            _mockSessionsService
                .Setup(s => s.UpdateSessionAsync(sessionId, smokerId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(UpdateSessionResult.Success);

            // Act
            var result = await _updateSession.Run(request, smokerId, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(sessionId, smokerId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()), Times.Once);
        }

        [Fact]
        public async Task UpdateSession_SessionNotFound_ReturnsNotFound()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "nonexistent-session";
            var requestBody = @"{""title"":""Updated Session""}";
            var request = TestFactory.CreateHttpRequestData(requestBody, "PUT");

            _mockSessionsService
                .Setup(s => s.UpdateSessionAsync(sessionId, smokerId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(UpdateSessionResult.NotFound);

            // Act
            var result = await _updateSession.Run(request, smokerId, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(sessionId, smokerId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()), Times.Once);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task UpdateSession_MissingSmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-456";
            var requestBody = @"{""title"":""Updated Session""}";
            var request = TestFactory.CreateHttpRequestData(requestBody, "PUT");

            // Act
            var result = await _updateSession.Run(request, null, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()), Times.Never);
        }

        [Fact]
        public async Task UpdateSession_EmptySessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var requestBody = @"{""title"":""Updated Session""}";
            var request = TestFactory.CreateHttpRequestData(requestBody, "PUT");

            // Act
            var result = await _updateSession.Run(request, smokerId, "");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()), Times.Never);
        }

        [Fact]
        public async Task UpdateSession_InvalidJson_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var requestBody = @"invalid json content";
            var request = TestFactory.CreateHttpRequestData(requestBody, "PUT");

            // Act
            var result = await _updateSession.Run(request, smokerId, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()), Times.Never);
        }

        #endregion

        #region Exception Tests

        [Fact]
        public async Task UpdateSession_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var requestBody = @"{""title"":""Updated Session""}";
            var request = TestFactory.CreateHttpRequestData(requestBody, "PUT");

            var expectedException = new InvalidOperationException("Database error");
            
            _mockSessionsService
                .Setup(s => s.UpdateSessionAsync(sessionId, smokerId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _updateSession.Run(request, smokerId, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(sessionId, smokerId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()), Times.Once);
        }

        #endregion

    }
}