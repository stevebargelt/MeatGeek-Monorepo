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
using MeatGeek.Sessions.Services.Models.Response;

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

        [Fact]
        public async Task CreateSession_ValidRequest_ReturnsOkWithSessionId()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "new-session-id";
            var requestBody = @"{""title"":""Test BBQ Session"",""description"":""Testing the brisket""}";
            var request = TestFactory.CreateHttpRequest(requestBody, "POST");

            _mockSessionsService
                .Setup(s => s.GetRunningSessionsAsync(smokerId))
                .ReturnsAsync(new SessionSummaries()); // No running sessions

            _mockSessionsService
                .Setup(s => s.AddSessionAsync(
                    "Test BBQ Session",
                    "Testing the brisket",
                    smokerId,
                    It.IsAny<DateTime>()))
                .ReturnsAsync(sessionId);

            // Act
            var response = await _createSession.Run(request, smokerId);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            // Verify response body contains session ID
            response.Body.Position = 0;
            using var reader = new StreamReader(response.Body);
            var responseBody = await reader.ReadToEndAsync();
            Assert.Contains(sessionId, responseBody);
            
            _mockSessionsService.Verify(s => s.GetRunningSessionsAsync(smokerId), Times.Once);
            _mockSessionsService.Verify(s => s.AddSessionAsync(
                "Test BBQ Session",
                "Testing the brisket",
                smokerId,
                It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task CreateSession_RunningSessionExists_ReturnsRunningSessionInfo()
        {
            // Arrange
            var smokerId = "smoker-123";
            var requestBody = @"{""title"":""Another Session""}";
            var request = TestFactory.CreateHttpRequest(requestBody, "POST");

            var runningSessions = new SessionSummaries();
            runningSessions.Add(new SessionSummary 
            { 
                Id = "existing-session",
                Title = "Already Running",
                SmokerId = smokerId
            });

            _mockSessionsService
                .Setup(s => s.GetRunningSessionsAsync(smokerId))
                .ReturnsAsync(runningSessions);

            // Act
            var response = await _createSession.Run(request, smokerId);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            // Verify response contains existing session info
            response.Body.Position = 0;
            using var reader = new StreamReader(response.Body);
            var responseBody = await reader.ReadToEndAsync();
            Assert.Contains("existing-session", responseBody);
            
            _mockSessionsService.Verify(s => s.GetRunningSessionsAsync(smokerId), Times.Once);
            _mockSessionsService.Verify(s => s.AddSessionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task CreateSession_ServiceThrowsException_HandlesGracefully()
        {
            // Arrange
            var smokerId = "smoker-123";
            var requestBody = @"{""title"":""Test Session""}";
            var request = TestFactory.CreateHttpRequest(requestBody, "POST");

            _mockSessionsService
                .Setup(s => s.GetRunningSessionsAsync(smokerId))
                .ThrowsAsync(new InvalidOperationException("Service unavailable"));

            // Act
            var response = await _createSession.Run(request, smokerId);

            // Assert - Function handles exception gracefully
            Assert.NotNull(response);
            _mockSessionsService.Verify(s => s.GetRunningSessionsAsync(smokerId), Times.Once);
        }
    }
}