using System;
using Newtonsoft.Json;

namespace MeatGeek.Sessions.Services.Models.Response
{
    public class SessionDetails
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
        [JsonProperty("description")] 
        public string Description { get; set; }
        [JsonProperty("startTime")] 
        public DateTime? StartTime { get; set; }
        [JsonProperty("endTime")]
        public DateTime? EndTime { get; set; }
        [JsonProperty("timestamp")]
        public DateTime TimeStamp { get; set; }    
    }
}