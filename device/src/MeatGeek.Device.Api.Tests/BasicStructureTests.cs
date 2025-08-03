using System;
using System.Reflection;
using Xunit;
using FluentAssertions;
using Inferno.Functions;

namespace MeatGeek.Device.Api.Tests
{
    public class BasicStructureTests
    {
        [Fact]
        public void DeviceApi_ShouldHaveExpectedFunctions()
        {
            // Arrange
            var assembly = typeof(HealthCheck).Assembly;

            // Act & Assert
            var healthCheckType = assembly.GetType("Inferno.Functions.HealthCheck");
            var iotGetStatusType = assembly.GetType("Inferno.Functions.IoTGetStatus");
            var iotGetTempsType = assembly.GetType("Inferno.Functions.IoTGetTemps");
            var iotSetModeType = assembly.GetType("Inferno.Functions.IoTSetMode");
            var iotSetPointType = assembly.GetType("Inferno.Functions.IoTSetPoint");
            var telemetryIntervalType = assembly.GetType("Inferno.Functions.TelemetryInterval");

            healthCheckType.Should().NotBeNull();
            iotGetStatusType.Should().NotBeNull();
            iotGetTempsType.Should().NotBeNull();
            iotSetModeType.Should().NotBeNull();
            iotSetPointType.Should().NotBeNull();
            telemetryIntervalType.Should().NotBeNull();
        }

        [Fact]
        public void AllFunctionClasses_ShouldBeStatic()
        {
            // Arrange
            var assembly = typeof(HealthCheck).Assembly;
            var functionTypes = new[]
            {
                assembly.GetType("Inferno.Functions.HealthCheck"),
                assembly.GetType("Inferno.Functions.IoTGetStatus"),
                assembly.GetType("Inferno.Functions.IoTGetTemps"),
                assembly.GetType("Inferno.Functions.IoTSetMode"),
                assembly.GetType("Inferno.Functions.IoTSetPoint"),
                assembly.GetType("Inferno.Functions.TelemetryInterval")
            };

            // Act & Assert
            foreach (var type in functionTypes)
            {
                if (type != null)
                {
                    type.Should().BeStatic($"Function class {type.Name} should be static");
                }
            }
        }

        [Fact]
        public void DeviceApi_Assembly_ShouldTargetCorrectFramework()
        {
            // Arrange
            var assembly = typeof(HealthCheck).Assembly;

            // Act
            var targetFramework = assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();

            // Assert
            targetFramework.Should().NotBeNull();
            targetFramework!.FrameworkName.Should().StartWith(".NETCoreApp,Version=v8.0");
        }
    }
}