using System;
using Newtonsoft.Json;

namespace MeatGeek.Shared.EventSchemas.Sessions
{
    public class SessionEndedEventData
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("smokerId")]
        public string SmokerId { get; set; }
        [JsonProperty("endTime")]
        public DateTime? EndTime { get; set; }
    }
}