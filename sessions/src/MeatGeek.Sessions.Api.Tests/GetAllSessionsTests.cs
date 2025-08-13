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
using MeatGeek.Sessions.Services.Models.Response;

namespace MeatGeek.Sessions.Api.Tests
{
    public class GetAllSessionsTests
    {
        private readonly Mock<ILogger<GetAllSessions>> _mockLogger;
        private readonly Mock<ISessionsService> _mockSessionsService;
        private readonly GetAllSessions _getAllSessions;

        public GetAllSessionsTests()
        {
            _mockLogger = new Mock<ILogger<GetAllSessions>>();
            _mockSessionsService = new Mock<ISessionsService>();
            _getAllSessions = new GetAllSessions(_mockLogger.Object, _mockSessionsService.Object);
        }

        [Fact]
        public async Task GetAllSessions_ValidSmokerId_ReturnsOkWithSessions()
        {
            // Arrange
            var smokerId = "smoker-123";
            var request = TestFactory.CreateHttpRequest();

            var sessionSummaries = new SessionSummaries();
            sessionSummaries.Add(new SessionSummary
            {
                Id = "session-1",
                Title = "Brisket Cook",
                SmokerId = smokerId
            });
            sessionSummaries.Add(new SessionSummary
            {
                Id = "session-2", 
                Title = "Ribs Cook",
                SmokerId = smokerId
            });

            _mockSessionsService
                .Setup(s => s.GetSessionsAsync(smokerId))
                .ReturnsAsync(sessionSummaries);

            // Act
            var response = await _getAllSessions.Run(request, smokerId);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            
            // Verify response contains session data
            response.Body.Position = 0;
            using var reader = new StreamReader(response.Body);
            var responseBody = await reader.ReadToEndAsync();
            Assert.Contains("session-1", responseBody);
            Assert.Contains("Brisket Cook", responseBody);

            _mockSessionsService.Verify(s => s.GetSessionsAsync(smokerId), Times.Once);
        }

        [Fact]
        public async Task GetAllSessions_ValidSmokerIdEmptyResult_ReturnsOk()
        {
            // Arrange
            var smokerId = "smoker-with-no-sessions";
            var request = TestFactory.CreateHttpRequest();

            var emptySessions = new SessionSummaries(); // Empty list

            _mockSessionsService
                .Setup(s => s.GetSessionsAsync(smokerId))
                .ReturnsAsync(emptySessions);

            // Act
            var response = await _getAllSessions.Run(request, smokerId);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            _mockSessionsService.Verify(s => s.GetSessionsAsync(smokerId), Times.Once);
        }

        [Fact]
        public async Task GetAllSessions_ServiceCallsSucceed_ReturnsOk()
        {
            // Arrange
            var smokerId = "smoker-valid";
            var request = TestFactory.CreateHttpRequest();

            var sessions = new SessionSummaries();
            sessions.Add(new SessionSummary { Id = "test-session", Title = "Test", SmokerId = smokerId });

            _mockSessionsService
                .Setup(s => s.GetSessionsAsync(smokerId))
                .ReturnsAsync(sessions);

            // Act
            var response = await _getAllSessions.Run(request, smokerId);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            _mockSessionsService.Verify(s => s.GetSessionsAsync(smokerId), Times.Once);
        }

        [Fact]
        public async Task GetAllSessions_ServiceThrowsException_HandlesGracefully()
        {
            // Arrange
            var smokerId = "smoker-123";
            var request = TestFactory.CreateHttpRequest();

            _mockSessionsService
                .Setup(s => s.GetSessionsAsync(smokerId))
                .ThrowsAsync(new InvalidOperationException("Database connection error"));

            // Act
            var response = await _getAllSessions.Run(request, smokerId);

            // Assert - Function handles exception gracefully
            Assert.NotNull(response);
            _mockSessionsService.Verify(s => s.GetSessionsAsync(smokerId), Times.Once);
        }
    }
}