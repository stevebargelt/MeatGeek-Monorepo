using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MeatGeek.Sessions.Api;
using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Results;

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

        [Fact]
        public async Task UpdateSession_ValidTitleUpdate_ReturnsNoContent()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var requestBody = @"{""title"":""Updated BBQ Session"",""endTime"":null}";
            var request = TestFactory.CreateHttpRequest(requestBody, "PATCH");

            _mockSessionsService
                .Setup(s => s.UpdateSessionAsync(sessionId, smokerId, "Updated BBQ Session", null, null))
                .ReturnsAsync(UpdateSessionResult.Success);

            // Act
            var response = await _updateSession.Run(request, smokerId, sessionId);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(
                sessionId, smokerId, "Updated BBQ Session", null, null), Times.Once);
        }

        [Fact]
        public async Task UpdateSession_SessionNotFound_ReturnsNotFound()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "nonexistent-session";
            var requestBody = @"{""description"":""New description"",""endTime"":null}";
            var request = TestFactory.CreateHttpRequest(requestBody, "PATCH");

            _mockSessionsService
                .Setup(s => s.UpdateSessionAsync(sessionId, smokerId, null, "New description", null))
                .ReturnsAsync(UpdateSessionResult.NotFound);

            // Act
            var response = await _updateSession.Run(request, smokerId, sessionId);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(
                sessionId, smokerId, null, "New description", null), Times.Once);
        }

        [Fact]
        public async Task UpdateSession_EndTimeUpdate_ReturnsNoContent()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var endTime = DateTime.UtcNow;
            var requestBody = $@"{{""endTime"":""{endTime:yyyy-MM-ddTHH:mm:ss.fffZ}""}}";
            var request = TestFactory.CreateHttpRequest(requestBody, "PATCH");

            _mockSessionsService
                .Setup(s => s.UpdateSessionAsync(sessionId, smokerId, null, null, It.IsAny<DateTime?>()))
                .ReturnsAsync(UpdateSessionResult.Success);

            // Act
            var response = await _updateSession.Run(request, smokerId, sessionId);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(
                sessionId, smokerId, null, null, It.IsAny<DateTime?>()), Times.Once);
        }

        [Fact]
        public async Task UpdateSession_ServiceThrowsException_HandlesGracefully()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var requestBody = @"{""title"":""Updated Title"",""endTime"":null}";
            var request = TestFactory.CreateHttpRequest(requestBody, "PATCH");

            _mockSessionsService
                .Setup(s => s.UpdateSessionAsync(sessionId, smokerId, "Updated Title", null, null))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act
            var response = await _updateSession.Run(request, smokerId, sessionId);

            // Assert - Function handles exception gracefully
            Assert.NotNull(response);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(
                sessionId, smokerId, "Updated Title", null, null), Times.Once);
        }
    }
}