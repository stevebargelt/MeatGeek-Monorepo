using System;
using Xunit;
using Newtonsoft.Json;
using MeatGeek.Sessions.WorkerApi.Models;

namespace MeatGeek.Sessions.WorkerApi.Tests.Models
{
    public class TempsTests
    {
        #region JSON Serialization Tests

        [Fact]
        public void Temps_SerializeToJson_ProducesCorrectFormat()
        {
            // Arrange
            var temps = new Temps
            {
                GrillTemp = 225.5,
                Probe1Temp = 165.0,
                Probe2Temp = 170.0,
                Probe3Temp = 180.0,
                Probe4Temp = 0.0
            };

            // Act
            var json = JsonConvert.SerializeObject(temps, Formatting.Indented);

            // Assert
            Assert.Contains("\"grillTemp\": 225.5", json);
            Assert.Contains("\"probe1Temp\": 165.0", json);
            Assert.Contains("\"probe2Temp\": 170.0", json);
            Assert.Contains("\"probe3Temp\": 180.0", json);
            Assert.Contains("\"probe4Temp\": 0.0", json);
        }

        [Fact]
        public void Temps_DeserializeFromJson_RestoresCorrectValues()
        {
            // Arrange
            var json = @"{
                ""grillTemp"": 225.5,
                ""probe1Temp"": 165.0,
                ""probe2Temp"": 170.0,
                ""probe3Temp"": 180.0,
                ""probe4Temp"": 0.0
            }";

            // Act
            var temps = JsonConvert.DeserializeObject<Temps>(json);

            // Assert
            Assert.Equal(225.5, temps.GrillTemp);
            Assert.Equal(165.0, temps.Probe1Temp);
            Assert.Equal(170.0, temps.Probe2Temp);
            Assert.Equal(180.0, temps.Probe3Temp);
            Assert.Equal(0.0, temps.Probe4Temp);
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Temps_DefaultValues_AreZero()
        {
            // Arrange & Act
            var temps = new Temps();

            // Assert
            Assert.Equal(0.0, temps.GrillTemp);
            Assert.Equal(0.0, temps.Probe1Temp);
            Assert.Equal(0.0, temps.Probe2Temp);
            Assert.Equal(0.0, temps.Probe3Temp);
            Assert.Equal(0.0, temps.Probe4Temp);
        }

