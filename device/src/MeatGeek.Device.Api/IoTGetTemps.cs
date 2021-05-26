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

    public static class IoTGetTemps
    {
        private static ServiceClient IoTHubServiceClient;
        private static ServiceClient MeatGeekIoTHubServiceClient;
        private static string ServiceConnectionString;
        private static string MeatGeekServiceConnectionString;

        [FunctionName("temps")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request. IoTGetTemps.");
            log.LogInformation("START: Inferno IoT");
            ServiceConnectionString = Environment.GetEnvironmentVariable("InfernoIoTServiceConnection", EnvironmentVariableTarget.Process);            
            IoTHubServiceClient = ServiceClient.CreateFromConnectionString(ServiceConnectionString);
            var methodInvocation = new CloudToDeviceMethod("SmokerGetTemps") { ResponseTimeout = TimeSpan.FromSeconds(30) };
            // Invoke the direct method asynchronously and get the response from the device.
            var response = await IoTHubServiceClient.InvokeDeviceMethodAsync("inferno1", methodInvocation);
            log.LogInformation("Response status: {0}, payload: {1}", response.Status, response.GetPayloadAsJson());
            log.LogInformation("Yes this is calling a C2D endpoint");
            log.LogInformation("END: Inferno IoT");

            // log.LogInformation("START: MeatGeek IoT");
            // MeatGeekServiceConnectionString = Environment.GetEnvironmentVariable("MeatGeekIoTServiceConnection", EnvironmentVariableTarget.Process);
            // MeatGeekIoTHubServiceClient = ServiceClient.CreateFromConnectionString(MeatGeekServiceConnectionString);
            // var methodInvocationMeatGeek = new CloudToDeviceMethod("SmokerGetTemps") { ResponseTimeout = TimeSpan.FromSeconds(30) };
            // // Invoke the direct method asynchronously and get the response from the device.
            // var response1 = await MeatGeekIoTHubServiceClient.InvokeDeviceMethodAsync("meatgeek1", "CSharpModule", methodInvocation);
            // log.LogInformation("Response status: {0}, payload: {1}", response1.Status, response1.GetPayloadAsJson());
            
            // log.LogInformation("END: MeatGeek IoT");

            return new ObjectResult(response.GetPayloadAsJson());
        }
    }
}


