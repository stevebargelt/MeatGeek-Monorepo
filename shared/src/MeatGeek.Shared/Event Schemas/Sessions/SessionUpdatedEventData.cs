using System;
using Newtonsoft.Json;

namespace MeatGeek.Shared.EventSchemas.Sessions
{
    public class SessionUpdatedEventData
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("smokerId")]
        public string SmokerId { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("endTime")]
        public DateTime? Endtime { get; set; }
    }
}