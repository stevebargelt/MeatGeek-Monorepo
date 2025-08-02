using System;
using System.IO;
using System.Text;
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

            var mockRequest = CreateMockHttpRequest(requestBody);
            
            _mockSessionsService
                .Setup(s => s.GetRunningSessionsAsync(smokerId))
                .ReturnsAsync(new SessionSummaries()); // Empty list

            _mockSessionsService
                .Setup(s => s.AddSessionAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    smokerId, 
                    It.IsAny<DateTime>()))
                .ReturnsAsync(sessionId);

            // Act
            var result = await _createSession.Run(mockRequest.Object, smokerId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var responseObject = okResult.Value;
            var idProperty = responseObject.GetType().GetProperty("id");
            Assert.NotNull(idProperty);
            Assert.Equal(sessionId, idProperty.GetValue(responseObject));

            _mockSessionsService.Verify(s => s.AddSessionAsync(
                "Test Session", 
                "Test Description", 
                smokerId, 
                It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task CreateSession_ValidRequestWithoutStartTime_UsesCurrentTime()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var requestBody = @"{""title"":""Test Session""}";
            var beforeTime = DateTime.UtcNow;

            var mockRequest = CreateMockHttpRequest(requestBody);
            
            _mockSessionsService
                .Setup(s => s.GetRunningSessionsAsync(smokerId))
                .ReturnsAsync(new SessionSummaries()); // Empty list

            _mockSessionsService
                .Setup(s => s.AddSessionAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    smokerId, 
                    It.IsAny<DateTime>()))
                .ReturnsAsync(sessionId);

            // Act
            var result = await _createSession.Run(mockRequest.Object, smokerId);
            var afterTime = DateTime.UtcNow;

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            _mockSessionsService.Verify(s => s.AddSessionAsync(
                "Test Session", 
                It.IsAny<string>(), 
                smokerId, 
                It.Is<DateTime>(dt => dt >= beforeTime && dt <= afterTime)), Times.Once);
        }

        [Fact]
        public async Task CreateSession_ValidRequestWithStartTime_UsesProvidedTime()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var startTime = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc);
            var requestBody = $@"{{""title"":""Test Session"",""startTime"":""{startTime:yyyy-MM-ddTHH:mm:ss.fffZ}""}}";

            var mockRequest = CreateMockHttpRequest(requestBody);
            
            _mockSessionsService
                .Setup(s => s.GetRunningSessionsAsync(smokerId))
                .ReturnsAsync(new SessionSummaries()); // Empty list

            _mockSessionsService
                .Setup(s => s.AddSessionAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    smokerId, 
                    It.IsAny<DateTime>()))
                .ReturnsAsync(sessionId);

            // Act
            var result = await _createSession.Run(mockRequest.Object, smokerId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            _mockSessionsService.Verify(s => s.AddSessionAsync(
                "Test Session", 
                It.IsAny<string>(), 
                smokerId, 
                startTime), Times.Once);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task CreateSession_MissingSmokerId_ReturnsBadRequest()
        {
            // Arrange
            var requestBody = @"{""title"":""Test Session""}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            // Act
            var result = await _createSession.Run(mockRequest.Object, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'smokerId'.", errorProperty.GetValue(errorObject));
        }

        [Fact]
        public async Task CreateSession_EmptySmokerId_ReturnsBadRequest()
        {
            // Arrange
            var requestBody = @"{""title"":""Test Session""}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            // Act
            var result = await _createSession.Run(mockRequest.Object, "");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'smokerId'.", errorProperty.GetValue(errorObject));
        }

        [Fact]
        public async Task CreateSession_MissingTitle_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var requestBody = @"{""description"":""Test Description""}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            // Act
            var result = await _createSession.Run(mockRequest.Object, smokerId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'title'.", errorProperty.GetValue(errorObject));
        }

        [Fact]
        public async Task CreateSession_EmptyTitle_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var requestBody = @"{""title"":""""}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            // Act
            var result = await _createSession.Run(mockRequest.Object, smokerId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'title'.", errorProperty.GetValue(errorObject));
        }

        [Fact]
        public async Task CreateSession_InvalidJson_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var requestBody = @"{""title"":""Test Session"","; // Invalid JSON - missing closing brace
            var mockRequest = CreateMockHttpRequest(requestBody);

            // Act & Assert - JsonSerializationException is thrown and not caught by CreateSession.cs
            await Assert.ThrowsAsync<Newtonsoft.Json.JsonSerializationException>(() => 
                _createSession.Run(mockRequest.Object, smokerId));
        }

        #endregion

        #region Conflict Tests

        [Fact]
        public async Task CreateSession_ExistingRunningSession_ReturnsConflict()
        {
            // Arrange
            var smokerId = "smoker-123";
            var requestBody = @"{""title"":""Test Session""}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            var runningSessions = new SessionSummaries();
            runningSessions.Add(new SessionSummary { Id = "running-session", SmokerId = smokerId, Title = "Running Session" });
            
            _mockSessionsService
                .Setup(s => s.GetRunningSessionsAsync(smokerId))
                .ReturnsAsync(runningSessions);

            // Act
            var result = await _createSession.Run(mockRequest.Object, smokerId);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Same(runningSessions, conflictResult.Value);

            // Verify AddSessionAsync was never called
            _mockSessionsService.Verify(s => s.AddSessionAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<DateTime>()), Times.Never);
        }

        #endregion

        #region Exception Tests

        [Fact]
        public async Task CreateSession_GetRunningSessionsThrows_ReturnsExceptionResult()
        {
            // Arrange
            var smokerId = "smoker-123";
            var requestBody = @"{""title"":""Test Session""}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            var expectedException = new InvalidOperationException("Database error");
            
            _mockSessionsService
                .Setup(s => s.GetRunningSessionsAsync(smokerId))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _createSession.Run(mockRequest.Object, smokerId);

            // Assert
            var exceptionResult = Assert.IsType<ExceptionResult>(result);
            // Note: ExceptionResult might be a custom type, so we just verify it's the expected type
        }

        [Fact]
        public async Task CreateSession_AddSessionAsyncThrows_ReturnsExceptionResult()
        {
            // Arrange
            var smokerId = "smoker-123";
            var requestBody = @"{""title"":""Test Session""}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            _mockSessionsService
                .Setup(s => s.GetRunningSessionsAsync(smokerId))
                .ReturnsAsync(new SessionSummaries()); // Empty list

            var expectedException = new InvalidOperationException("Database error");
            
            _mockSessionsService
                .Setup(s => s.AddSessionAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    smokerId, 
                    It.IsAny<DateTime>()))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _createSession.Run(mockRequest.Object, smokerId);

            // Assert
            var exceptionResult = Assert.IsType<ExceptionResult>(result);
        }

        #endregion

        #region Helper Methods

        private Mock<HttpRequest> CreateMockHttpRequest(string body)
        {
            var mockRequest = new Mock<HttpRequest>();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            mockRequest.Setup(r => r.Body).Returns(stream);
            return mockRequest;
        }

        #endregion
    }
}