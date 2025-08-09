using System.Text.Json;
using MeatGeek.MockDevice.Models;
using Xunit;

namespace MeatGeek.MockDevice.Tests;

public class MockDeviceModelTests
{
    [Fact]
    public void MockSmokerStatus_SerializesToJson()
    {
        // Arrange
        var status = new MockSmokerStatus
        {
            AugerOn = true,
            BlowerOn = false,
            IgniterOn = false,
            Temps = new MockTemps
            {
                GrillTemp = 225.5,
                Probe1Temp = 165.2,
                Probe2Temp = 0.0,
                Probe3Temp = 0.0,
                Probe4Temp = 0.0
            },
            FireHealthy = true,
            Mode = "cooking",
            SetPoint = 225,
            ModeTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            CurrentTime = new DateTime(2024, 1, 1, 14, 30, 0, DateTimeKind.Utc)
        };

        // Act
        var json = JsonSerializer.Serialize(status);
        var deserialized = JsonSerializer.Deserialize<MockSmokerStatus>(json);

        // Assert
        Assert.NotNull(json);
        Assert.NotNull(deserialized);
        Assert.Equal(status.Mode, deserialized.Mode);
        Assert.Equal(status.SetPoint, deserialized.SetPoint);
        Assert.Equal(status.AugerOn, deserialized.AugerOn);
        Assert.Equal(status.Temps.GrillTemp, deserialized.Temps.GrillTemp);
    }

    [Fact]
    public void MockDeviceResponse_HasCorrectStructure()
    {
        // Arrange
        var response = new MockDeviceResponse
        {
            Result = new MockSmokerStatus
            {
                Mode = "idle",
                SetPoint = 0,
                Temps = new MockTemps()
            }
        };

        // Act
        var json = JsonSerializer.Serialize(response);

        // Assert
        Assert.Contains("result", json);
        Assert.Contains("mode", json);
        Assert.Contains("temps", json);
    }

    [Fact]
    public void MockTemps_AllProbesInitializeToZero()
    {
        // Arrange & Act
        var temps = new MockTemps();

        // Assert
        Assert.Equal(0.0, temps.GrillTemp);
        Assert.Equal(0.0, temps.Probe1Temp);
        Assert.Equal(0.0, temps.Probe2Temp);
        Assert.Equal(0.0, temps.Probe3Temp);
        Assert.Equal(0.0, temps.Probe4Temp);
    }
}