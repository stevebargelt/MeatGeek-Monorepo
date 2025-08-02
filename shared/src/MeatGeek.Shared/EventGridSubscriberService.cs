using System;
using MeatGeek.Shared.EventSchemas.Sessions;
using Newtonsoft.Json.Linq;

namespace MeatGeek.Shared
{
    public interface IEventGridSubscriberService
    {
        (string smokerId, string sessionId) DeconstructEventGridMessage(EventGridEvent eventGridEvent);
    }

    public class EventGridSubscriberService : IEventGridSubscriberService
    {
        internal const string EventGridSubscriptionValidationHeaderKey = "Aeg-Event-Type";

        public (string smokerId, string sessionId) DeconstructEventGridMessage(EventGridEvent eventGridEvent)
        {

            // find the SessionID and SmokerID from the subject
            var eventGridEventSubjectComponents = eventGridEvent.Subject.Split('/');
            if (eventGridEventSubjectComponents.Length != 2)
            {
                throw new InvalidOperationException("Event Grid event subject is not in expected format.");
            }
            var smokerId = eventGridEventSubjectComponents[0];
            var sessionId = eventGridEventSubjectComponents[1];

            return (smokerId, sessionId);
        }

        private object CreateStronglyTypedDataObject(object data, string eventType)
        {
            switch (eventType)
            {
                // creates

                case EventTypes.Sessions.SessionCreated:
                    return ConvertDataObjectToType<SessionCreatedEventData>(data);

                // updates

                case EventTypes.Sessions.SessionUpdated:
                    return ConvertDataObjectToType<SessionUpdatedEventData>(data);

                // deletes

                case EventTypes.Sessions.SessionDeleted:
                    return ConvertDataObjectToType<SessionDeletedEventData>(data);

                default:
                    throw new ArgumentException($"Unexpected event type '{eventType}' in {nameof(CreateStronglyTypedDataObject)}");
            }
        }

        private T ConvertDataObjectToType<T>(object dataObject)
        {
            if (dataObject is JObject o)
            {
                return o.ToObject<T>();
            }

            return (T)dataObject;
        }
    }
}
