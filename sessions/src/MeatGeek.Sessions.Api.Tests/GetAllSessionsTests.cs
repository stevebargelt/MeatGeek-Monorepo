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

        #region Valid Request Tests

        [Fact]
        public async Task GetAllSessions_ValidSmokerId_ReturnsOkWithSessions()
        {
            // Arrange
            var smokerId = "smoker-123";
            var request = TestFactory.CreateHttpRequestData(method: "GET");
            
            var sessionSummaries = new SessionSummaries();
            sessionSummaries.Add(new SessionSummary
            {
                Id = "session-1",
                SmokerId = smokerId,
                Title = "First Session",
                EndTime = DateTime.UtcNow.AddHours(-1)
            });
            sessionSummaries.Add(new SessionSummary
            {
                Id = "session-2",
                SmokerId = smokerId,
                Title = "Second Session",
                EndTime = DateTime.UtcNow.AddHours(-2)
            });

            _mockSessionsService
                .Setup(s => s.GetSessionsAsync(smokerId))
                .ReturnsAsync(sessionSummaries);

            // Act
            var result = await _getAllSessions.Run(request, smokerId);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionsAsync(smokerId), Times.Once);
        }

        [Fact]
        public async Task GetAllSessions_NoSessions_ReturnsOkWithEmptyList()
        {
            // Arrange
            var smokerId = "smoker-123";
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            var emptySessionSummaries = new SessionSummaries();

            _mockSessionsService
                .Setup(s => s.GetSessionsAsync(smokerId))
                .ReturnsAsync(emptySessionSummaries);

            // Act
            var result = await _getAllSessions.Run(request, smokerId);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionsAsync(smokerId), Times.Once);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task GetAllSessions_MissingSmokerId_ReturnsBadRequest()
        {
            // Arrange
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            // Act
            var result = await _getAllSessions.Run(request, null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionsAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetAllSessions_EmptySmokerId_ReturnsBadRequest()
        {
            // Arrange
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            // Act
            var result = await _getAllSessions.Run(request, "");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionsAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region Exception Tests

        [Fact]
        public async Task GetAllSessions_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var smokerId = "smoker-123";
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            var expectedException = new InvalidOperationException("Database error");
            
            _mockSessionsService
                .Setup(s => s.GetSessionsAsync(smokerId))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _getAllSessions.Run(request, smokerId);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionsAsync(smokerId), Times.Once);
        }

        #endregion

        #region Integration Tests

        [Theory]
        [InlineData("smoker-123")]
        [InlineData("SMOKER-ABC")]
        [InlineData("smoker-with-dashes")]
        public async Task GetAllSessions_VariousSmokerIdFormats_CallsServiceCorrectly(string smokerId)
        {
            // Arrange
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            var sessionSummaries = new SessionSummaries();

            _mockSessionsService
                .Setup(s => s.GetSessionsAsync(smokerId))
                .ReturnsAsync(sessionSummaries);

            // Act
            var result = await _getAllSessions.Run(request, smokerId);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionsAsync(smokerId), Times.Once);
        }

        #endregion
    }
}