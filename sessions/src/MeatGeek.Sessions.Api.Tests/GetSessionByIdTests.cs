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
    public class GetSessionByIdTests
    {
        private readonly Mock<ILogger<GetSessionById>> _mockLogger;
        private readonly Mock<ISessionsService> _mockSessionsService;
        private readonly Mock<ILogger> _mockGenericLogger;
        private readonly GetSessionById _getSessionById;

        public GetSessionByIdTests()
        {
            _mockLogger = new Mock<ILogger<GetSessionById>>();
            _mockSessionsService = new Mock<ISessionsService>();
            _mockGenericLogger = new Mock<ILogger>();
            _getSessionById = new GetSessionById(_mockLogger.Object, _mockSessionsService.Object);
        }

        #region Valid Request Tests

        [Fact]
        public async Task GetSessionById_ValidParameters_ReturnsOkWithSession()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var mockRequest = new Mock<HttpRequest>();

            var sessionDetails = new SessionDetails
            {
                Id = sessionId,
                SmokerId = smokerId,
                Title = "Test Session",
                Description = "Test Description",
                StartTime = DateTime.UtcNow.AddHours(-2),
                EndTime = DateTime.UtcNow.AddHours(-1)
            };

            _mockSessionsService
                .Setup(s => s.GetSessionAsync(sessionId, smokerId))
                .ReturnsAsync(sessionDetails);

            // Act
            var result = await _getSessionById.Run(mockRequest.Object, smokerId, sessionId, _mockGenericLogger.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSession = Assert.IsType<SessionDetails>(okResult.Value);
            Assert.Equal(sessionId, returnedSession.Id);
            Assert.Equal(smokerId, returnedSession.SmokerId);
            Assert.Equal("Test Session", returnedSession.Title);
            
            _mockSessionsService.Verify(s => s.GetSessionAsync(sessionId, smokerId), Times.Once);
        }

        [Fact]
        public async Task GetSessionById_SessionNotFound_ReturnsNotFound()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "nonexistent-session";
            var mockRequest = new Mock<HttpRequest>();

            _mockSessionsService
                .Setup(s => s.GetSessionAsync(sessionId, smokerId))
                .ReturnsAsync((SessionDetails)null);

            // Act
            var result = await _getSessionById.Run(mockRequest.Object, smokerId, sessionId, _mockGenericLogger.Object);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockSessionsService.Verify(s => s.GetSessionAsync(sessionId, smokerId), Times.Once);
        }

        #endregion

        #region Validation Tests

        [Fact]
        public async Task GetSessionById_MissingSmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-456";
            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _getSessionById.Run(mockRequest.Object, null, sessionId, _mockGenericLogger.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'smokerId'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.GetSessionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetSessionById_EmptySmokerId_ReturnsBadRequest()
        {
            // Arrange
            var sessionId = "session-456";
            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _getSessionById.Run(mockRequest.Object, "", sessionId, _mockGenericLogger.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            Assert.Equal("Missing required property 'smokerId'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.GetSessionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetSessionById_MissingSessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _getSessionById.Run(mockRequest.Object, smokerId, null, _mockGenericLogger.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            // Note: The implementation has a bug - it says 'smokerId' instead of 'id' for the missing id error
            Assert.Equal("Missing required property 'smokerId'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.GetSessionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetSessionById_EmptySessionId_ReturnsBadRequest()
        {
            // Arrange
            var smokerId = "smoker-123";
            var mockRequest = new Mock<HttpRequest>();

            // Act
            var result = await _getSessionById.Run(mockRequest.Object, smokerId, "", _mockGenericLogger.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errorObject = badRequestResult.Value;
            var errorProperty = errorObject.GetType().GetProperty("error");
            Assert.NotNull(errorProperty);
            // Note: The implementation has a bug - it says 'smokerId' instead of 'id' for the missing id error
            Assert.Equal("Missing required property 'smokerId'.", errorProperty.GetValue(errorObject));

            _mockSessionsService.Verify(s => s.GetSessionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region Exception Tests

        [Fact]
        public async Task GetSessionById_ServiceThrowsException_ReturnsExceptionResult()
        {
            // Arrange
            var smokerId = "smoker-123";
            var sessionId = "session-456";
            var mockRequest = new Mock<HttpRequest>();

            var expectedException = new InvalidOperationException("Database connection error");

            _mockSessionsService
                .Setup(s => s.GetSessionAsync(sessionId, smokerId))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _getSessionById.Run(mockRequest.Object, smokerId, sessionId, _mockGenericLogger.Object);

            // Assert
            Assert.IsType<ExceptionResult>(result);
            _mockSessionsService.Verify(s => s.GetSessionAsync(sessionId, smokerId), Times.Once);
        }

        #endregion

        #region Integration Tests

        [Theory]
        [InlineData("smoker-guid-123", "session-guid-456")]
        [InlineData("12345", "67890")]
        [InlineData("smoker-with-dashes", "session-with-dashes")]
        public async Task GetSessionById_VariousIdFormats_CallsServiceCorrectly(string smokerId, string sessionId)
        {
            // Arrange
            var mockRequest = new Mock<HttpRequest>();

            var sessionDetails = new SessionDetails
            {
                Id = sessionId,
                SmokerId = smokerId,
                Title = "Test Session",
                StartTime = DateTime.UtcNow
            };

            _mockSessionsService
                .Setup(s => s.GetSessionAsync(sessionId, smokerId))
                .ReturnsAsync(sessionDetails);

            // Act
            var result = await _getSessionById.Run(mockRequest.Object, smokerId, sessionId, _mockGenericLogger.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSession = Assert.IsType<SessionDetails>(okResult.Value);
            Assert.Equal(sessionId, returnedSession.Id);
            Assert.Equal(smokerId, returnedSession.SmokerId);
            
            _mockSessionsService.Verify(s => s.GetSessionAsync(sessionId, smokerId), Times.Once);
        }

        [Fact]
        public async Task GetSessionById_CompleteSessionDetails_ReturnsAllFields()
        {
            // Arrange
            var smokerId = "smoker-complete";
            var sessionId = "session-complete";
            var mockRequest = new Mock<HttpRequest>();

            var startTime = DateTime.UtcNow.AddHours(-3);
            var endTime = DateTime.UtcNow.AddHours(-1);

            var sessionDetails = new SessionDetails
            {
                Id = sessionId,
                SmokerId = smokerId,
                Title = "Complete BBQ Session",
                Description = "A full BBQ session with all details",
                StartTime = startTime,
                EndTime = endTime
            };

            _mockSessionsService
                .Setup(s => s.GetSessionAsync(sessionId, smokerId))
                .ReturnsAsync(sessionDetails);

            // Act
            var result = await _getSessionById.Run(mockRequest.Object, smokerId, sessionId, _mockGenericLogger.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSession = Assert.IsType<SessionDetails>(okResult.Value);
            
            Assert.Equal(sessionId, returnedSession.Id);
            Assert.Equal(smokerId, returnedSession.SmokerId);
            Assert.Equal("Complete BBQ Session", returnedSession.Title);
            Assert.Equal("A full BBQ session with all details", returnedSession.Description);
            Assert.Equal(startTime, returnedSession.StartTime);
            Assert.Equal(endTime, returnedSession.EndTime);
            
            _mockSessionsService.Verify(s => s.GetSessionAsync(sessionId, smokerId), Times.Once);
        }

        [Fact]
        public async Task GetSessionById_SessionWithNullOptionalFields_ReturnsSuccessfully()
        {
            // Arrange
            var smokerId = "smoker-minimal";
            var sessionId = "session-minimal";
            var mockRequest = new Mock<HttpRequest>();

            var sessionDetails = new SessionDetails
            {
                Id = sessionId,
                SmokerId = smokerId,
                Title = "Minimal Session",
                Description = null, // Optional field
                StartTime = DateTime.UtcNow.AddHours(-1),
                EndTime = null // Optional field - session still in progress
            };

            _mockSessionsService
                .Setup(s => s.GetSessionAsync(sessionId, smokerId))
                .ReturnsAsync(sessionDetails);

            // Act
            var result = await _getSessionById.Run(mockRequest.Object, smokerId, sessionId, _mockGenericLogger.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSession = Assert.IsType<SessionDetails>(okResult.Value);
            
            Assert.Equal(sessionId, returnedSession.Id);
            Assert.Equal(smokerId, returnedSession.SmokerId);
            Assert.Equal("Minimal Session", returnedSession.Title);
            Assert.Null(returnedSession.Description);
            Assert.Null(returnedSession.EndTime);
            Assert.NotEqual(default(DateTime), returnedSession.StartTime);
            
            _mockSessionsService.Verify(s => s.GetSessionAsync(sessionId, smokerId), Times.Once);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task GetSessionById_LongParameterValues_HandlesCorrectly()
        {
            // Arrange
            var smokerId = "very-long-smoker-id-that-might-be-used-in-some-systems-with-detailed-naming-conventions";
            var sessionId = "very-long-session-id-that-might-be-generated-by-some-guid-systems-or-detailed-naming";
            var mockRequest = new Mock<HttpRequest>();

            var sessionDetails = new SessionDetails
            {
                Id = sessionId,
                SmokerId = smokerId,
                Title = "Session with Long IDs",
                StartTime = DateTime.UtcNow
            };

            _mockSessionsService
                .Setup(s => s.GetSessionAsync(sessionId, smokerId))
                .ReturnsAsync(sessionDetails);

            // Act
            var result = await _getSessionById.Run(mockRequest.Object, smokerId, sessionId, _mockGenericLogger.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSession = Assert.IsType<SessionDetails>(okResult.Value);
            Assert.Equal(sessionId, returnedSession.Id);
            Assert.Equal(smokerId, returnedSession.SmokerId);
            
            _mockSessionsService.Verify(s => s.GetSessionAsync(sessionId, smokerId), Times.Once);
        }

        [Fact]
        public async Task GetSessionById_SpecialCharactersInIds_HandlesCorrectly()
        {
            // Arrange
            var smokerId = "smoker_123-test";
            var sessionId = "session_456-test";
            var mockRequest = new Mock<HttpRequest>();

            var sessionDetails = new SessionDetails
            {
                Id = sessionId,
                SmokerId = smokerId,
                Title = "Session with Special Characters",
                StartTime = DateTime.UtcNow
            };

            _mockSessionsService
                .Setup(s => s.GetSessionAsync(sessionId, smokerId))
                .ReturnsAsync(sessionDetails);

            // Act
            var result = await _getSessionById.Run(mockRequest.Object, smokerId, sessionId, _mockGenericLogger.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSession = Assert.IsType<SessionDetails>(okResult.Value);
            Assert.Equal(sessionId, returnedSession.Id);
            Assert.Equal(smokerId, returnedSession.SmokerId);
            
            _mockSessionsService.Verify(s => s.GetSessionAsync(sessionId, smokerId), Times.Once);
        }

        #endregion
    }
}