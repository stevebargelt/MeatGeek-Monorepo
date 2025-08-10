using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace MeatGeek.IoT.Edge.Shared.Models;

/// <summary>
/// Represents the complete status of a BBQ smoker device.
/// Supports both System.Text.Json and Newtonsoft.Json serialization.
/// </summary>
public class SmokerStatus
{
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonPropertyName("ttl")]
    [JsonProperty("ttl")]
    public int? Ttl { get; set; }

    [JsonPropertyName("smokerId")]
    [JsonProperty("smokerId")]
    public string? SmokerId { get; set; }

    [JsonPropertyName("sessionId")]
    [JsonProperty("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonPropertyName("augerOn")]
    [JsonProperty("augerOn")]
    public bool AugerOn { get; set; }

    [JsonPropertyName("blowerOn")]
    [JsonProperty("blowerOn")]
    public bool BlowerOn { get; set; }

    [JsonPropertyName("igniterOn")]
    [JsonProperty("igniterOn")]
    public bool IgniterOn { get; set; }

    [JsonPropertyName("temps")]
    [JsonProperty("temps")]
    public Temps Temps { get; set; } = new();

    [JsonPropertyName("fireHealthy")]
    [JsonProperty("fireHealthy")]
    public bool FireHealthy { get; set; }

    [JsonPropertyName("mode")]
    [JsonProperty("mode")]
    public string Mode { get; set; } = "idle";

    [JsonPropertyName("setPoint")]
    [JsonProperty("setPoint")]
    public int SetPoint { get; set; }

    [JsonPropertyName("modeTime")]
    [JsonProperty("modeTime")]
    public DateTime ModeTime { get; set; }

    [JsonPropertyName("currentTime")]
    [JsonProperty("currentTime")]
    public DateTime CurrentTime { get; set; }
}