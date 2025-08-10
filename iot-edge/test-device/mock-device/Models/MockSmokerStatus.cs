using System.Text.Json.Serialization;

namespace MeatGeek.MockDevice.Models;

public class MockSmokerStatus
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("ttl")]
    public int? Ttl { get; set; }

    [JsonPropertyName("smokerId")]
    public string? SmokerId { get; set; }

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("augerOn")]
    public bool AugerOn { get; set; }

    [JsonPropertyName("blowerOn")]
    public bool BlowerOn { get; set; }

    [JsonPropertyName("igniterOn")]
    public bool IgniterOn { get; set; }

    [JsonPropertyName("temps")]
    public MockTemps Temps { get; set; } = new();

    [JsonPropertyName("fireHealthy")]
    public bool FireHealthy { get; set; }

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "idle";

    [JsonPropertyName("setPoint")]
    public int SetPoint { get; set; }

    [JsonPropertyName("modeTime")]
    public DateTime ModeTime { get; set; }

    [JsonPropertyName("currentTime")]
    public DateTime CurrentTime { get; set; }
}

public class MockTemps
{
    [JsonPropertyName("grillTemp")]
    public double GrillTemp { get; set; }

    [JsonPropertyName("probe1Temp")]
    public double Probe1Temp { get; set; }

    [JsonPropertyName("probe2Temp")]
    public double Probe2Temp { get; set; }

    [JsonPropertyName("probe3Temp")]
    public double Probe3Temp { get; set; }

    [JsonPropertyName("probe4Temp")]
    public double Probe4Temp { get; set; }
}

public class MockDeviceResponse
{
    [JsonPropertyName("result")]
    public MockSmokerStatus Result { get; set; } = new();
}