using MeatGeek.MockDevice.Services;
using Xunit;

namespace MeatGeek.MockDevice.Tests;

public class TelemetrySimulatorTests
{
    [Fact]
    public void TelemetrySimulator_InitializesWithIdleState()
    {
        // Arrange & Act
        var simulator = new TelemetrySimulator();
        var status = simulator.GetCurrentStatus();

        // Assert
        Assert.False(simulator.IsCooking);
        Assert.Equal("idle", status.Mode);
        Assert.False(status.AugerOn);
        Assert.False(status.BlowerOn);
        Assert.False(status.IgniterOn);
        Assert.Null(status.SessionId);
        Assert.Equal("telemetry", status.Type);
        Assert.Equal(259200, status.Ttl); // 3-day TTL for telemetry
    }

    [Fact]
    public void TelemetrySimulator_StartsWithAmbientTemperature()
    {
        // Arrange & Act
        var simulator = new TelemetrySimulator();
        var status = simulator.GetCurrentStatus();

        // Assert
        Assert.Equal(70.0, status.Temps.GrillTemp); // Default ambient temp
        Assert.Equal(70.0, status.Temps.Probe1Temp);
        Assert.Equal(0.0, status.Temps.Probe2Temp); // Unused probes
        Assert.Equal(0.0, status.Temps.Probe3Temp);
        Assert.Equal(0.0, status.Temps.Probe4Temp);
    }

    [Fact]
    public void StartCooking_TransitionsToStartupMode()
    {
        // Arrange
        var simulator = new TelemetrySimulator();

        // Act
        simulator.StartCooking(CookingScenarios.Brisket);
        var status = simulator.GetCurrentStatus();

        // Assert
        Assert.True(simulator.IsCooking);
        Assert.Equal("startup", status.Mode);
        Assert.True(status.IgniterOn);
        Assert.True(status.AugerOn);
        Assert.Equal("simulation-session", status.SessionId);
        Assert.Equal("status", status.Type);
        Assert.Equal(-1, status.Ttl); // Permanent TTL for session data
        Assert.Equal(225, status.SetPoint); // Brisket target temp
    }

    [Fact]
    public void StopCooking_ReturnsToIdleState()
    {
        // Arrange
        var simulator = new TelemetrySimulator();
        simulator.StartCooking(CookingScenarios.Chicken);

        // Act
        simulator.StopCooking();
        var status = simulator.GetCurrentStatus();

        // Assert
        Assert.False(simulator.IsCooking);
        Assert.Equal("idle", status.Mode);
        Assert.False(status.AugerOn);
        Assert.False(status.BlowerOn);
        Assert.False(status.IgniterOn);
        Assert.Null(status.SessionId);
        Assert.Equal("telemetry", status.Type);
        Assert.Equal(259200, status.Ttl);
    }

    [Fact]
    public void SetTargetTemperature_UpdatesSetPoint()
    {
        // Arrange
        var simulator = new TelemetrySimulator();
        var originalStatus = simulator.GetCurrentStatus();
        var originalSetPoint = originalStatus.SetPoint;

        // Act
        simulator.SetTargetTemperature(300);
        var updatedStatus = simulator.GetCurrentStatus();

        // Assert
        Assert.NotEqual(originalSetPoint, updatedStatus.SetPoint);
        Assert.Equal(300, updatedStatus.SetPoint);
    }

    [Theory]
    [InlineData("brisket", 225, 203)]
    [InlineData("porkshoulder", 250, 195)]
    [InlineData("ribs", 275, 190)]
    [InlineData("chicken", 350, 165)]
    public void CookingScenarios_HaveCorrectTargetTemperatures(string scenarioName, int expectedGrillTemp, int expectedProbeTemp)
    {
        // Arrange
        var scenario = scenarioName.ToLower() switch
        {
            "brisket" => CookingScenarios.Brisket,
            "porkshoulder" => CookingScenarios.PorkShoulder,
            "ribs" => CookingScenarios.Ribs,
            "chicken" => CookingScenarios.Chicken,
            _ => CookingScenarios.Default
        };

        // Assert
        Assert.Equal(expectedGrillTemp, scenario.TargetGrillTemperature);
        Assert.Equal(expectedProbeTemp, scenario.TargetProbeTemperature);
        Assert.True(scenario.GrillHeatingRate > 0);
        Assert.True(scenario.GrillCoolingRate > 0);
        Assert.True(scenario.MeatHeatingRate > 0);
    }

