using System;
using Newtonsoft.Json;

namespace MeatGeek.IoT.Models
{
    public class SmokerStatus
    {
        [JsonProperty("id")] 
        public string Id { get; set; }
        [JsonProperty] 
        public int? ttl { get; set; }
        [JsonProperty("smokerId")] 
        public string SmokerId { get; set; }
        [JsonProperty("sessionId")] 
        public string SessionId { get; set; }
        [JsonProperty("type")] 
        public string Type { get; set; }
        [JsonProperty("augerOn")] 
        public bool AugerOn { get; set; }
        [JsonProperty("blowerOn")] 
        public bool BlowerOn { get; set; }
        [JsonProperty("igniterOn")] 
        public bool IgniterOn { get; set; }
        [JsonProperty("temps")] 
        public Temps Temps { get; set; }
        [JsonProperty("fireHealthy")] 
        public bool FireHealthy { get; set; }
        [JsonProperty("mode")] 
        public string Mode { get; set; }
        [JsonProperty("setPoint")] 
        public int SetPoint { get; set; }
        [JsonProperty("modeTime")] 
        public DateTime ModeTime { get; set; }
        [JsonProperty("currentTime")] 
        public DateTime CurrentTime { get; set; }
    }
}