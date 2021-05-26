using Newtonsoft.Json;

namespace MeatGeek.Shared.EventSchemas.Sessions
{
    public class SessionCreatedEventData
    {
        [JsonProperty("Id")]
        public string Id { get; set; }
        [JsonProperty("SmokerId")]
        public string SmokerId { get; set; }
        [JsonProperty("Title")]
        public string Title { get; set; }
    }
}
