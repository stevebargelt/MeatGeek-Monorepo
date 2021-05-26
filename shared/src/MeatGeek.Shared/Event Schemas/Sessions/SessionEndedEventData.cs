using System;

namespace MeatGeek.Shared.EventSchemas.Sessions
{
    public class SessionEndedEventData
    {
        public string Id { get; set; }
        public string SmokerId { get; set; }
        public DateTime EndTime { get; set; }
    }
}