using System;
using System.IO;
using System.Net;
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

        [Fact]
        public async Task DeleteSession_ValidRequest_ReturnsNoContent()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var request = TestFactory.CreateHttpRequest();

            _mockSessionsService
                .Setup(s => s.DeleteSessionAsync(sessionId, smokerId))
                .ReturnsAsync(DeleteSessionResult.Success);

            // Act
            var response = await _deleteSession.Run(request, smokerId, sessionId);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            _mockSessionsService.Verify(s => s.DeleteSessionAsync(sessionId, smokerId), Times.Once);
        }

        [Fact]
        public async Task DeleteSession_SessionNotFound_ReturnsNotFound()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "nonexistent-session";
            var request = TestFactory.CreateHttpRequest();

            _mockSessionsService
                .Setup(s => s.DeleteSessionAsync(sessionId, smokerId))
                .ReturnsAsync(DeleteSessionResult.NotFound);

            // Act
            var response = await _deleteSession.Run(request, smokerId, sessionId);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            _mockSessionsService.Verify(s => s.DeleteSessionAsync(sessionId, smokerId), Times.Once);
        }

        [Fact]
        public async Task DeleteSession_ServiceThrowsException_HandlesGracefully()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var request = TestFactory.CreateHttpRequest();

            _mockSessionsService
                .Setup(s => s.DeleteSessionAsync(sessionId, smokerId))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act
            var response = await _deleteSession.Run(request, smokerId, sessionId);

            // Assert - Function handles exception gracefully
            Assert.NotNull(response);
            _mockSessionsService.Verify(s => s.DeleteSessionAsync(sessionId, smokerId), Times.Once);
        }
    }
}