using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Repositories;
using MeatGeek.Sessions.Services.Models.Data;
using MeatGeek.Sessions.Services.Models.Response;
using MeatGeek.Sessions.Services.Models.Results;
using MeatGeek.Shared;
using MeatGeek.Shared.EventSchemas.Sessions;

namespace MeatGeek.Sessions.Services.Tests
{
    public class SessionsServiceTests
    {
        private readonly Mock<ISessionsRepository> _mockRepository;
        private readonly Mock<IEventGridPublisherService> _mockEventGridPublisher;
        private readonly Mock<ILogger<SessionsService>> _mockLogger;
        private readonly SessionsService _service;

        public SessionsServiceTests()
        {
            _mockRepository = new Mock<ISessionsRepository>();
            _mockEventGridPublisher = new Mock<IEventGridPublisherService>();
            _mockLogger = new Mock<ILogger<SessionsService>>();
            _service = new SessionsService(_mockRepository.Object, _mockEventGridPublisher.Object, _mockLogger.Object);
        }

        #region AddSessionAsync Tests

        [Fact]
        public async Task AddSessionAsync_ValidSession_ReturnsDocumentId()
        {
            // Arrange
            var title = "Test Session";
            var description = "Test Description";
            var smokerId = "smoker-123";
            var startTime = DateTime.UtcNow;
            var expectedId = "session-id-123";

            _mockRepository
                .Setup(r => r.AddSessionAsync(It.IsAny<SessionDocument>()))
                .ReturnsAsync(expectedId);

            // Act
            var result = await _service.AddSessionAsync(title, description, smokerId, startTime);

            // Assert
            Assert.Equal(expectedId, result);
            
            // Verify repository was called with correct data
            _mockRepository.Verify(r => r.AddSessionAsync(It.Is<SessionDocument>(s =>
                s.Title == title &&
                s.Description == description &&
                s.SmokerId == smokerId &&
                s.StartTime == startTime &&
                s.Type == "session"
            )), Times.Once);
        }

        [Fact]
        public async Task AddSessionAsync_ValidSession_PublishesSessionCreatedEvent()
        {
            // Arrange
            var title = "Test Session";
            var description = "Test Description";
            var smokerId = "smoker-123";
            var startTime = DateTime.UtcNow;
            var sessionId = "session-id-123";

            _mockRepository
                .Setup(r => r.AddSessionAsync(It.IsAny<SessionDocument>()))
                .ReturnsAsync(sessionId);

            // Act
            await _service.AddSessionAsync(title, description, smokerId, startTime);

            // Assert
            _mockEventGridPublisher.Verify(e => e.PostEventGridEventAsync(
                EventTypes.Sessions.SessionCreated,
                $"{smokerId}",
                It.Is<SessionCreatedEventData>(data =>
                    data.Id == sessionId &&
                    data.SmokerId == smokerId &&
                    data.Title == title
                )
            ), Times.Once);
        }

        #endregion

        #region DeleteSessionAsync Tests

        [Fact]
        public async Task DeleteSessionAsync_ValidSession_ReturnsSuccess()
        {
            // Arrange
            var sessionId = "session-123";
            var smokerId = "smoker-123";

            _mockRepository
                .Setup(r => r.DeleteSessionAsync(sessionId, smokerId))
                .ReturnsAsync(DeleteSessionResult.Success);

            // Act
            var result = await _service.DeleteSessionAsync(sessionId, smokerId);

            // Assert
            Assert.Equal(DeleteSessionResult.Success, result);
        }

        [Fact]
        public async Task DeleteSessionAsync_SessionNotFound_ReturnsNotFound()
        {
            // Arrange
            var sessionId = "nonexistent-session";
            var smokerId = "smoker-123";

            _mockRepository
                .Setup(r => r.DeleteSessionAsync(sessionId, smokerId))
                .ReturnsAsync(DeleteSessionResult.NotFound);

            // Act
            var result = await _service.DeleteSessionAsync(sessionId, smokerId);

            // Assert
            Assert.Equal(DeleteSessionResult.NotFound, result);
        }

