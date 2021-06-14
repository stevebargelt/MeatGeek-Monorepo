using Newtonsoft.Json;

namespace MeatGeek.Shared.EventSchemas.Sessions
{
    public class SessionCreatedEventData
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("smokerId")]
        public string SmokerId { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
    }
}
