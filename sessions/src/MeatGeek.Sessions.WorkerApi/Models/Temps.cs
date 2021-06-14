using System;
using Newtonsoft.Json;

namespace MeatGeek.Sessions.WorkerApi.Models
{
    public class Temps
    {
        [JsonProperty("grillTemp")] 
        public double GrillTemp { get; set; }
        [JsonProperty("probe1Temp")] 
        public double Probe1Temp { get; set; }
        [JsonProperty("probe2Temp")] 
        public double Probe2Temp { get; set; }
        [JsonProperty("probe3Temp")] 
        public double Probe3Temp { get; set; }
        [JsonProperty("probe4Temp")] 
        public double Probe4Temp { get; set; }

    }
}