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
    public class EndSessionTests
    {
        private readonly Mock<ILogger<EndSession>> _mockLogger;
        private readonly Mock<ISessionsService> _mockSessionsService;
        private readonly EndSession _endSession;

        public EndSessionTests()
        {
            _mockLogger = new Mock<ILogger<EndSession>>();
            _mockSessionsService = new Mock<ISessionsService>();
            _endSession = new EndSession(_mockLogger.Object, _mockSessionsService.Object);
        }

        #region Valid Request Tests

        [Fact]
        public async Task EndSession_ValidRequestNoBody_ReturnsNoContentWithCurrentTime()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var request = TestFactory.CreateHttpRequestData("", "PUT");
            
            var beforeTime = DateTime.UtcNow;

            _mockSessionsService
                .Setup(s => s.EndSessionAsync(sessionId, smokerId, It.IsAny<DateTime>()))
                .ReturnsAsync(EndSessionResult.Success);

            // Act
            var result = await _endSession.Run(request, smokerId, sessionId);
            var afterTime = DateTime.UtcNow;

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
            _mockSessionsService.Verify(s => s.EndSessionAsync(
                sessionId, 
                smokerId, 
                It.Is<DateTime>(dt => dt >= beforeTime && dt <= afterTime)), Times.Once);
        }

        [Fact]
        public async Task EndSession_SessionNotFound_ReturnsNotFound()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "nonexistent-session";
            var request = TestFactory.CreateHttpRequestData("", "PUT");

            _mockSessionsService
                .Setup(s => s.EndSessionAsync(sessionId, smokerId, It.IsAny<DateTime>()))
                .ReturnsAsync(EndSessionResult.NotFound);

            // Act
            var result = await _endSession.Run(request, smokerId, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            _mockSessionsService.Verify(s => s.EndSessionAsync(sessionId, smokerId, It.IsAny<DateTime>()), Times.Once);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task EndSession_MissingSmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-456";
            var request = TestFactory.CreateHttpRequestData("", "PUT");

            // Act
            var result = await _endSession.Run(request, null, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.EndSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        #endregion

    }
}