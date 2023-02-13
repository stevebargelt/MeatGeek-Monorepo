using System;
using Newtonsoft.Json;

namespace MeatGeek.Sessions.Services.Models.Data
{
    public class SessionStatusDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("smokerId")] 
        public string SmokerId { get; set; }
        [JsonProperty("sessionId")] 
        public string SessionId { get; set; }
        [JsonProperty("type")] 
        public string Type 
        { 
            get { return "status"; }  
        }
        [JsonProperty("augerOn")] 
        public string AugerOn { get; set; }
        [JsonProperty("blowerOn")] 
        public string BlowerOn { get; set; }
        [JsonProperty("igniterOn")] 
        public string IgniterOn { get; set; }
        [JsonProperty("temps")]
        public StatusTemps Temps { get; set; }
        [JsonProperty("fireHealthy")] 
        public string FireHealthy { get; set; }
        [JsonProperty("mode")] 
        public string Mode { get; set; }
        [JsonProperty("setPoint")] 
        public string SetPoint { get; set; }
        [JsonProperty("modeTime")] 
        public DateTime ModeTime { get; set; }
        [JsonProperty("currentTime")] 
        public DateTime CurrentTime { get; set; }
        [JsonProperty("_etag")] 
        public string ETag { get; set; }        
    }
}