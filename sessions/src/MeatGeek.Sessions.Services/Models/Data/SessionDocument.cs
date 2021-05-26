using System;
using Newtonsoft.Json;

namespace MeatGeek.Sessions.Services.Models.Data
{
    public class SessionDocument
    {
        [JsonProperty("id")] 
        public string Id { get; set; }
        [JsonProperty("smokerid")] 
        public string SmokerId { get; set; }
        [JsonProperty("_etag")] 
        public string ETag { get; set; }        
        [JsonProperty("title")] 
        public string Title { get; set; }
        [JsonProperty("description")] 
        public string Description { get; set; }
        [JsonProperty("starttime")] 
        public DateTime? StartTime { get; set; }
        [JsonProperty("endtime")]
        public DateTime? EndTime { get; set; }
        [JsonProperty("timestamp")]
        public DateTime TimeStamp { get; set; }
    }
}