using System;
using Newtonsoft.Json;

namespace MeatGeek.Sessions.Services.Models.Response
{
    public class SessionSummary
    {
        [JsonProperty("id")] 
        public string Id { get; set; }
        [JsonProperty("smokerId")] 
        public string SmokerId { get; set; }
        [JsonProperty("type")] 
        public string Type { 
            get { return "session"; }
        }        
        [JsonProperty("title")] 
        public string Title { get; set; }
        [JsonProperty("endTime")]
        public DateTime? EndTime { get; set; }       
    }
}