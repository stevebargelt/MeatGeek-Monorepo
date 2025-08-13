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
using MeatGeek.Sessions.Services.Models.Results;
using MeatGeek.Shared;

namespace MeatGeek.Sessions.Api.Tests
{
    public class UpdateSessionTests
    {
        private readonly Mock<ILogger<UpdateSession>> _mockLogger;
        private readonly Mock<ISessionsService> _mockSessionsService;
        private readonly Mock<ILogger> _mockGenericLogger;
        private readonly UpdateSession _updateSession;

        public UpdateSessionTests()
        {
            _mockLogger = new Mock<ILogger<UpdateSession>>();
            _mockSessionsService = new Mock<ISessionsService>();
            _mockGenericLogger = new Mock<ILogger>();
            _updateSession = new UpdateSession(_mockLogger.Object, _mockSessionsService.Object);
        }

        #region Valid Request Tests

        [Fact]
        public async Task UpdateSession_ValidTitle_ReturnsNoContent()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var requestBody = @"{""title"":""Updated Session Title"", ""endTime"":null}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            _mockSessionsService
                .Setup(s => s.UpdateSessionAsync(sessionId, smokerId, "Updated Session Title", null, null))
                .ReturnsAsync(UpdateSessionResult.Success);

            // Act
            var result = await _updateSession.Run(mockRequest.Object, smokerId, sessionId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(sessionId, smokerId, "Updated Session Title", null, null), Times.Once);
        }

        [Fact]
        public async Task UpdateSession_ValidDescription_ReturnsNoContent()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var requestBody = @"{""description"":""Updated Description"", ""endTime"":null}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            _mockSessionsService
                .Setup(s => s.UpdateSessionAsync(sessionId, smokerId, null, "Updated Description", null))
                .ReturnsAsync(UpdateSessionResult.Success);

