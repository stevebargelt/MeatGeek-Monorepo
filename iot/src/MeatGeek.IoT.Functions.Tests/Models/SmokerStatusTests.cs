using System;
using Xunit;
using FluentAssertions;
using MeatGeek.IoT.Models;

namespace MeatGeek.IoT.Functions.Tests.Models
{
    public class SmokerStatusTests
    {
        [Fact]
        public void SmokerStatus_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var status = new SmokerStatus();

            // Assert
            status.Should().NotBeNull();
        }

        [Fact]
        public void SmokerStatus_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var smokerId = "test-smoker-123";
            var sessionId = Guid.NewGuid().ToString();
            var mode = "HOLD";
            var setPoint = 225;
            var augerOn = true;
            var blowerOn = true;
            var igniterOn = false;
            var fireHealthy = true;
            var currentTime = DateTime.UtcNow;

            // Act
            var status = new SmokerStatus
            {
                Id = id,
                SmokerId = smokerId,
                SessionId = sessionId,
                Mode = mode,
                SetPoint = setPoint,
                AugerOn = augerOn,
                BlowerOn = blowerOn,
                IgniterOn = igniterOn,
                FireHealthy = fireHealthy,
                CurrentTime = currentTime,
                Type = "status"
            };

            // Assert
            status.Id.Should().Be(id);
            status.SmokerId.Should().Be(smokerId);
            status.SessionId.Should().Be(sessionId);
            status.Mode.Should().Be(mode);
            status.SetPoint.Should().Be(setPoint);
            status.AugerOn.Should().Be(augerOn);
            status.BlowerOn.Should().Be(blowerOn);
            status.IgniterOn.Should().Be(igniterOn);
            status.FireHealthy.Should().Be(fireHealthy);
            status.CurrentTime.Should().Be(currentTime);
            status.Type.Should().Be("status");
        }
    }
}