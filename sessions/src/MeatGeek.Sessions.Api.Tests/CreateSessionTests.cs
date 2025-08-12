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
using MeatGeek.Sessions.Services.Models.Response;
using MeatGeek.Shared;
using MeatGeek.Sessions.Api.Tests.Helpers;

namespace MeatGeek.Sessions.Api.Tests
{
    public class CreateSessionTests
    {
        private readonly Mock<ILogger<CreateSession>> _mockLogger;
        private readonly Mock<ISessionsService> _mockSessionsService;
        private readonly CreateSession _createSession;

        public CreateSessionTests()
        {
            _mockLogger = new Mock<ILogger<CreateSession>>();
            _mockSessionsService = new Mock<ISessionsService>();
            _createSession = new CreateSession(_mockLogger.Object, _mockSessionsService.Object);
        }

        #region Valid Request Tests

        [Fact]
        public async Task CreateSession_ValidRequest_ReturnsOkWithSessionId()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var requestBody = @"{""title"":""Test Session"",""description"":""Test Description""}";
            var request = TestFactory.CreateHttpRequestData(requestBody, "POST");
            
            _mockSessionsService
                .Setup(s => s.GetRunningSessionsAsync(smokerId))
                .ReturnsAsync(new SessionSummaries()); // Empty list

            _mockSessionsService
                .Setup(s => s.AddSessionAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<DateTime>()))
                .ReturnsAsync(sessionId);

            // Act
            var result = await _createSession.Run(request, smokerId);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetRunningSessionsAsync(smokerId), Times.Once);
            _mockSessionsService.Verify(s => s.AddSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task CreateSession_MinimalValidRequest_ReturnsOkWithSessionId()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var requestBody = @"{""title"":""Test Session""}";
            var request = TestFactory.CreateHttpRequestData(requestBody, "POST");

            _mockSessionsService
                .Setup(s => s.GetRunningSessionsAsync(smokerId))
                .ReturnsAsync(new SessionSummaries()); // Empty list

            _mockSessionsService
                .Setup(s => s.AddSessionAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<DateTime>()))
                .ReturnsAsync(sessionId);

            // Act
            var result = await _createSession.Run(request, smokerId);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetRunningSessionsAsync(smokerId), Times.Once);
            _mockSessionsService.Verify(s => s.AddSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Once);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task CreateSession_MissingSmokerId_ReturnsBadRequest()
        {
            // Arrange
            var requestBody = @"{""title"":""Test Session""}";
            var request = TestFactory.CreateHttpRequestData(requestBody, "POST");

            // Act
            var result = await _createSession.Run(request, null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetRunningSessionsAsync(It.IsAny<string>()), Times.Never);
            _mockSessionsService.Verify(s => s.AddSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task CreateSession_EmptySmokerId_ReturnsBadRequest()
        {
            // Arrange
            var requestBody = @"{""title"":""Test Session""}";
            var request = TestFactory.CreateHttpRequestData(requestBody, "POST");

            // Act
            var result = await _createSession.Run(request, "");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetRunningSessionsAsync(It.IsAny<string>()), Times.Never);
            _mockSessionsService.Verify(s => s.AddSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task CreateSession_InvalidJson_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var requestBody = @"invalid json content";
            var request = TestFactory.CreateHttpRequestData(requestBody, "POST");

            // Act
            var result = await _createSession.Run(request, smokerId);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetRunningSessionsAsync(It.IsAny<string>()), Times.Never);
            _mockSessionsService.Verify(s => s.AddSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        #endregion

        #region Business Logic Tests

        [Fact]
        public async Task CreateSession_HasRunningSession_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var requestBody = @"{""title"":""Test Session""}";
            var request = TestFactory.CreateHttpRequestData(requestBody, "POST");

            // Setup running session
            var runningSessions = new SessionSummaries();
            runningSessions.Add(new SessionSummary { Id = "running-session-1", SmokerId = smokerId });

            _mockSessionsService
                .Setup(s => s.GetRunningSessionsAsync(smokerId))
                .ReturnsAsync(runningSessions);

            // Act
            var result = await _createSession.Run(request, smokerId);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetRunningSessionsAsync(smokerId), Times.Once);
            _mockSessionsService.Verify(s => s.AddSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        #endregion

        #region Exception Tests

        [Fact]
        public async Task CreateSession_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var smokerId = "smoker-123";
            var requestBody = @"{""title"":""Test Session""}";
            var request = TestFactory.CreateHttpRequestData(requestBody, "POST");

            var expectedException = new InvalidOperationException("Database error");
            
            _mockSessionsService
                .Setup(s => s.GetRunningSessionsAsync(smokerId))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _createSession.Run(request, smokerId);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetRunningSessionsAsync(smokerId), Times.Once);
        }

        #endregion

    }
}