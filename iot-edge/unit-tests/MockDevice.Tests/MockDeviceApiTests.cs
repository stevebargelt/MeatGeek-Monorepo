using Microsoft.AspNetCore.Mvc.Testing;
using System.Diagnostics;
using System.Text.Json;
using MeatGeek.MockDevice.Models;
using Xunit;

namespace MeatGeek.MockDevice.Tests;

public class MockDeviceApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MockDeviceApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetStatusReturnsValidJsonStructure()
    {
        // Act
        var response = await _client.GetAsync("/api/robots/MeatGeekBot/commands/get_status");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify it's valid JSON and can be deserialized
        var deviceResponse = JsonSerializer.Deserialize<MockDeviceResponse>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        Assert.NotNull(deviceResponse);
        Assert.NotNull(deviceResponse.Result);
    }

    [Fact]
    public async Task GetStatusIncludesAllRequiredFields()
    {
        // Act
        var response = await _client.GetAsync("/api/robots/MeatGeekBot/commands/get_status");
        var content = await response.Content.ReadAsStringAsync();
        var deviceResponse = JsonSerializer.Deserialize<MockDeviceResponse>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Assert - Verify all required fields are present and have valid values
        var status = deviceResponse!.Result;
        
        // Core identity fields
        Assert.NotNull(status.Id);
        Assert.NotNull(status.SmokerId);
        Assert.NotNull(status.Type);
        Assert.True(status.Ttl.HasValue);
        
        // Session field can be null when no active session
        // Assert.NotNull(status.SessionId) - not required, can be null
        
        // Operating fields
        Assert.NotNull(status.Mode);
        Assert.True(status.SetPoint >= 0);
        Assert.NotEqual(DateTime.MinValue, status.ModeTime);
        Assert.NotEqual(DateTime.MinValue, status.CurrentTime);
        
        // Temperature fields
        Assert.NotNull(status.Temps);
        Assert.True(status.Temps.GrillTemp >= 0);
        Assert.True(status.Temps.Probe1Temp >= 0);
        Assert.True(status.Temps.Probe2Temp >= 0);
        Assert.True(status.Temps.Probe3Temp >= 0);
        Assert.True(status.Temps.Probe4Temp >= 0);
    }

    [Fact]
    public async Task GetStatusRespondsWithin500ms()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();
        
        // Act
        var response = await _client.GetAsync("/api/robots/MeatGeekBot/commands/get_status");
        stopwatch.Stop();
        
        // Assert
        response.EnsureSuccessStatusCode();
        Assert.True(stopwatch.ElapsedMilliseconds < 500, 
            $"Response time {stopwatch.ElapsedMilliseconds}ms exceeded 500ms threshold");
    }

    [Fact]
    public async Task HealthCheckEndpointWorks()
    {
        // Act
        var response = await _client.GetAsync("/health");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        Assert.Contains("healthy", content);
        Assert.Contains("timestamp", content);
    }

    [Fact]
    public async Task GetStatusReturnsCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/robots/MeatGeekBot/commands/get_status");
        
        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task GetStatusReturnsTelemetryTypeWithCorrectTtl()
    {
        // Act
        var response = await _client.GetAsync("/api/robots/MeatGeekBot/commands/get_status");
        var content = await response.Content.ReadAsStringAsync();
        var deviceResponse = JsonSerializer.Deserialize<MockDeviceResponse>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        // Assert - When no session, should be telemetry type with 3-day TTL or -1 for session
        var status = deviceResponse!.Result;
        Assert.NotNull(status.Type);
        
        // Since we're returning -1 in the mock for now (simulating session data),
        // verify the TTL is set correctly
        Assert.True(status.Ttl.HasValue);
        Assert.True(status.Ttl == -1 || status.Ttl == 259200, // -1 for session, 259200 (3 days) for telemetry
            $"TTL should be -1 (session) or 259200 (3-day telemetry), but was {status.Ttl}");
    }

    [Fact]
    public async Task GetStatusReturnsConsistentStructure()
    {
        // Act - Make multiple calls to ensure consistent structure
        var response1 = await _client.GetAsync("/api/robots/MeatGeekBot/commands/get_status");
        var response2 = await _client.GetAsync("/api/robots/MeatGeekBot/commands/get_status");
        
        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();
        
        var status1 = JsonSerializer.Deserialize<MockDeviceResponse>(content1, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var status2 = JsonSerializer.Deserialize<MockDeviceResponse>(content2, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert - Structure should be identical (though timestamps may differ)
        Assert.Equal(status1!.Result.Mode, status2!.Result.Mode);
        Assert.Equal(status1.Result.SetPoint, status2.Result.SetPoint);
        Assert.Equal(status1.Result.AugerOn, status2.Result.AugerOn);
        Assert.Equal(status1.Result.BlowerOn, status2.Result.BlowerOn);
        Assert.Equal(status1.Result.IgniterOn, status2.Result.IgniterOn);
        Assert.Equal(status1.Result.FireHealthy, status2.Result.FireHealthy);
    }
}