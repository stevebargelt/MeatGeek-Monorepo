using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using Microsoft.Azure.Devices;

namespace Inferno.Functions
{

    public static class IoTGetStatus
    {
        private static ServiceClient IoTHubServiceClient;
        private static string ServiceConnectionString;

        [FunctionName("status")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request. IoTGetTemps.");
            ServiceConnectionString = Environment.GetEnvironmentVariable("InfernoIoTServiceConnection", EnvironmentVariableTarget.Process);
            IoTHubServiceClient = ServiceClient.CreateFromConnectionString(ServiceConnectionString);
            var methodInvocation = new CloudToDeviceMethod("SmokerGetStatus") { ResponseTimeout = TimeSpan.FromSeconds(30) };
            // Invoke the direct method asynchronously and get the response from the device.
            var response = await IoTHubServiceClient.InvokeDeviceMethodAsync("inferno1", methodInvocation);
            log.LogInformation("Response status: {0}, payload: {1}", response.Status, response.GetPayloadAsJson());
            return new ObjectResult(response.GetPayloadAsJson());
        }
    }
}


