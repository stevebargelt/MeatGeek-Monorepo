using System;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MeatGeek.Sessions.WorkerApi.Models
{
    public class SmokerStatus
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty] public int? ttl { get; set; }
        [JsonProperty] public string SmokerId { get; set; }
        [JsonProperty] public string SessionId { get; set; }
        [JsonProperty] public string Type { get; set; }
        [JsonProperty] public bool AugerOn { get; set; }
        [JsonProperty] public bool BlowerOn { get; set; }
        [JsonProperty] public bool IgniterOn { get; set; }
        [JsonProperty] public Temps Temps { get; set; }
        [JsonProperty] public bool FireHealthy { get; set; }
        [JsonProperty] public string Mode { get; set; }
        [JsonProperty] public int SetPoint { get; set; }
        [JsonProperty] public DateTime ModeTime { get; set; }
        [JsonProperty] public DateTime CurrentTime { get; set; }
    }
}