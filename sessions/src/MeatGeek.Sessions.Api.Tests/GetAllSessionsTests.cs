using System;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MeatGeek.Sessions;
using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Response;
using MeatGeek.Shared;

namespace MeatGeek.Sessions.Api.Tests
{
    public class GetAllSessionsTests
    {
        private readonly Mock<ILogger<GetAllSessions>> _mockLogger;
        private readonly Mock<ISessionsService> _mockSessionsService;
        private readonly Mock<ILogger> _mockGenericLogger;
        private readonly GetAllSessions _getAllSessions;

        public GetAllSessionsTests()
        {
            _mockLogger = new Mock<ILogger<GetAllSessions>>();
            _mockSessionsService = new Mock<ISessionsService>();
            _mockGenericLogger = new Mock<ILogger>();
            _getAllSessions = new GetAllSessions(_mockLogger.Object, _mockSessionsService.Object);
        }

        #region Valid Request Tests

        [Fact]
        public async Task GetAllSessions_ValidSmokerId_ReturnsOkWithSessions()
        {
            // Arrange
            var smokerId = "smoker-123";
            var mockRequest = new Mock<HttpRequest>();

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
                EndTime = null // Still running
            });

            _mockSessionsService
                .Setup(s => s.GetSessionsAsync(smokerId))
                .ReturnsAsync(sessionSummaries);

            // Act
            var result = await _getAllSessions.Run(mockRequest.Object, smokerId);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            Assert.NotNull(contentResult.Content);
            Assert.Contains("session-1", contentResult.Content);
            Assert.Contains("session-2", contentResult.Content);
            
