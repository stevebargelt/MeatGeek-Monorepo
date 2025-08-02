using System;
using Xunit;
using Newtonsoft.Json;
using MeatGeek.Sessions.WorkerApi.Models;

namespace MeatGeek.Sessions.WorkerApi.Tests.Models
{
    public class SmokerStatusTests
    {
        #region JSON Serialization Tests

        [Fact]
        public void SmokerStatus_SerializeToJson_ProducesCorrectFormat()
        {
            // Arrange
            var smokerStatus = new SmokerStatus
            {
                Id = "status-123",
                ttl = 86400,
                SmokerId = "smoker-456",
                SessionId = "session-789",
                Type = "status",
                AugerOn = true,
                BlowerOn = false,
                IgniterOn = true,
                Temps = new Temps
                {
                    GrillTemp = 225.5,
                    Probe1Temp = 165.0,
                    Probe2Temp = 0.0,
                    Probe3Temp = 0.0,
                    Probe4Temp = 0.0
                },
                FireHealthy = true,
                Mode = "Smoke",
                SetPoint = 225,
                ModeTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc),
                CurrentTime = new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc)
            };

            // Act
            var json = JsonConvert.SerializeObject(smokerStatus, Formatting.Indented);

            // Assert
            Assert.Contains("\"id\": \"status-123\"", json);
            Assert.Contains("\"ttl\": 86400", json);
            Assert.Contains("\"smokerId\": \"smoker-456\"", json);
            Assert.Contains("\"sessionId\": \"session-789\"", json);
            Assert.Contains("\"augerOn\": true", json);
            Assert.Contains("\"blowerOn\": false", json);
            Assert.Contains("\"igniterOn\": true", json);
            Assert.Contains("\"fireHealthy\": true", json);
            Assert.Contains("\"mode\": \"Smoke\"", json);
            Assert.Contains("\"setPoint\": 225", json);
            Assert.Contains("\"grillTemp\": 225.5", json);
            Assert.Contains("\"probe1Temp\": 165.0", json);
        }

        [Fact]
        public void SmokerStatus_DeserializeFromJson_RestoresCorrectValues()
        {
            // Arrange
            var json = @"{
                ""id"": ""status-123"",
                ""ttl"": 86400,
                ""smokerId"": ""smoker-456"",
                ""sessionId"": ""session-789"",
                ""type"": ""status"",
                ""augerOn"": true,
                ""blowerOn"": false,
                ""igniterOn"": true,
                ""temps"": {
                    ""grillTemp"": 225.5,
                    ""probe1Temp"": 165.0,
                    ""probe2Temp"": 0.0,
                    ""probe3Temp"": 0.0,
                    ""probe4Temp"": 0.0
                },
                ""fireHealthy"": true,
                ""mode"": ""Smoke"",
                ""setPoint"": 225,
                ""modeTime"": ""2024-01-15T12:00:00Z"",
                ""currentTime"": ""2024-01-15T14:30:00Z""
            }";

            // Act
            var smokerStatus = JsonConvert.DeserializeObject<SmokerStatus>(json);

            // Assert
            Assert.Equal("status-123", smokerStatus.Id);
            Assert.Equal(86400, smokerStatus.ttl);
            Assert.Equal("smoker-456", smokerStatus.SmokerId);
            Assert.Equal("session-789", smokerStatus.SessionId);
            Assert.Equal("status", smokerStatus.Type);
            Assert.True(smokerStatus.AugerOn);
            Assert.False(smokerStatus.BlowerOn);
            Assert.True(smokerStatus.IgniterOn);
            Assert.True(smokerStatus.FireHealthy);
            Assert.Equal("Smoke", smokerStatus.Mode);
            Assert.Equal(225, smokerStatus.SetPoint);
            Assert.Equal(225.5, smokerStatus.Temps.GrillTemp);
            Assert.Equal(165.0, smokerStatus.Temps.Probe1Temp);
            Assert.Equal(new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc), smokerStatus.ModeTime);
            Assert.Equal(new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc), smokerStatus.CurrentTime);
        }

        #endregion

        #region Property Tests

        [Fact]
        public void SmokerStatus_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var smokerStatus = new SmokerStatus();

            // Assert
            Assert.Null(smokerStatus.Id);
            Assert.Null(smokerStatus.ttl);
            Assert.Null(smokerStatus.SmokerId);
            Assert.Null(smokerStatus.SessionId);
            Assert.Null(smokerStatus.Type);
            Assert.False(smokerStatus.AugerOn);
            Assert.False(smokerStatus.BlowerOn);
            Assert.False(smokerStatus.IgniterOn);
            Assert.Null(smokerStatus.Temps);
            Assert.False(smokerStatus.FireHealthy);
            Assert.Null(smokerStatus.Mode);
            Assert.Equal(0, smokerStatus.SetPoint);
            Assert.Equal(default(DateTime), smokerStatus.ModeTime);
            Assert.Equal(default(DateTime), smokerStatus.CurrentTime);
        }

        [Fact]
        public void SmokerStatus_SettingAllProperties_WorksCorrectly()
        {
            // Arrange
            var temps = new Temps
            {
                GrillTemp = 250.0,
                Probe1Temp = 160.0,
                Probe2Temp = 170.0,
                Probe3Temp = 0.0,
                Probe4Temp = 0.0
            };

            var modeTime = DateTime.UtcNow.AddHours(-1);
            var currentTime = DateTime.UtcNow;

            // Act
            var smokerStatus = new SmokerStatus
            {
                Id = "test-id",
                ttl = 3600,
                SmokerId = "test-smoker",
                SessionId = "test-session",
                Type = "test-type",
                AugerOn = true,
                BlowerOn = true,
                IgniterOn = false,
                Temps = temps,
                FireHealthy = true,
                Mode = "Grill",
                SetPoint = 250,
                ModeTime = modeTime,
                CurrentTime = currentTime
            };

            // Assert
            Assert.Equal("test-id", smokerStatus.Id);
            Assert.Equal(3600, smokerStatus.ttl);
            Assert.Equal("test-smoker", smokerStatus.SmokerId);
            Assert.Equal("test-session", smokerStatus.SessionId);
            Assert.Equal("test-type", smokerStatus.Type);
            Assert.True(smokerStatus.AugerOn);
            Assert.True(smokerStatus.BlowerOn);
            Assert.False(smokerStatus.IgniterOn);
            Assert.Equal(temps, smokerStatus.Temps);
            Assert.True(smokerStatus.FireHealthy);
            Assert.Equal("Grill", smokerStatus.Mode);
            Assert.Equal(250, smokerStatus.SetPoint);
            Assert.Equal(modeTime, smokerStatus.ModeTime);
            Assert.Equal(currentTime, smokerStatus.CurrentTime);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void SmokerStatus_NullTtl_SerializesCorrectly()
        {
            // Arrange
            var smokerStatus = new SmokerStatus
            {
                Id = "test-null-ttl",
                ttl = null,
                SmokerId = "smoker-123"
            };

            // Act
            var json = JsonConvert.SerializeObject(smokerStatus);
            var deserialized = JsonConvert.DeserializeObject<SmokerStatus>(json);

            // Assert
            Assert.Null(deserialized.ttl);
            Assert.Equal("test-null-ttl", deserialized.Id);
            Assert.Equal("smoker-123", deserialized.SmokerId);
        }

        [Fact]
        public void SmokerStatus_NullTemps_HandledCorrectly()
        {
            // Arrange
            var smokerStatus = new SmokerStatus
            {
                Id = "test-null-temps",
                SmokerId = "smoker-123",
                Temps = null
            };

            // Act
            var json = JsonConvert.SerializeObject(smokerStatus);
            var deserialized = JsonConvert.DeserializeObject<SmokerStatus>(json);

            // Assert
            Assert.Null(deserialized.Temps);
            Assert.Equal("test-null-temps", deserialized.Id);
        }

        [Fact]
        public void SmokerStatus_ExtremeDateValues_SerializesCorrectly()
        {
            // Arrange
            var smokerStatus = new SmokerStatus
            {
                Id = "test-extreme-dates",
                ModeTime = DateTime.MinValue,
                CurrentTime = DateTime.MaxValue
            };

            // Act
            var json = JsonConvert.SerializeObject(smokerStatus);
            var deserialized = JsonConvert.DeserializeObject<SmokerStatus>(json);

            // Assert
            Assert.Equal(DateTime.MinValue, deserialized.ModeTime);
            Assert.Equal(DateTime.MaxValue, deserialized.CurrentTime);
        }

        #endregion
    }
}