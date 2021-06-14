using System;
using Newtonsoft.Json;

namespace MeatGeek.Sessions.Services.Models.Data
{
    public class SessionDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("smokerId")] 
        public string SmokerId { get; set; }
        [JsonProperty("_etag")] 
        public string ETag { get; set; }        
        [JsonProperty("title")] 
        public string Title { get; set; }
        [JsonProperty("type")] 
        public string Type { get; set; }
        [JsonProperty("description")] 
        public string Description { get; set; }
        [JsonProperty("startTime")] 
        public DateTime? StartTime { get; set; }
        [JsonProperty("endTime")]
        public DateTime? EndTime { get; set; }
        [JsonProperty("timeStamp")]
        public DateTime TimeStamp { get; set; }
    }
}