    [Fact]
    public void UpdateSimulation_ActivatesHeatingComponentsWhenBelowTarget()
    {
        // Arrange
        var simulator = new TelemetrySimulator();
        simulator.StartCooking(CookingScenarios.Chicken); // 350°F target, starts at 70°F ambient
        
        var initialStatus = simulator.GetCurrentStatus();
        var targetTemp = initialStatus.SetPoint;
        
        // Act - Call update to activate components based on temperature difference
        simulator.UpdateSimulation();
        var finalStatus = simulator.GetCurrentStatus();

        // Assert - When far below target, heating components should be active
        Assert.True(finalStatus.Temps.GrillTemp < targetTemp - 50, 
            $"Should be far below target for this test: current={finalStatus.Temps.GrillTemp}, target={targetTemp}");
        
        // Should have at least some heating components active when starting up
        var hasHeatingComponents = finalStatus.AugerOn || finalStatus.IgniterOn || finalStatus.BlowerOn;
        Assert.True(hasHeatingComponents, 
            $"At least one heating component should be active when far below target. Auger: {finalStatus.AugerOn}, Igniter: {finalStatus.IgniterOn}, Blower: {finalStatus.BlowerOn}");
        
        // Mode should be startup initially
        Assert.Equal("startup", finalStatus.Mode);
    }

    [Fact]
    public void UpdateSimulation_CoolsToAmbientWhenNotCooking()
    {
        // Arrange
        var simulator = new TelemetrySimulator();
        
        // Start cooking to heat up, then stop
        simulator.StartCooking(CookingScenarios.Default);
        simulator.UpdateSimulation(); // Heat up a bit
        var heatedStatus = simulator.GetCurrentStatus();
        var heatedTemp = heatedStatus.Temps.GrillTemp;
        
        simulator.StopCooking();
        
        // Act - Simulate cooling
        for (int i = 0; i < 5; i++)
        {
            simulator.UpdateSimulation();
            Thread.Sleep(10);
        }
        
        var cooledStatus = simulator.GetCurrentStatus();

        // Assert - Should cool towards ambient (or stay at ambient if already there)
        Assert.True(cooledStatus.Temps.GrillTemp <= heatedTemp + 1, // Allow for small variance
            $"Grill temp should not increase when cooling. Was {heatedTemp}, now {cooledStatus.Temps.GrillTemp}");
    }

    [Fact]
    public void GetCurrentStatus_GeneratesUniqueIds()
    {
        // Arrange
        var simulator = new TelemetrySimulator();

        // Act
        var status1 = simulator.GetCurrentStatus();
        var status2 = simulator.GetCurrentStatus();

        // Assert
        Assert.NotEqual(status1.Id, status2.Id);
        Assert.NotNull(status1.Id);
        Assert.NotNull(status2.Id);
    }

    [Fact]
    public void GetCurrentStatus_IncludesValidTimestamps()
    {
        // Arrange
        var simulator = new TelemetrySimulator();
        var beforeTime = DateTime.UtcNow.AddMinutes(-1);
        var afterTime = DateTime.UtcNow.AddMinutes(1);

        // Act
        var status = simulator.GetCurrentStatus();

        // Assert
        Assert.True(status.CurrentTime > beforeTime && status.CurrentTime < afterTime,
            $"CurrentTime should be recent: {status.CurrentTime}");
        Assert.True(status.ModeTime > beforeTime && status.ModeTime < afterTime,
            $"ModeTime should be recent: {status.ModeTime}");
    }

    [Fact]
    public void TelemetrySimulator_TemperatureStaysWithinReasonableBounds()
    {
        // Arrange
        var simulator = new TelemetrySimulator();
        simulator.StartCooking(CookingScenarios.Brisket);

        // Act - Multiple updates to see temperature progression
        for (int i = 0; i < 20; i++)
        {
            simulator.UpdateSimulation();
            var status = simulator.GetCurrentStatus();

            // Assert - Temperatures should stay within reasonable bounds
            Assert.True(status.Temps.GrillTemp >= 50 && status.Temps.GrillTemp <= 600,
                $"Grill temp out of bounds: {status.Temps.GrillTemp}");
            Assert.True(status.Temps.Probe1Temp >= 50 && status.Temps.Probe1Temp <= 400,
                $"Probe temp out of bounds: {status.Temps.Probe1Temp}");
            
            Thread.Sleep(5); // Small delay
        }
    }
}