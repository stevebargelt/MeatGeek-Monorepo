using System;
using Newtonsoft.Json;

namespace MeatGeek.Sessions.Services.Models.Request
{
    public class UpdateSessionRequest
    {
        [JsonProperty("smokerid")] 
        public string SmokerId { get; set; }
        [JsonProperty("title")] 
        public string Title { get; set; }
        [JsonProperty("description")] 
        public string Description { get; set; }
        [JsonProperty("endtime")]
        public DateTime? EndTime { get; set; }
    }
}