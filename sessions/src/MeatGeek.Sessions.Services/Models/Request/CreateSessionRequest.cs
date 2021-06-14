using System;
using Newtonsoft.Json;

namespace MeatGeek.Sessions.Services.Models.Request
{
    public class CreateSessionRequest
    {
        [JsonProperty("smokerId")] 
        public string SmokerId { get; set; }
        [JsonProperty("title")] 
        public string Title { get; set; }
        [JsonProperty("description")] 
        public string Description { get; set; }
        [JsonProperty("startTime")] 
        public DateTime? StartTime { get; set; }
             
    }
}
