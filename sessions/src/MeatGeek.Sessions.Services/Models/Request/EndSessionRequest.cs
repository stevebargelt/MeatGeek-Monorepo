using System;
using Newtonsoft.Json;

namespace MeatGeek.Sessions.Services.Models.Request
{
    public class EndSessionRequest
    {
        [JsonProperty("smokerId")] 
        public string SmokerId { get; set; }
        [JsonProperty("endTime")]
        public DateTime? EndTime { get; set; }
    }
}