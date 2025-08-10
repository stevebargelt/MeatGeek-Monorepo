using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace MeatGeek.IoT.Edge.Shared.Models;

/// <summary>
/// Generic device response wrapper for API responses.
/// Supports both System.Text.Json and Newtonsoft.Json serialization.
/// </summary>
/// <typeparam name="T">The type of the result data</typeparam>
public class DeviceResponse<T>
{
    [JsonPropertyName("result")]
    [JsonProperty("result")]
    public T Result { get; set; } = default!;
}

/// <summary>
/// Device response specifically for SmokerStatus results.
/// </summary>
public class DeviceResponse : DeviceResponse<SmokerStatus>
{
}