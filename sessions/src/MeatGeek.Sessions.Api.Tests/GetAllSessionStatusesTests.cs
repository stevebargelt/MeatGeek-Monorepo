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
    public class GetAllSessionStatusesTests
    {
        private readonly Mock<ILogger<CreateSession>> _mockLogger;
        private readonly Mock<ISessionsService> _mockSessionsService;
        private readonly Mock<CosmosClient> _mockCosmosClient;
        private readonly Mock<ILogger> _mockGenericLogger;
        private readonly GetAllSessionStatuses _getAllSessionStatuses;

        public GetAllSessionStatusesTests()
        {
            _mockLogger = new Mock<ILogger<CreateSession>>();
            _mockSessionsService = new Mock<ISessionsService>();
            _mockCosmosClient = new Mock<CosmosClient>();
            _mockGenericLogger = new Mock<ILogger>();
            _getAllSessionStatuses = new GetAllSessionStatuses(_mockLogger.Object, _mockSessionsService.Object, _mockCosmosClient.Object);
        }

        #region Valid Request Tests

        [Fact]
        public async Task GetAllSessionStatuses_ValidParameters_ReturnsOkWithStatuses()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var mockRequest = new Mock<HttpRequest>();

            var sessionStatuses = new SessionStatuses();
            sessionStatuses.Add(new SessionStatusDocument
            {
                Id = "status-1",
                SessionId = sessionId,
                SmokerId = smokerId,
                StatusTime = DateTime.UtcNow.AddMinutes(-30),
                Temps = new StatusTemps { GrillTemp = 225.0, MeatTemp = 160.0 }
            });
            sessionStatuses.Add(new SessionStatusDocument
            {
                Id = "status-2",
                SessionId = sessionId,
                SmokerId = smokerId,
                StatusTime = DateTime.UtcNow.AddMinutes(-15),
                Temps = new StatusTemps { GrillTemp = 230.0, MeatTemp = 165.0 }
            });

            _mockSessionsService
                .Setup(s => s.GetSessionStatusesAsync(sessionId, smokerId))
                .ReturnsAsync(sessionStatuses);

            // Act
            var result = await _getAllSessionStatuses.Run(mockRequest.Object, smokerId, sessionId, _mockGenericLogger.Object);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            Assert.NotNull(contentResult.Content);
            Assert.Contains("status-1", contentResult.Content);
            Assert.Contains("status-2", contentResult.Content);
            
            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(sessionId, smokerId), Times.Once);
        }

        [Fact]
        public async Task GetAllSessionStatuses_EmptyStatusesList_ReturnsOkWithEmptyArray()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var mockRequest = new Mock<HttpRequest>();

            var sessionStatuses = new SessionStatuses(); // Empty list

            _mockSessionsService
                .Setup(s => s.GetSessionStatusesAsync(sessionId, smokerId))
                .ReturnsAsync(sessionStatuses);

            // Act
            var result = await _getAllSessionStatuses.Run(mockRequest.Object, smokerId, sessionId, _mockGenericLogger.Object);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            Assert.NotNull(contentResult.Content);
            
            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(sessionId, smokerId), Times.Once);
        }

        [Fact]
        public async Task GetAllSessionStatuses_ServiceReturnsNull_ReturnsNotFound()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var mockRequest = new Mock<HttpRequest>();

            _mockSessionsService
                .Setup(s => s.GetSessionStatusesAsync(sessionId, smokerId))
                .ReturnsAsync((SessionStatuses)null);

            // Act
            var result = await _getAllSessionStatuses.Run(mockRequest.Object, smokerId, sessionId, _mockGenericLogger.Object);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(sessionId, smokerId), Times.Once);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task GetAllSessionStatuses_MissingSmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-456";
            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _getAllSessionStatuses.Run(mockRequest.Object, null, sessionId, _mockGenericLogger.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'smokerId'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetAllSessionStatuses_EmptySmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-456";
            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _getAllSessionStatuses.Run(mockRequest.Object, "", sessionId, _mockGenericLogger.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'smokerId'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetAllSessionStatuses_MissingSessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _getAllSessionStatuses.Run(mockRequest.Object, smokerId, null, _mockGenericLogger.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'sessionId'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetAllSessionStatuses_EmptySessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _getAllSessionStatuses.Run(mockRequest.Object, smokerId, "", _mockGenericLogger.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'sessionId'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region Exception Tests

        [Fact]
        public async Task GetAllSessionStatuses_ServiceThrowsException_ReturnsExceptionResult()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var mockRequest = new Mock<HttpRequest>();

            var expectedException = new InvalidOperationException("Database connection error");

            _mockSessionsService
                .Setup(s => s.GetSessionStatusesAsync(sessionId, smokerId))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _getAllSessionStatuses.Run(mockRequest.Object, smokerId, sessionId, _mockGenericLogger.Object);

            // Assert
            Assert.IsType<ExceptionResult>(result);
            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(sessionId, smokerId), Times.Once);
        }

        #endregion

        #region Integration Tests

        [Theory]
        [InlineData("smoker-guid-123", "session-guid-456")]
        [InlineData("12345", "67890")]
        [InlineData("smoker-with-dashes", "session-with-dashes")]
        public async Task GetAllSessionStatuses_VariousIdFormats_CallsServiceCorrectly(string smokerId, string sessionId)
        {
            // Arrange
            var mockRequest = new Mock<HttpRequest>();

            var sessionStatuses = new SessionStatuses();
            sessionStatuses.Add(new SessionStatusDocument
            {
                Id = "test-status",
                SessionId = sessionId,
                SmokerId = smokerId,
                StatusTime = DateTime.UtcNow,
                Temps = new StatusTemps { GrillTemp = 225.0 }
            });

            _mockSessionsService
                .Setup(s => s.GetSessionStatusesAsync(sessionId, smokerId))
                .ReturnsAsync(sessionStatuses);

            // Act
            var result = await _getAllSessionStatuses.Run(mockRequest.Object, smokerId, sessionId, _mockGenericLogger.Object);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(sessionId, smokerId), Times.Once);
        }

        [Fact]
        public async Task GetAllSessionStatuses_LargeStatusesList_SerializesCorrectly()
        {
            // Arrange
            var smokerId = "smoker-large-list";
            var sessionId = "session-large-list";
            var mockRequest = new Mock<HttpRequest>();

            var sessionStatuses = new SessionStatuses();
            for (int i = 0; i < 100; i++)
            {
                sessionStatuses.Add(new SessionStatusDocument
                {
                    Id = $"status-{i}",
                    SessionId = sessionId,
                    SmokerId = smokerId,
                    StatusTime = DateTime.UtcNow.AddMinutes(-i),
                    Temps = new StatusTemps { GrillTemp = 225.0 + i, MeatTemp = 160.0 + i }
                });
            }

            _mockSessionsService
                .Setup(s => s.GetSessionStatusesAsync(sessionId, smokerId))
                .ReturnsAsync(sessionStatuses);

            // Act
            var result = await _getAllSessionStatuses.Run(mockRequest.Object, smokerId, sessionId, _mockGenericLogger.Object);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            Assert.NotNull(contentResult.Content);
            
            // Verify it contains some of the statuses
            Assert.Contains("status-0", contentResult.Content);
            Assert.Contains("status-99", contentResult.Content);
            
            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(sessionId, smokerId), Times.Once);
        }

        #endregion

        #region JSON Serialization Tests

        [Fact]
        public async Task GetAllSessionStatuses_StatusesWithNullValues_SerializesCorrectly()
        {
            // Arrange
            var smokerId = "smoker-null-values";
            var sessionId = "session-null-values";
            var mockRequest = new Mock<HttpRequest>();

            var sessionStatuses = new SessionStatuses();
            sessionStatuses.Add(new SessionStatusDocument
            {
                Id = "status-with-nulls",
                SessionId = sessionId,
                SmokerId = smokerId,
                StatusTime = DateTime.UtcNow,
                Temps = null // Testing null handling
            });

            _mockSessionsService
                .Setup(s => s.GetSessionStatusesAsync(sessionId, smokerId))
                .ReturnsAsync(sessionStatuses);

            // Act
            var result = await _getAllSessionStatuses.Run(mockRequest.Object, smokerId, sessionId, _mockGenericLogger.Object);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            Assert.NotNull(contentResult.Content);
            
            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(sessionId, smokerId), Times.Once);
        }

        [Fact]
        public async Task GetAllSessionStatuses_FormattedJsonOutput_IsIndented()
        {
            // Arrange
            var smokerId = "smoker-formatted";
            var sessionId = "session-formatted";
            var mockRequest = new Mock<HttpRequest>();

            var sessionStatuses = new SessionStatuses();
            sessionStatuses.Add(new SessionStatusDocument
            {
                Id = "formatted-status",
                SessionId = sessionId,
                SmokerId = smokerId,
                StatusTime = DateTime.UtcNow,
                Temps = new StatusTemps { GrillTemp = 225.0 }
            });

            _mockSessionsService
                .Setup(s => s.GetSessionStatusesAsync(sessionId, smokerId))
                .ReturnsAsync(sessionStatuses);

            // Act
            var result = await _getAllSessionStatuses.Run(mockRequest.Object, smokerId, sessionId, _mockGenericLogger.Object);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.NotNull(contentResult.Content);
            
            // Indented JSON should contain newlines and spaces for formatting
            Assert.Contains("\n", contentResult.Content);
            Assert.Contains("  ", contentResult.Content); // Indentation spaces
            
            _mockSessionsService.Verify(s => s.GetSessionStatusesAsync(sessionId, smokerId), Times.Once);
        }

        #endregion
    }
}