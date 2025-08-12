using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MeatGeek.Sessions;
using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Data;
using MeatGeek.Shared;
using MeatGeek.Sessions.Api.Tests.Helpers;

namespace MeatGeek.Sessions.Api.Tests
{
    public class GetAllSessionStatusesTests
    {
        private readonly Mock<ILogger<GetAllSessionStatuses>> _mockLogger;
        private readonly Mock<ISessionsService> _mockSessionsService;
        private readonly Mock<CosmosClient> _mockCosmosClient;
        private readonly GetAllSessionStatuses _getAllSessionStatuses;

        public GetAllSessionStatusesTests()
        {
            _mockLogger = new Mock<ILogger<GetAllSessionStatuses>>();
            _mockSessionsService = new Mock<ISessionsService>();
            _mockCosmosClient = new Mock<CosmosClient>();
            _getAllSessionStatuses = new GetAllSessionStatuses(_mockLogger.Object, _mockSessionsService.Object, _mockCosmosClient.Object);
        }

        #region Valid Request Tests

        [Fact]
        public async Task GetAllSessionStatuses_ValidRequest_ReturnsOkWithStatuses()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            var statuses = new SessionStatuses();
            statuses.Add(new SessionStatusDocument
            {
                SmokerId = smokerId,
                SessionId = sessionId,
                CurrentTime = DateTime.UtcNow,
                SetPoint = "225.0"
            });

            _mockSessionsService
                .Setup(s => s.GetSessionStatusesAsync(sessionId, smokerId))
                .ReturnsAsync(statuses);

            // Act
            var result = await _getAllSessionStatuses.Run(request, smokerId, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(sessionId, smokerId), Times.Once);
        }

        [Fact]
        public async Task GetAllSessionStatuses_NoStatuses_ReturnsNotFound()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            _mockSessionsService
                .Setup(s => s.GetSessionStatusesAsync(sessionId, smokerId))
                .ReturnsAsync((SessionStatuses)null);

            // Act
            var result = await _getAllSessionStatuses.Run(request, smokerId, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(sessionId, smokerId), Times.Once);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task GetAllSessionStatuses_MissingSmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-456";
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            // Act
            var result = await _getAllSessionStatuses.Run(request, null, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetAllSessionStatuses_EmptySmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-456";
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            // Act
            var result = await _getAllSessionStatuses.Run(request, "", sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetAllSessionStatuses_MissingSessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            // Act
            var result = await _getAllSessionStatuses.Run(request, smokerId, null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetAllSessionStatuses_EmptySessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            // Act
            var result = await _getAllSessionStatuses.Run(request, smokerId, "");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region Exception Tests

        [Fact]
        public async Task GetAllSessionStatuses_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            var expectedException = new InvalidOperationException("Database error");
            
            _mockSessionsService
                .Setup(s => s.GetSessionStatusesAsync(sessionId, smokerId))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _getAllSessionStatuses.Run(request, smokerId, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(sessionId, smokerId), Times.Once);
        }

        #endregion

        #region Integration Tests

        [Theory]
        [InlineData("smoker-123", "session-456")]
        [InlineData("SMOKER-ABC", "SESSION-XYZ")]
        [InlineData("smoker-with-dashes", "session-with-dashes")]
        public async Task GetAllSessionStatuses_VariousIdFormats_CallsServiceCorrectly(string smokerId, string sessionId)
        {
            // Arrange
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            var statuses = new SessionStatuses();
            statuses.Add(new SessionStatusDocument { SmokerId = smokerId, SessionId = sessionId });

            _mockSessionsService
                .Setup(s => s.GetSessionStatusesAsync(sessionId, smokerId))
                .ReturnsAsync(statuses);

            // Act
            var result = await _getAllSessionStatuses.Run(request, smokerId, sessionId);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(sessionId, smokerId), Times.Once);
        }

        #endregion
    }
}