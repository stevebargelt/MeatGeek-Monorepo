using System;
using Newtonsoft.Json;

namespace MeatGeek.Shared.EventSchemas.Sessions
{
    public class SessionDeletedEventData
    {
        [JsonProperty("id")] 
        public string Id { get; set; }
        [JsonProperty("smokerId")] 
        public string SmokerId { get; set; }
    }
}
