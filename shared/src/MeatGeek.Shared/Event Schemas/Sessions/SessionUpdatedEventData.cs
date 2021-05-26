using System;
using Newtonsoft.Json;

namespace MeatGeek.Shared.EventSchemas.Sessions
{
    public class SessionUpdatedEventData
    {
        [JsonProperty("Id")]
        public string Id { get; set; }
        [JsonProperty("SmokerId")]
        public string SmokerId { get; set; }
        [JsonProperty("Title")]
        public string Title { get; set; }
        [JsonProperty("Description")]
        public string Description { get; set; }
        [JsonProperty("EndTime")]
        public DateTime? Endtime { get; set; }
    }
}