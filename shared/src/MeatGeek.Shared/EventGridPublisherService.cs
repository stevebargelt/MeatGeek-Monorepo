using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;

namespace MeatGeek.Shared
{
    public interface IEventGridPublisherService
    {
        Task PostEventGridEventAsync<T>(string type, string subject, T payload);
    }

    public class EventGridPublisherService : IEventGridPublisherService
    {
        private ILogger<EventGridPublisherService> _log;

        public EventGridPublisherService(ILogger<EventGridPublisherService> logger)
        {
            _log = logger;
        }

        public Task PostEventGridEventAsync<T>(string type, string subject, T payload)
        {
            _log.LogInformation("PostEventGridEventAsync starting");
            // get the connection details for the Event Grid topic
            var topicEndpointUri = new Uri(Environment.GetEnvironmentVariable("EventGridTopicEndpoint"));
            _log.LogInformation("PostEventGridEventAsync: topicEndpointUri =" + topicEndpointUri);
            _log.LogInformation("PostEventGridEventAsync: EventType =" + type);
            _log.LogInformation("PostEventGridEventAsync: Subject =" + subject);
            var topicEndpointHostname = topicEndpointUri.Host;
            var topicKey = Environment.GetEnvironmentVariable("EventGridTopicKey");
            //_log.LogInformation("topicKey =" + topicKey);
            var topicCredentials = new TopicCredentials(topicKey);

            // prepare the events for submission to Event Grid
            var events = new List<Microsoft.Azure.EventGrid.Models.EventGridEvent>
            {
                new Microsoft.Azure.EventGrid.Models.EventGridEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    EventType = type,
                    Subject = subject,
                    EventTime = DateTime.UtcNow,
                    Data = payload,
                    DataVersion = "1"
                }
            };

            // publish the events
            var client = new EventGridClient(topicCredentials);
            return client.PublishEventsWithHttpMessagesAsync(topicEndpointHostname, events);
        }
    }
}
