using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Devices;
using Moq;
using Xunit;
using FluentAssertions;
using MeatGeek.IoT.WorkerApi;
using Newtonsoft.Json.Linq;
using MeatGeek.Shared;
using MeatGeek.Shared.EventSchemas.Sessions;

namespace MeatGeek.IoT.WorkerApi.Tests
{
    public class SessionCreatedTriggerTests
    {
        [Fact]
        public void SessionCreatedTrigger_CanBeInstantiatedWithValidParameters()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<SessionCreatedTrigger>>();
            var serviceClient = ServiceClient.CreateFromConnectionString("HostName=test.azure-devices.net;SharedAccessKeyName=test;SharedAccessKey=dGVzdA==");
            
            // Act - This test just verifies the class can be instantiated
            var trigger = new SessionCreatedTrigger(serviceClient, mockLogger.Object);
            
            // Assert
            Assert.NotNull(trigger);
            
            // Cleanup
            serviceClient.Dispose();
        }

        [Fact]
        public void SessionCreatedEventData_ShouldHaveCorrectProperties()
        {
            // Arrange
            var eventData = new SessionCreatedEventData
            {
                Id = "test-id",
                SmokerId = "test-smoker", 
                Title = "Test Title"
            };

            // Assert
            eventData.Id.Should().Be("test-id");
            eventData.SmokerId.Should().Be("test-smoker");
            eventData.Title.Should().Be("Test Title");
        }
    }
}