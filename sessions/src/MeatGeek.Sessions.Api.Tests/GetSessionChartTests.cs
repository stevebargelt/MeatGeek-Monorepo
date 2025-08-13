using System;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MeatGeek.Sessions;
using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Data;
using MeatGeek.Shared;

namespace MeatGeek.Sessions.Api.Tests
{
    public class GetSessionChartTests
    {
        private readonly Mock<ILogger<GetSessionChart>> _mockLogger;
        private readonly Mock<ISessionsService> _mockSessionsService;
        private readonly Mock<CosmosClient> _mockCosmosClient;
        private readonly Mock<ILogger> _mockGenericLogger;
        private readonly GetSessionChart _getSessionChart;

        public GetSessionChartTests()
        {
            _mockLogger = new Mock<ILogger<GetSessionChart>>();
            _mockSessionsService = new Mock<ISessionsService>();
            _mockCosmosClient = new Mock<CosmosClient>();
            _getSessionChart = new GetSessionChart(_mockLogger.Object, _mockSessionsService.Object);
        }

        #region Valid Request Tests

        [Fact]
        public async Task GetSessionChart_ValidParametersWithTimeSeries_ReturnsOkWithChart()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var timeSeries = 5;
            var mockRequest = new Mock<HttpRequest>();

            var sessionStatuses = new SessionStatuses();
            sessionStatuses.Add(new SessionStatusDocument
            {
                Id = "chart-1",
                SessionId = sessionId,
                SmokerId = smokerId,
                CurrentTime = DateTime.UtcNow.AddMinutes(-30),
                Temps = new StatusTemps { GrillTemp = "225.0", Probe1Temp = "160.0" }
            });
            sessionStatuses.Add(new SessionStatusDocument
            {
                Id = "chart-2",
                SessionId = sessionId,
                SmokerId = smokerId,
                CurrentTime = DateTime.UtcNow.AddMinutes(-25),
                Temps = new StatusTemps { GrillTemp = "230.0", Probe1Temp = "165.0" }
            });

            _mockSessionsService
                .Setup(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries))
                .ReturnsAsync(sessionStatuses);

