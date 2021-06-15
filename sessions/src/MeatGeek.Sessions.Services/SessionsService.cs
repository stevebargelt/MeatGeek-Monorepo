using System;
using System.Linq;
using System.Threading.Tasks;
using MeatGeek.Sessions.Services.Models;
using MeatGeek.Sessions.Services.Models.Data;
using MeatGeek.Sessions.Services.Models.Response;
using MeatGeek.Sessions.Services.Models.Results;
using MeatGeek.Sessions.Services.Repositories;
using MeatGeek.Shared;
using MeatGeek.Shared.EventSchemas.Sessions;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Extensions;
using Microsoft.Extensions.Http;

namespace MeatGeek.Sessions.Services
{
    public interface ISessionsService
    {
        Task<string> AddSessionAsync(string title, string description, string smokerId, DateTime startTime);
        Task<DeleteSessionResult> DeleteSessionAsync(string SessionId, string smokerId);
        Task<UpdateSessionResult> UpdateSessionAsync(string SessionId, string smokerId, string title, string description, DateTime? endTime);
        Task<UpdateSessionResult> EndSessionAsync(string SessionId, string smokerId, DateTime? endTime);
        Task<SessionDetails> GetSessionAsync(string SessionId, string smokerId);
        Task<SessionSummaries> GetSessionsAsync(string smokerId);
    }

    
    public class SessionsService : ISessionsService
    {
        private ILogger<SessionsService> _log;
        protected ISessionsRepository _sessionsRepository;
        protected IEventGridPublisherService _eventGridPublisher;


        public SessionsService(ISessionsRepository sessionsRepository, IEventGridPublisherService eventGridPublisher, ILogger<SessionsService> logger)
        {
            _sessionsRepository = sessionsRepository;
            _eventGridPublisher = eventGridPublisher;
            _log = logger;
        }

        public async Task<string> AddSessionAsync(string title, string description, string smokerId, DateTime startTime)
        {
            // create the document in Cosmos DB
            var SessionDocument = new SessionDocument
            {
                Title = title,
                Description = description,
                SmokerId = smokerId,
                Type = "session",
                StartTime = startTime
            };
            var SessionId = await _sessionsRepository.AddSessionAsync(SessionDocument);
            _log.LogInformation("SessionId = " + SessionId);
            
            // post a SessionCreated event to Event Grid
            var eventData = new SessionCreatedEventData
            {
                Id = SessionId,
                SmokerId = smokerId,
                Title = title
            };
            var subject = $"{smokerId}";
            _log.LogInformation("subject = " + subject);

            try 
            {
                await _eventGridPublisher.PostEventGridEventAsync(EventTypes.Sessions.SessionCreated, subject, eventData);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "<-- Exception from SessionsServiceDI");
            }

            return SessionId;
        }

        public async Task<DeleteSessionResult> DeleteSessionAsync(string sessionId, string smokerId)
        {
            // delete the document from Cosmos DB
            var result = await _sessionsRepository.DeleteSessionAsync(sessionId, smokerId);
            if (result == DeleteSessionResult.NotFound)
            {
                return DeleteSessionResult.NotFound;
            }

            // post a SessionDeleted event to Event Grid
            var subject = $"{smokerId}";
            await _eventGridPublisher.PostEventGridEventAsync(EventTypes.Sessions.SessionDeleted, subject, new SessionDeletedEventData());

            return DeleteSessionResult.Success;
        }

        public async Task<UpdateSessionResult> UpdateSessionAsync(string sessionId, string smokerId, string title, string description, DateTime? endTime)
        {
            _log.LogInformation($"SessionsService: UpdateSessionAsync Called");
            // get the current version of the document from Cosmos DB
            var SessionDocument = await _sessionsRepository.GetSessionAsync(sessionId, smokerId);
            _log.LogInformation($"SessionsService: AFTER GetSessionAsync ");
            var eventData = new SessionUpdatedEventData{};
            eventData.Id = sessionId;
            eventData.SmokerId = smokerId;

            if (SessionDocument == null)
            {
                return UpdateSessionResult.NotFound;
            }

            if (!string.IsNullOrEmpty(title))
            {
                SessionDocument.Title = title;
                eventData.Title = title;
            }
            if (!string.IsNullOrEmpty(description))
            {
                SessionDocument.Description = description;
                eventData.Description = description;
            }
            if (endTime.HasValue)
            {
                SessionDocument.EndTime = endTime;
                eventData.Endtime = endTime;
            }
            
            await _sessionsRepository.UpdateSessionAsync(SessionDocument);

            // post a SessionNameUpdated event to Event Grid
            var subject = $"{smokerId}";
            await _eventGridPublisher.PostEventGridEventAsync(EventTypes.Sessions.SessionUpdated, subject, eventData);

            return UpdateSessionResult.Success;
        }

        public async Task<UpdateSessionResult> EndSessionAsync(string sessionId, string smokerId, DateTime? endTime)
        {
            _log.LogInformation($"SessionsService: EndSessionAsync Called");
            // get the current version of the document from Cosmos DB
            var SessionDocument = await _sessionsRepository.GetSessionAsync(sessionId, smokerId);
            _log.LogInformation($"SessionsService: EndSessionAsync AFTER GetSessionAsync ");
            var eventData = new SessionUpdatedEventData{};
            eventData.Id = sessionId;
            eventData.SmokerId = smokerId;

            if (SessionDocument == null)
            {
                return UpdateSessionResult.NotFound;
            }
            if (endTime.HasValue)
            {
                SessionDocument.EndTime = endTime;
                eventData.Endtime = endTime;
            }
            else 
            {
                //TODO: Update this to incorrect parameter
                return UpdateSessionResult.NotFound;
            }
            
            await _sessionsRepository.UpdateSessionAsync(SessionDocument);

            // post a SessionNameUpdated event to Event Grid
            var subject = $"{smokerId}";
            await _eventGridPublisher.PostEventGridEventAsync(EventTypes.Sessions.SessionUpdated, subject, eventData);

            return UpdateSessionResult.Success;
        }


        public async Task<SessionDetails> GetSessionAsync(string sessionId, string smokerId)
        {
            var SessionDocument = await _sessionsRepository.GetSessionAsync(sessionId, smokerId);
            if (SessionDocument == null)
            {
                return null;
            }

            return new SessionDetails
            {
                Id = SessionDocument.Id,
                SmokerId = SessionDocument.SmokerId,
                Title = SessionDocument.Title,
                Description = SessionDocument.Description,
                StartTime = SessionDocument.StartTime,
                EndTime = SessionDocument.EndTime,
            };
        }

        
        public Task<SessionSummaries> GetSessionsAsync(string smokerId)
        {
            return _sessionsRepository.GetSessionsAsync(smokerId);
        }

    }
}
