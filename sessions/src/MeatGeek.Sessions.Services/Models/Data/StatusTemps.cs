using System;
using Newtonsoft.Json;

namespace MeatGeek.Sessions.Services.Models.Data
{
    public class StatusTemps
    {
        [JsonProperty("grillTemp")]
        public string GrillTemp { get; set; }
        [JsonProperty("probe1Temp")] 
        public string Probe1Temp { get; set; }
        [JsonProperty("probe2Temp")] 
        public string Probe2Temp { get; set; }
        [JsonProperty("probe3Temp")] 
        public string Probe3Temp { get; set; }
        [JsonProperty("probe4Temp")] 
        public string Probe4Temp { get; set; }
    }
}