        [Fact]
        public async Task DeleteSessionAsync_Success_PublishesSessionDeletedEvent()
        {
            // Arrange
            var sessionId = "session-123";
            var smokerId = "smoker-123";

            _mockRepository
                .Setup(r => r.DeleteSessionAsync(sessionId, smokerId))
                .ReturnsAsync(DeleteSessionResult.Success);

            // Act
            await _service.DeleteSessionAsync(sessionId, smokerId);

            // Assert
            _mockEventGridPublisher.Verify(e => e.PostEventGridEventAsync(
                EventTypes.Sessions.SessionDeleted,
                $"{smokerId}",
                It.IsAny<SessionDeletedEventData>()
            ), Times.Once);
        }

        #endregion

        #region UpdateSessionAsync Tests

        [Fact]
        public async Task UpdateSessionAsync_ValidSession_ReturnsSuccess()
        {
            // Arrange
            var sessionId = "session-123";
            var smokerId = "smoker-123";
            var title = "Updated Title";
            var description = "Updated Description";
            var endTime = DateTime.UtcNow.AddHours(2);

            var sessionDocument = new SessionDocument
            {
                Id = sessionId,
                SmokerId = smokerId,
                Title = "Original Title",
                Description = "Original Description"
            };

            _mockRepository
                .Setup(r => r.GetSessionAsync(sessionId, smokerId))
                .ReturnsAsync(sessionDocument);

            _mockRepository
                .Setup(r => r.UpdateSessionAsync(It.IsAny<SessionDocument>()))
                .ReturnsAsync(sessionDocument);

            // Act
            var result = await _service.UpdateSessionAsync(sessionId, smokerId, title, description, endTime);

            // Assert
            Assert.Equal(UpdateSessionResult.Success, result);
        }

        [Fact]
        public async Task UpdateSessionAsync_Success_PublishesSessionUpdatedEvent()
        {
            // Arrange
            var sessionId = "session-123";
            var smokerId = "smoker-123";
            var title = "Updated Title";
            var description = "Updated Description";
            var endTime = DateTime.UtcNow.AddHours(2);

            var sessionDocument = new SessionDocument
            {
                Id = sessionId,
                SmokerId = smokerId,
                Title = "Original Title",
                Description = "Original Description"
            };

            _mockRepository
                .Setup(r => r.GetSessionAsync(sessionId, smokerId))
                .ReturnsAsync(sessionDocument);

            _mockRepository
                .Setup(r => r.UpdateSessionAsync(It.IsAny<SessionDocument>()))
                .ReturnsAsync(sessionDocument);

            // Act
            await _service.UpdateSessionAsync(sessionId, smokerId, title, description, endTime);

            // Assert
            _mockEventGridPublisher.Verify(e => e.PostEventGridEventAsync(
                EventTypes.Sessions.SessionUpdated,
                $"{smokerId}",
                It.Is<SessionUpdatedEventData>(data =>
                    data.Id == sessionId &&
                    data.SmokerId == smokerId &&
                    data.Title == title &&
                    data.Description == description
                )
            ), Times.Once);
        }

        #endregion

        #region EndSessionAsync Tests

        [Fact]
        public async Task EndSessionAsync_ValidSession_ReturnsSuccess()
        {
            // Arrange
            var sessionId = "session-123";
            var smokerId = "smoker-123";
            var endTime = DateTime.UtcNow;

            var sessionDocument = new SessionDocument
            {
                Id = sessionId,
                SmokerId = smokerId,
                Title = "Test Session"
            };

            _mockRepository
                .Setup(r => r.GetSessionAsync(sessionId, smokerId))
                .ReturnsAsync(sessionDocument);

            _mockRepository
                .Setup(r => r.UpdateSessionAsync(It.IsAny<SessionDocument>()))
                .ReturnsAsync(sessionDocument);

            // Act
            var result = await _service.EndSessionAsync(sessionId, smokerId, endTime);

            // Assert
            Assert.Equal(EndSessionResult.Success, result);
        }

