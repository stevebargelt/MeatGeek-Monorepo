using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MeatGeek.Sessions;
using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Response;
using MeatGeek.Shared;
using MeatGeek.Sessions.Api.Tests.Helpers;

namespace MeatGeek.Sessions.Api.Tests
{
    public class GetSessionByIdTests
    {
        private readonly Mock<ILogger<GetSessionById>> _mockLogger;
        private readonly Mock<ISessionsService> _mockSessionsService;
        private readonly GetSessionById _getSessionById;

        public GetSessionByIdTests()
        {
            _mockLogger = new Mock<ILogger<GetSessionById>>();
            _mockSessionsService = new Mock<ISessionsService>();
            _getSessionById = new GetSessionById(_mockLogger.Object, _mockSessionsService.Object);
        }

        #region Valid Request Tests

        [Fact]
        public async Task GetSessionById_ValidRequest_ReturnsOkWithSession()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            var sessionSummary = new SessionDetails
            {
                Id = sessionId,
                SmokerId = smokerId,
                Title = "Test Session",
                Description = "Test Description",
                StartTime = DateTime.UtcNow.AddHours(-1)
            };

            _mockSessionsService
                .Setup(s => s.GetSessionAsync(sessionId, smokerId))
                .ReturnsAsync(sessionSummary);

            // Act
            var result = await _getSessionById.Run(request, smokerId, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionAsync(sessionId, smokerId), Times.Once);
        }

        [Fact]
        public async Task GetSessionById_SessionNotFound_ReturnsNotFound()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "nonexistent-session";
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            _mockSessionsService
                .Setup(s => s.GetSessionAsync(sessionId, smokerId))
                .ReturnsAsync((SessionDetails)null);

            // Act
            var result = await _getSessionById.Run(request, smokerId, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionAsync(sessionId, smokerId), Times.Once);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task GetSessionById_MissingSmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-456";
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            // Act
            var result = await _getSessionById.Run(request, null, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetSessionById_EmptySmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-456";
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            // Act
            var result = await _getSessionById.Run(request, "", sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetSessionById_MissingSessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            // Act
            var result = await _getSessionById.Run(request, smokerId, null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetSessionById_EmptySessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            // Act
            var result = await _getSessionById.Run(request, smokerId, "");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region Exception Tests

        [Fact]
        public async Task GetSessionById_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            var expectedException = new InvalidOperationException("Database error");
            
            _mockSessionsService
                .Setup(s => s.GetSessionAsync(sessionId, smokerId))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _getSessionById.Run(request, smokerId, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionAsync(sessionId, smokerId), Times.Once);
        }

        #endregion

        #region Integration Tests

        [Theory]
        [InlineData("smoker-123", "session-456")]
        [InlineData("SMOKER-ABC", "SESSION-XYZ")]
        [InlineData("smoker-with-dashes", "session-with-dashes")]
        public async Task GetSessionById_VariousIdFormats_CallsServiceCorrectly(string smokerId, string sessionId)
        {
            // Arrange
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            var sessionSummary = new SessionDetails
            {
                Id = sessionId,
                SmokerId = smokerId,
                Title = "Test Session"
            };

            _mockSessionsService
                .Setup(s => s.GetSessionAsync(sessionId, smokerId))
                .ReturnsAsync(sessionSummary);

            // Act
            var result = await _getSessionById.Run(request, smokerId, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionAsync(sessionId, smokerId), Times.Once);
        }

        #endregion
    }
}