            _mockSessionsService.Verify(s => s.GetSessionsAsync(smokerId), Times.Once);
        }

        [Fact]
        public async Task GetAllSessions_EmptySessionsList_ReturnsOkWithEmptyArray()
        {
            // Arrange
            var smokerId = "smoker-empty";
            var mockRequest = new Mock<HttpRequest>();

            var sessionSummaries = new SessionSummaries(); // Empty list

            _mockSessionsService
                .Setup(s => s.GetSessionsAsync(smokerId))
                .ReturnsAsync(sessionSummaries);

            // Act
            var result = await _getAllSessions.Run(mockRequest.Object, smokerId);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            Assert.NotNull(contentResult.Content);
            
            _mockSessionsService.Verify(s => s.GetSessionsAsync(smokerId), Times.Once);
        }

        [Fact]
        public async Task GetAllSessions_ServiceReturnsNull_ReturnsNotFound()
        {
            // Arrange
            var smokerId = "smoker-not-found";
            var mockRequest = new Mock<HttpRequest>();

            _mockSessionsService
                .Setup(s => s.GetSessionsAsync(smokerId))
                .ReturnsAsync((SessionSummaries)null);

            // Act
            var result = await _getAllSessions.Run(mockRequest.Object, smokerId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockSessionsService.Verify(s => s.GetSessionsAsync(smokerId), Times.Once);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task GetAllSessions_MissingSmokerId_ReturnsBadRequest()
        {
            // Arrange
            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _getAllSessions.Run(mockRequest.Object, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'smokerId'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.GetSessionsAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetAllSessions_EmptySmokerId_ReturnsBadRequest()
        {
            // Arrange
            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _getAllSessions.Run(mockRequest.Object, "");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'smokerId'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.GetSessionsAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region Exception Tests

        [Fact]
        public async Task GetAllSessions_ServiceThrowsException_ReturnsExceptionResult()
        {
            // Arrange
            var smokerId = "smoker-exception";
            var mockRequest = new Mock<HttpRequest>();

            var expectedException = new InvalidOperationException("Database connection error");

            _mockSessionsService
                .Setup(s => s.GetSessionsAsync(smokerId))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _getAllSessions.Run(mockRequest.Object, smokerId);

            // Assert
            Assert.IsType<ExceptionResult>(result);
            _mockSessionsService.Verify(s => s.GetSessionsAsync(smokerId), Times.Once);
        }

        #endregion

        #region Integration Tests

        [Theory]
        [InlineData("smoker-guid-123")]
        [InlineData("12345")]
        [InlineData("smoker-with-dashes")]
        public async Task GetAllSessions_VariousSmokerIdFormats_CallsServiceCorrectly(string smokerId)
        {
            // Arrange
            var mockRequest = new Mock<HttpRequest>();

            var sessionSummaries = new SessionSummaries();
            sessionSummaries.Add(new SessionSummary
            {
                Id = "test-session",
                SmokerId = smokerId,
                Title = "Test Session",
                EndTime = DateTime.UtcNow
            });

            _mockSessionsService
                .Setup(s => s.GetSessionsAsync(smokerId))
                .ReturnsAsync(sessionSummaries);

            // Act
            var result = await _getAllSessions.Run(mockRequest.Object, smokerId);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            _mockSessionsService.Verify(s => s.GetSessionsAsync(smokerId), Times.Once);
        }

        [Fact]
        public async Task GetAllSessions_LargeSessionsList_SerializesCorrectly()
        {
            // Arrange
            var smokerId = "smoker-large-list";
            var mockRequest = new Mock<HttpRequest>();

            var sessionSummaries = new SessionSummaries();
            for (int i = 0; i < 100; i++)
            {
                sessionSummaries.Add(new SessionSummary
                {
                    Id = $"session-{i}",
                    SmokerId = smokerId,
                    Title = $"Session {i}",
                    EndTime = i % 2 == 0 ? DateTime.UtcNow.AddHours(-i) : null
                });
            }

            _mockSessionsService
                .Setup(s => s.GetSessionsAsync(smokerId))
                .ReturnsAsync(sessionSummaries);

            // Act
            var result = await _getAllSessions.Run(mockRequest.Object, smokerId);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            Assert.NotNull(contentResult.Content);
            
            // Verify it contains some of the sessions
            Assert.Contains("session-0", contentResult.Content);
            Assert.Contains("session-99", contentResult.Content);
            
            _mockSessionsService.Verify(s => s.GetSessionsAsync(smokerId), Times.Once);
        }

        #endregion

        #region JSON Serialization Tests

        [Fact]
        public async Task GetAllSessions_SessionsWithNullValues_SerializesCorrectly()
        {
            // Arrange
            var smokerId = "smoker-null-values";
            var mockRequest = new Mock<HttpRequest>();

            var sessionSummaries = new SessionSummaries();
            sessionSummaries.Add(new SessionSummary
            {
                Id = "session-with-nulls",
                SmokerId = smokerId,
                Title = "Session with Null Values",
                EndTime = null // Testing null handling
            });

            _mockSessionsService
                .Setup(s => s.GetSessionsAsync(smokerId))
                .ReturnsAsync(sessionSummaries);

            // Act
            var result = await _getAllSessions.Run(mockRequest.Object, smokerId);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/json", contentResult.ContentType);
            Assert.Equal(StatusCodes.Status200OK, contentResult.StatusCode);
            Assert.NotNull(contentResult.Content);
            
            // With NullValueHandling.Ignore, null values should not appear in JSON
            Assert.DoesNotContain("\"EndTime\": null", contentResult.Content);
            
            _mockSessionsService.Verify(s => s.GetSessionsAsync(smokerId), Times.Once);
        }

        [Fact]
        public async Task GetAllSessions_FormattedJsonOutput_IsIndented()
        {
            // Arrange
            var smokerId = "smoker-formatted";
            var mockRequest = new Mock<HttpRequest>();

            var sessionSummaries = new SessionSummaries();
            sessionSummaries.Add(new SessionSummary
            {
                Id = "formatted-session",
                SmokerId = smokerId,
                Title = "Formatted Session",
                EndTime = DateTime.UtcNow
            });

            _mockSessionsService
                .Setup(s => s.GetSessionsAsync(smokerId))
                .ReturnsAsync(sessionSummaries);

            // Act
            var result = await _getAllSessions.Run(mockRequest.Object, smokerId);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.NotNull(contentResult.Content);
            
            // Indented JSON should contain newlines and spaces for formatting
            Assert.Contains("\n", contentResult.Content);
            Assert.Contains("  ", contentResult.Content); // Indentation spaces
            
            _mockSessionsService.Verify(s => s.GetSessionsAsync(smokerId), Times.Once);
        }

        #endregion
    }
}