        [Fact]
        public void Temps_SettingAllProperties_WorksCorrectly()
        {
            // Arrange & Act
            var temps = new Temps
            {
                GrillTemp = 250.75,
                Probe1Temp = 160.25,
                Probe2Temp = 170.50,
                Probe3Temp = 180.00,
                Probe4Temp = 155.75
            };

            // Assert
            Assert.Equal(250.75, temps.GrillTemp);
            Assert.Equal(160.25, temps.Probe1Temp);
            Assert.Equal(170.50, temps.Probe2Temp);
            Assert.Equal(180.00, temps.Probe3Temp);
            Assert.Equal(155.75, temps.Probe4Temp);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Temps_NegativeTemperatures_HandledCorrectly()
        {
            // Arrange & Act
            var temps = new Temps
            {
                GrillTemp = -10.5,
                Probe1Temp = -5.0,
                Probe2Temp = 0.0,
                Probe3Temp = 32.0,
                Probe4Temp = -273.15 // Absolute zero
            };

            // Act
            var json = JsonConvert.SerializeObject(temps);
            var deserialized = JsonConvert.DeserializeObject<Temps>(json);

            // Assert
            Assert.Equal(-10.5, deserialized.GrillTemp);
            Assert.Equal(-5.0, deserialized.Probe1Temp);
            Assert.Equal(0.0, deserialized.Probe2Temp);
            Assert.Equal(32.0, deserialized.Probe3Temp);
            Assert.Equal(-273.15, deserialized.Probe4Temp);
        }

        [Fact]
        public void Temps_HighTemperatures_HandledCorrectly()
        {
            // Arrange & Act
            var temps = new Temps
            {
                GrillTemp = 1000.0,  // Very high grill temp
                Probe1Temp = 500.0,  // Very high probe temp
                Probe2Temp = 999.99,
                Probe3Temp = double.MaxValue,
                Probe4Temp = 212.0   // Boiling point of water
            };

            // Act
            var json = JsonConvert.SerializeObject(temps);
            var deserialized = JsonConvert.DeserializeObject<Temps>(json);

            // Assert
            Assert.Equal(1000.0, deserialized.GrillTemp);
            Assert.Equal(500.0, deserialized.Probe1Temp);
            Assert.Equal(999.99, deserialized.Probe2Temp);
            Assert.Equal(double.MaxValue, deserialized.Probe3Temp);
            Assert.Equal(212.0, deserialized.Probe4Temp);
        }

        [Fact]
        public void Temps_PrecisionValues_PreservedInSerialization()
        {
            // Arrange
            var temps = new Temps
            {
                GrillTemp = 225.123456789,
                Probe1Temp = 165.987654321,
                Probe2Temp = 0.000000001,
                Probe3Temp = 999.999999999,
                Probe4Temp = 123.456789123
            };

            // Act
            var json = JsonConvert.SerializeObject(temps);
            var deserialized = JsonConvert.DeserializeObject<Temps>(json);

            // Assert - Note: JSON serialization may have some precision limits
            Assert.Equal(225.123456789, deserialized.GrillTemp, 10); // 10 decimal places precision
            Assert.Equal(165.987654321, deserialized.Probe1Temp, 10);
            Assert.Equal(0.000000001, deserialized.Probe2Temp, 15);
            Assert.Equal(999.999999999, deserialized.Probe3Temp, 10);
            Assert.Equal(123.456789123, deserialized.Probe4Temp, 10);
        }

        [Fact]
        public void Temps_SpecialDoubleValues_HandledCorrectly()
        {
            // Arrange
            var temps = new Temps
            {
                GrillTemp = double.NaN,
                Probe1Temp = double.PositiveInfinity,
                Probe2Temp = double.NegativeInfinity,
                Probe3Temp = double.Epsilon,
                Probe4Temp = 0.0
            };

            // Act & Assert
            // JSON serialization should handle these special values
            var json = JsonConvert.SerializeObject(temps);
            Assert.NotNull(json);
            
            // Note: NaN and Infinity values in JSON depend on JsonSerializerSettings
            // The default behavior may convert them to null or string representations
        }

        #endregion

        #region Realistic Scenario Tests

        [Fact]
        public void Temps_TypicalBBQScenario_ReflectsRealUsage()
        {
            // Arrange - Typical BBQ scenario with brisket and ribs
            var temps = new Temps
            {
                GrillTemp = 225.0,      // Low and slow BBQ temperature
                Probe1Temp = 165.0,     // Brisket flat internal temp
                Probe2Temp = 195.0,     // Brisket point internal temp
                Probe3Temp = 203.0,     // Pork ribs internal temp
                Probe4Temp = 0.0        // Unused probe
            };

            // Act
            var json = JsonConvert.SerializeObject(temps);
            var deserialized = JsonConvert.DeserializeObject<Temps>(json);

            // Assert
            Assert.Equal(225.0, deserialized.GrillTemp);
            Assert.Equal(165.0, deserialized.Probe1Temp);
            Assert.Equal(195.0, deserialized.Probe2Temp);
            Assert.Equal(203.0, deserialized.Probe3Temp);
            Assert.Equal(0.0, deserialized.Probe4Temp);
        }

        [Fact]
        public void Temps_UnusedProbesScenario_OnlyGrillAndOneProbe()
        {
            // Arrange - Common scenario where only grill and one probe are used
            var temps = new Temps
            {
                GrillTemp = 350.0,      // Higher temp for chicken
                Probe1Temp = 165.0,     // Chicken breast internal temp
                Probe2Temp = 0.0,       // Unused
                Probe3Temp = 0.0,       // Unused
                Probe4Temp = 0.0        // Unused
            };

            // Act
            var json = JsonConvert.SerializeObject(temps);
            var deserialized = JsonConvert.DeserializeObject<Temps>(json);

            // Assert
            Assert.Equal(350.0, deserialized.GrillTemp);
            Assert.Equal(165.0, deserialized.Probe1Temp);
            Assert.Equal(0.0, deserialized.Probe2Temp);
            Assert.Equal(0.0, deserialized.Probe3Temp);
            Assert.Equal(0.0, deserialized.Probe4Temp);
        }

        #endregion
    }
}