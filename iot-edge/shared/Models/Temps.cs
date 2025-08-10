using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace MeatGeek.IoT.Edge.Shared.Models;

/// <summary>
/// Represents temperature readings from BBQ smoker sensors.
/// Supports both System.Text.Json and Newtonsoft.Json serialization.
/// </summary>
public class Temps
{
    [JsonPropertyName("grillTemp")]
    [JsonProperty("grillTemp")]
    public double GrillTemp { get; set; }

    [JsonPropertyName("probe1Temp")]
    [JsonProperty("probe1Temp")]
    public double Probe1Temp { get; set; }

    [JsonPropertyName("probe2Temp")]
    [JsonProperty("probe2Temp")]
    public double Probe2Temp { get; set; }

    [JsonPropertyName("probe3Temp")]
    [JsonProperty("probe3Temp")]
    public double Probe3Temp { get; set; }

    [JsonPropertyName("probe4Temp")]
    [JsonProperty("probe4Temp")]
    public double Probe4Temp { get; set; }
}