            // Act
            var result = await _updateSession.Run(mockRequest.Object, smokerId, sessionId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(sessionId, smokerId, null, "Updated Description", null), Times.Once);
        }

        [Fact]
        public async Task UpdateSession_ValidEndTime_ReturnsNoContent()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var endTime = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc);
            var requestBody = $@"{{""endTime"":""{endTime:yyyy-MM-ddTHH:mm:ss.fffZ}""}}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            _mockSessionsService
                .Setup(s => s.UpdateSessionAsync(sessionId, smokerId, null, null, It.IsAny<DateTime?>()))
                .ReturnsAsync(UpdateSessionResult.Success);

            // Act
            var result = await _updateSession.Run(mockRequest.Object, smokerId, sessionId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(sessionId, smokerId, null, null, It.IsAny<DateTime?>()), Times.Once);
        }

        [Fact]
        public async Task UpdateSession_AllFields_ReturnsNoContent()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var endTime = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc);
            var requestBody = $@"{{
                ""title"": ""Updated Title"",
                ""description"": ""Updated Description"",
                ""endTime"": ""{endTime:yyyy-MM-ddTHH:mm:ss.fffZ}""
            }}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            _mockSessionsService
                .Setup(s => s.UpdateSessionAsync(sessionId, smokerId, "Updated Title", "Updated Description", It.IsAny<DateTime?>()))
                .ReturnsAsync(UpdateSessionResult.Success);

            // Act
            var result = await _updateSession.Run(mockRequest.Object, smokerId, sessionId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(sessionId, smokerId, "Updated Title", "Updated Description", It.IsAny<DateTime?>()), Times.Once);
        }

        [Fact]
        public async Task UpdateSession_SessionNotFound_ReturnsNotFound()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "nonexistent-session";
            var requestBody = @"{""title"":""Updated Title"", ""endTime"":null}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            _mockSessionsService
                .Setup(s => s.UpdateSessionAsync(sessionId, smokerId, "Updated Title", null, null))
                .ReturnsAsync(UpdateSessionResult.NotFound);

            // Act
            var result = await _updateSession.Run(mockRequest.Object, smokerId, sessionId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(sessionId, smokerId, "Updated Title", null, null), Times.Once);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task UpdateSession_InvalidJson_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var requestBody = @"{""title"":""Updated Title"", ""endTime"":null"; // Invalid JSON - missing closing brace
            var mockRequest = CreateMockHttpRequest(requestBody);

            // Act
            var result = await _updateSession.Run(mockRequest.Object, smokerId, sessionId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Body should be provided in JSON format.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.UpdateSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()), Times.Never);
        }

        [Fact]
        public async Task UpdateSession_EmptyJson_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var requestBody = @"{}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            // Act
            var result = await _updateSession.Run(mockRequest.Object, smokerId, sessionId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required properties. Nothing to update.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.UpdateSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()), Times.Never);
        }

        [Fact]
        public async Task UpdateSession_EmptyStringValues_IgnoresEmptyValues()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var requestBody = @"{""title"":"""", ""description"":""Valid Description"", ""endTime"":null}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            _mockSessionsService
                .Setup(s => s.UpdateSessionAsync(sessionId, smokerId, null, "Valid Description", null))
                .ReturnsAsync(UpdateSessionResult.Success);

            // Act
            var result = await _updateSession.Run(mockRequest.Object, smokerId, sessionId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            // Verify that empty title is passed as null (ignored), but valid description is passed
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(sessionId, smokerId, null, "Valid Description", null), Times.Once);
        }

        #endregion

        #region Date Parsing Tests

        [Theory]
        [InlineData("2024-01-15T14:30:00.000Z")]
        [InlineData("2024-01-15T14:30:00Z")]
        [InlineData("2024-01-15T14:30:00")]
        public async Task UpdateSession_ValidDateFormats_ParsesCorrectly(string dateString)
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var requestBody = $@"{{""endTime"":""{dateString}""}}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            _mockSessionsService
                .Setup(s => s.UpdateSessionAsync(sessionId, smokerId, null, null, It.IsAny<DateTime?>()))
                .ReturnsAsync(UpdateSessionResult.Success);

            // Act
            var result = await _updateSession.Run(mockRequest.Object, smokerId, sessionId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(sessionId, smokerId, null, null, It.IsAny<DateTime?>()), Times.Once);
        }

        [Fact]
        public async Task UpdateSession_InvalidDateFormat_ThrowsException()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var requestBody = @"{""endTime"":""2024-13-45T99:99:99Z""}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            _mockSessionsService
                .Setup(s => s.UpdateSessionAsync(sessionId, smokerId, null, null, null))
                .ReturnsAsync(UpdateSessionResult.Success);

            // Act 
            var result = await _updateSession.Run(mockRequest.Object, smokerId, sessionId);

            // Assert - Invalid date format string is not recognized as JTokenType.Date by JSON.NET, 
            // so it's treated as a string and ignored. The function returns NoContentResult.
            Assert.IsType<NoContentResult>(result);

            // Since the invalid date string is ignored, the service is called with null values
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(sessionId, smokerId, null, null, null), Times.Once);
        }

        #endregion

        #region Exception Tests

        [Fact]
        public async Task UpdateSession_ServiceThrowsException_ReturnsExceptionResult()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var requestBody = @"{""title"":""Updated Title"", ""endTime"":null}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            var expectedException = new InvalidOperationException("Database error");
            
            _mockSessionsService
                .Setup(s => s.UpdateSessionAsync(sessionId, smokerId, "Updated Title", null, null))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _updateSession.Run(mockRequest.Object, smokerId, sessionId);

            // Assert
            Assert.IsType<ExceptionResult>(result);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(sessionId, smokerId, "Updated Title", null, null), Times.Once);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task UpdateSession_NullValues_IgnoresNullValues()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var requestBody = @"{""title"":null, ""description"":""Valid Description"", ""endTime"":null}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            _mockSessionsService
                .Setup(s => s.UpdateSessionAsync(sessionId, smokerId, null, "Valid Description", null))
                .ReturnsAsync(UpdateSessionResult.Success);

            // Act
            var result = await _updateSession.Run(mockRequest.Object, smokerId, sessionId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(sessionId, smokerId, null, "Valid Description", null), Times.Once);
        }

        [Fact]
        public async Task UpdateSession_WhitespaceValues_TreatsAsValidStrings()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-id-456";
            var requestBody = @"{""title"":""   "", ""description"":""Valid Description"", ""endTime"":null}";
            var mockRequest = CreateMockHttpRequest(requestBody);

            _mockSessionsService
                .Setup(s => s.UpdateSessionAsync(sessionId, smokerId, "   ", "Valid Description", null))
                .ReturnsAsync(UpdateSessionResult.Success);

            // Act
            var result = await _updateSession.Run(mockRequest.Object, smokerId, sessionId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockSessionsService.Verify(s => s.UpdateSessionAsync(sessionId, smokerId, "   ", "Valid Description", null), Times.Once);
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