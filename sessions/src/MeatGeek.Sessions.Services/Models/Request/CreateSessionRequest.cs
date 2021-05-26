using System;
using Newtonsoft.Json;

namespace MeatGeek.Sessions.Services.Models.Request
{
    public class CreateSessionRequest
    {
        [JsonProperty("smokerid")] 
        public string SmokerId { get; set; }
        [JsonProperty("title")] 
        public string Title { get; set; }
        [JsonProperty("description")] 
        public string Description { get; set; }
        [JsonProperty("starttime")] 
        public DateTime? StartTime { get; set; }
             
    }
}