            // Act
            var result = await _getSessionChart.Run(mockRequest.Object, smokerId, sessionId, timeSeries);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            Assert.NotNull(contentResult.Content);
            Assert.Contains("chart-1", contentResult.Content);
            Assert.Contains("chart-2", contentResult.Content);
            
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries), Times.Once);
        }

        [Fact]
        public async Task GetSessionChart_ValidParametersWithoutTimeSeries_DefaultsToOne()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            int? timeSeries = null;
            var mockRequest = new Mock<HttpRequest>();

            var sessionStatuses = new SessionStatuses();
            sessionStatuses.Add(new SessionStatusDocument
            {
                Id = "chart-default",
                SessionId = sessionId,
                SmokerId = smokerId,
                CurrentTime = DateTime.UtcNow,
                Temps = new StatusTemps { GrillTemp = "225.0" }
            });

            _mockSessionsService
                .Setup(s => s.GetSessionChartAsync(sessionId, smokerId, 1))
                .ReturnsAsync(sessionStatuses);

            // Act
            var result = await _getSessionChart.Run(mockRequest.Object, smokerId, sessionId, timeSeries);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(sessionId, smokerId, 1), Times.Once);
        }

        [Fact]
        public async Task GetSessionChart_TimeSeriesZero_DefaultsToOne()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var timeSeries = 0;
            var mockRequest = new Mock<HttpRequest>();

            var sessionStatuses = new SessionStatuses();

            _mockSessionsService
                .Setup(s => s.GetSessionChartAsync(sessionId, smokerId, 1))
                .ReturnsAsync(sessionStatuses);

            // Act
            var result = await _getSessionChart.Run(mockRequest.Object, smokerId, sessionId, timeSeries);

            // Assert
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(sessionId, smokerId, 1), Times.Once);
        }

        [Fact]
        public async Task GetSessionChart_TimeSeriesNegative_DefaultsToOne()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var timeSeries = -5;
            var mockRequest = new Mock<HttpRequest>();

            var sessionStatuses = new SessionStatuses();

            _mockSessionsService
                .Setup(s => s.GetSessionChartAsync(sessionId, smokerId, 1))
                .ReturnsAsync(sessionStatuses);

            // Act
            var result = await _getSessionChart.Run(mockRequest.Object, smokerId, sessionId, timeSeries);

            // Assert
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(sessionId, smokerId, 1), Times.Once);
        }

        [Fact]
        public async Task GetSessionChart_TimeSeriesGreaterThan60_CapsAt60()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var timeSeries = 120;
            var mockRequest = new Mock<HttpRequest>();

            var sessionStatuses = new SessionStatuses();

            _mockSessionsService
                .Setup(s => s.GetSessionChartAsync(sessionId, smokerId, 60))
                .ReturnsAsync(sessionStatuses);

            // Act
            var result = await _getSessionChart.Run(mockRequest.Object, smokerId, sessionId, timeSeries);

            // Assert
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(sessionId, smokerId, 60), Times.Once);
        }

        [Fact]
        public async Task GetSessionChart_ServiceReturnsNull_ReturnsNotFound()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var timeSeries = 5;
            var mockRequest = new Mock<HttpRequest>();

            _mockSessionsService
                .Setup(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries))
                .ReturnsAsync((SessionStatuses)null);

            // Act
            var result = await _getSessionChart.Run(mockRequest.Object, smokerId, sessionId, timeSeries);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries), Times.Once);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task GetSessionChart_MissingSmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-456";
            var timeSeries = 5;
            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _getSessionChart.Run(mockRequest.Object, null, sessionId, timeSeries);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'smokerId'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.GetSessionChartAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task GetSessionChart_EmptySmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-456";
            var timeSeries = 5;
            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _getSessionChart.Run(mockRequest.Object, "", sessionId, timeSeries);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'smokerId'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.GetSessionChartAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task GetSessionChart_MissingSessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var timeSeries = 5;
            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _getSessionChart.Run(mockRequest.Object, smokerId, null, timeSeries);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'sessionId'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.GetSessionChartAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task GetSessionChart_EmptySessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var timeSeries = 5;
            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _getSessionChart.Run(mockRequest.Object, smokerId, "", timeSeries);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'sessionId'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.GetSessionChartAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
        }

        #endregion

        #region Exception Tests

        [Fact]
        public async Task GetSessionChart_ServiceThrowsException_ReturnsExceptionResult()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var timeSeries = 5;
            var mockRequest = new Mock<HttpRequest>();

            var expectedException = new InvalidOperationException("Database connection error");

            _mockSessionsService
                .Setup(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _getSessionChart.Run(mockRequest.Object, smokerId, sessionId, timeSeries);

            // Assert
            Assert.IsType<ExceptionResult>(result);
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries), Times.Once);
        }

        #endregion

        #region TimeSeries Parameter Tests

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(15)]
        [InlineData(30)]
        [InlineData(60)]
        public async Task GetSessionChart_ValidTimeSeriesValues_PassesToService(int timeSeries)
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var mockRequest = new Mock<HttpRequest>();

            var sessionStatuses = new SessionStatuses();
            sessionStatuses.Add(new SessionStatusDocument
            {
                Id = "chart-test",
                SessionId = sessionId,
                SmokerId = smokerId,
                CurrentTime = DateTime.UtcNow,
                Temps = new StatusTemps { GrillTemp = "225.0" }
            });

            _mockSessionsService
                .Setup(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries))
                .ReturnsAsync(sessionStatuses);

            // Act
            var result = await _getSessionChart.Run(mockRequest.Object, smokerId, sessionId, timeSeries);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries), Times.Once);
        }

        #endregion

        #region Integration Tests

        [Theory]
        [InlineData("smoker-guid-123", "session-guid-456")]
        [InlineData("12345", "67890")]
        [InlineData("smoker-with-dashes", "session-with-dashes")]
        public async Task GetSessionChart_VariousIdFormats_CallsServiceCorrectly(string smokerId, string sessionId)
        {
            // Arrange
            var timeSeries = 10;
            var mockRequest = new Mock<HttpRequest>();

            var sessionStatuses = new SessionStatuses();
            sessionStatuses.Add(new SessionStatusDocument
            {
                Id = "test-chart",
                SessionId = sessionId,
                SmokerId = smokerId,
                CurrentTime = DateTime.UtcNow,
                Temps = new StatusTemps { GrillTemp = "225.0" }
            });

            _mockSessionsService
                .Setup(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries))
                .ReturnsAsync(sessionStatuses);

            // Act
            var result = await _getSessionChart.Run(mockRequest.Object, smokerId, sessionId, timeSeries);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries), Times.Once);
        }

        [Fact]
        public async Task GetSessionChart_LargeChartData_SerializesCorrectly()
        {
            // Arrange
            var smokerId = "smoker-large-chart";
            var sessionId = "session-large-chart";
            var timeSeries = 5;
            var mockRequest = new Mock<HttpRequest>();

            var sessionStatuses = new SessionStatuses();
            for (int i = 0; i < 500; i++)
            {
                sessionStatuses.Add(new SessionStatusDocument
                {
                    Id = $"chart-{i}",
                    SessionId = sessionId,
                    SmokerId = smokerId,
                    CurrentTime = DateTime.UtcNow.AddMinutes(-i * 5),
                    Temps = new StatusTemps { GrillTemp = (225.0 + (i % 50)).ToString(), Probe1Temp = (160.0 + (i % 30)).ToString() }
                });
            }

            _mockSessionsService
                .Setup(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries))
                .ReturnsAsync(sessionStatuses);

            // Act
            var result = await _getSessionChart.Run(mockRequest.Object, smokerId, sessionId, timeSeries);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            Assert.NotNull(contentResult.Content);
            
            // Verify it contains some of the chart data
            Assert.Contains("chart-0", contentResult.Content);
            Assert.Contains("chart-499", contentResult.Content);
            
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries), Times.Once);
        }

        #endregion

        #region JSON Serialization Tests

        [Fact]
        public async Task GetSessionChart_FormattedJsonOutput_IsIndented()
        {
            // Arrange
            var smokerId = "smoker-formatted";
            var sessionId = "session-formatted";
            var timeSeries = 5;
            var mockRequest = new Mock<HttpRequest>();

            var sessionStatuses = new SessionStatuses();
            sessionStatuses.Add(new SessionStatusDocument
            {
                Id = "formatted-chart",
                SessionId = sessionId,
                SmokerId = smokerId,
                CurrentTime = DateTime.UtcNow,
                Temps = new StatusTemps { GrillTemp = "225.0" }
            });

            _mockSessionsService
                .Setup(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries))
                .ReturnsAsync(sessionStatuses);

            // Act
            var result = await _getSessionChart.Run(mockRequest.Object, smokerId, sessionId, timeSeries);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.NotNull(contentResult.Content);
            
            // Indented JSON should contain newlines and spaces for formatting
            Assert.Contains("\n", contentResult.Content);
            Assert.Contains("  ", contentResult.Content); // Indentation spaces
            
            _mockSessionsService.Verify(s => s.GetSessionChartAsync(sessionId, smokerId, timeSeries), Times.Once);
        }

        #endregion
    }
}