        [Fact]
        public async Task EndSessionAsync_Success_PublishesSessionEndedEvent()
        {
            // Arrange
            var sessionId = "session-123";
            var smokerId = "smoker-123";
            var endTime = DateTime.UtcNow;

            var sessionDocument = new SessionDocument
            {
                Id = sessionId,
                SmokerId = smokerId,
                Title = "Test Session"
            };

            _mockRepository
                .Setup(r => r.GetSessionAsync(sessionId, smokerId))
                .ReturnsAsync(sessionDocument);

            _mockRepository
                .Setup(r => r.UpdateSessionAsync(It.IsAny<SessionDocument>()))
                .ReturnsAsync(sessionDocument);

            // Act
            await _service.EndSessionAsync(sessionId, smokerId, endTime);

            // Assert
            _mockEventGridPublisher.Verify(e => e.PostEventGridEventAsync(
                EventTypes.Sessions.SessionEnded,
                $"{smokerId}",
                It.Is<SessionEndedEventData>(data =>
                    data.Id == sessionId &&
                    data.SmokerId == smokerId &&
                    data.EndTime == endTime
                )
            ), Times.Once);
        }

        #endregion

        #region GetSessionAsync Tests

        [Fact]
        public async Task GetSessionAsync_ValidSession_ReturnsSessionDetails()
        {
            // Arrange
            var sessionId = "session-123";
            var smokerId = "smoker-123";
            var sessionDocument = new SessionDocument
            {
                Id = sessionId,
                SmokerId = smokerId,
                Title = "Test Session",
                Description = "Test Description",
                StartTime = DateTime.UtcNow.AddHours(-1),
                EndTime = null
            };

            _mockRepository
                .Setup(r => r.GetSessionAsync(sessionId, smokerId))
                .ReturnsAsync(sessionDocument);

            // Act
            var result = await _service.GetSessionAsync(sessionId, smokerId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sessionId, result.Id);
            Assert.Equal(smokerId, result.SmokerId);
            Assert.Equal("Test Session", result.Title);
            Assert.Equal("Test Description", result.Description);
        }

        [Fact]
        public async Task GetSessionAsync_SessionNotFound_ReturnsNull()
        {
            // Arrange
            var sessionId = "nonexistent-session";
            var smokerId = "smoker-123";

            _mockRepository
                .Setup(r => r.GetSessionAsync(sessionId, smokerId))
                .ReturnsAsync((SessionDocument)null);

            // Act
            var result = await _service.GetSessionAsync(sessionId, smokerId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetSessionsAsync Tests

        [Fact]
        public async Task GetSessionsAsync_ValidSmokerId_ReturnsSessionSummaries()
        {
            // Arrange
            var smokerId = "smoker-123";
            var expectedSummaries = new SessionSummaries();

            _mockRepository
                .Setup(r => r.GetSessionsAsync(smokerId))
                .ReturnsAsync(expectedSummaries);

            // Act
            var result = await _service.GetSessionsAsync(smokerId);

            // Assert
            Assert.NotNull(result);
            Assert.Same(expectedSummaries, result);
        }

        #endregion

        #region Integration with Repository Tests

        [Fact]
        public async Task Service_CallsRepository_WithCorrectParameters()
        {
            // Arrange
            var sessionId = "session-123";
            var smokerId = "smoker-123";

            // Act
            await _service.GetSessionAsync(sessionId, smokerId);

            // Assert
            _mockRepository.Verify(r => r.GetSessionAsync(sessionId, smokerId), Times.Once);
        }

        [Fact]
        public async Task Service_HandlesRepositoryExceptions_Gracefully()
        {
            // Arrange
            var sessionId = "session-123";
            var smokerId = "smoker-123";
            var exception = new InvalidOperationException("Repository error");

            _mockRepository
                .Setup(r => r.GetSessionAsync(sessionId, smokerId))
                .ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.GetSessionAsync(sessionId, smokerId));

            Assert.Same(exception, thrownException);
        }

        #endregion

        #region Business Logic Validation Tests

        [Fact]
        public async Task AddSessionAsync_SetsCorrectDefaults()
        {
            // Arrange
            var title = "Test Session";
            var description = "Test Description";
            var smokerId = "smoker-123";
            var startTime = DateTime.UtcNow;

            _mockRepository
                .Setup(r => r.AddSessionAsync(It.IsAny<SessionDocument>()))
                .ReturnsAsync("session-id");

            // Act
            await _service.AddSessionAsync(title, description, smokerId, startTime);

            // Assert
            _mockRepository.Verify(r => r.AddSessionAsync(It.Is<SessionDocument>(s =>
                s.Type == "session" &&
                s.TTL == -1
            )), Times.Once);
        }

        #endregion
    }
}