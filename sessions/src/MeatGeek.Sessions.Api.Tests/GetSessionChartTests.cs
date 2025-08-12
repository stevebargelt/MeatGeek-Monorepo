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
    public class GetSessionChartTests
    {
        private readonly Mock<ILogger<GetSessionChart>> _mockLogger;
        private readonly Mock<ISessionsService> _mockSessionsService;
        private readonly Mock<CosmosClient> _mockCosmosClient;
        private readonly GetSessionChart _getSessionChart;

        public GetSessionChartTests()
        {
            _mockLogger = new Mock<ILogger<GetSessionChart>>();
            _mockSessionsService = new Mock<ISessionsService>();
            _mockCosmosClient = new Mock<CosmosClient>();
            _getSessionChart = new GetSessionChart(_mockLogger.Object, _mockSessionsService.Object, _mockCosmosClient.Object);
        }

        #region Valid Request Tests

        [Fact]
        public async Task GetSessionChart_ValidRequest_ReturnsOkWithChart()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var timeSeries = 5;
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            var statuses = new List<SessionStatusDocument>
            {
                new SessionStatusDocument
                {
                    SmokerId = smokerId,
                    SessionId = sessionId,
                    CurrentTime = DateTime.UtcNow,
                    SetPoint = "225.0"
                }
            };

            _mockSessionsService
                .Setup(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries))
                .ReturnsAsync(statuses);

            // Act
            var result = await _getSessionChart.Run(request, smokerId, sessionId, timeSeries);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries), Times.Once);
        }

        [Fact]
        public async Task GetSessionChart_NoStatuses_ReturnsNotFound()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var timeSeries = 5;
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            _mockSessionsService
                .Setup(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries))
                .ReturnsAsync((List<SessionStatusDocument>)null);

            // Act
            var result = await _getSessionChart.Run(request, smokerId, sessionId, timeSeries);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries), Times.Once);
        }

        [Fact]
        public async Task GetSessionChart_DefaultTimeSeries_SetsTo1()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            int? timeSeries = null;
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            var statuses = new List<SessionStatusDocument> { new SessionStatusDocument() };

            _mockSessionsService
                .Setup(s => s.GetSessionChartAsync(sessionId, smokerId, 1))
                .ReturnsAsync(statuses);

            // Act
            var result = await _getSessionChart.Run(request, smokerId, sessionId, timeSeries);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(sessionId, smokerId, 1), Times.Once);
        }

        [Fact]
        public async Task GetSessionChart_TimeSeriesOver60_SetsTo60()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var timeSeries = 100; // Over the limit
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            var statuses = new List<SessionStatusDocument> { new SessionStatusDocument() };

            _mockSessionsService
                .Setup(s => s.GetSessionChartAsync(sessionId, smokerId, 60))
                .ReturnsAsync(statuses);

            // Act
            var result = await _getSessionChart.Run(request, smokerId, sessionId, timeSeries);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(sessionId, smokerId, 60), Times.Once);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task GetSessionChart_MissingSmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-456";
            var timeSeries = 5;
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            // Act
            var result = await _getSessionChart.Run(request, null, sessionId, timeSeries);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task GetSessionChart_EmptySmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-456";
            var timeSeries = 5;
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            // Act
            var result = await _getSessionChart.Run(request, "", sessionId, timeSeries);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task GetSessionChart_MissingSessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var timeSeries = 5;
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            // Act
            var result = await _getSessionChart.Run(request, smokerId, null, timeSeries);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task GetSessionChart_EmptySessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var timeSeries = 5;
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            // Act
            var result = await _getSessionChart.Run(request, smokerId, "", timeSeries);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        }

        #endregion

        #region Exception Tests

        [Fact]
        public async Task GetSessionChart_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var timeSeries = 5;
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            var expectedException = new InvalidOperationException("Database error");
            
            _mockSessionsService
                .Setup(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _getSessionChart.Run(request, smokerId, sessionId, timeSeries);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries), Times.Once);
        }

        #endregion

        #region Integration Tests

        [Theory]
        [InlineData("smoker-123", "session-456", 1)]
        [InlineData("SMOKER-ABC", "SESSION-XYZ", 10)]
        [InlineData("smoker-with-dashes", "session-with-dashes", 30)]
        public async Task GetSessionChart_VariousParameters_CallsServiceCorrectly(string smokerId, string sessionId, int timeSeries)
        {
            // Arrange
            var request = TestFactory.CreateHttpRequestData(method: "GET");

            var statuses = new List<SessionStatusDocument>
            {
                new SessionStatusDocument { SmokerId = smokerId, SessionId = sessionId }
            };

            _mockSessionsService
                .Setup(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries))
                .ReturnsAsync(statuses);

            // Act
            var result = await _getSessionChart.Run(request, smokerId, sessionId, timeSeries);

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries), Times.Once);
        }

        #endregion
    }
}