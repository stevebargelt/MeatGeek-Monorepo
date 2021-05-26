using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Devices;

using Newtonsoft.Json;

namespace Inferno.Functions
{
    public static class IoTSetPoint
    {
        private static ServiceClient IoTHubServiceClient;
        private static string ServiceConnectionString;

        [FunctionName("SetSetPoint")]
        public static async Task<IActionResult> SetSetPoint(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "setpoint")]HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            ServiceConnectionString = Environment.GetEnvironmentVariable("InfernoIoTServiceConnection", EnvironmentVariableTarget.Process);
            IoTHubServiceClient = ServiceClient.CreateFromConnectionString(ServiceConnectionString);

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            int value;
            try
            {
                value = JsonConvert.DeserializeObject<int>(requestBody);
            }
            catch (JsonReaderException)
            {
                return new BadRequestObjectResult(new { error = "Body should be a single integer between 180 and 450." });
            }
            log.LogInformation("value = " + value);
          
            if (value < 180 || value > 450) 
            {
                return new BadRequestObjectResult(new { error = "Body should be a single integer between 180 and 450." });
            }
            
            var methodInvocation = new CloudToDeviceMethod("SmokerSetSetPoint") { ResponseTimeout = TimeSpan.FromSeconds(30) };
            methodInvocation.SetPayloadJson(value.ToString());

            // Invoke the direct method asynchronously and get the response from the device.
            var response = await IoTHubServiceClient.InvokeDeviceMethodAsync("inferno1", methodInvocation);
            log.LogInformation("Response status: {0}, payload: {1}", response.Status, response.GetPayloadAsJson());
            return new ObjectResult(response.GetPayloadAsJson());
        }

        [FunctionName("GetSetPoint")]
        public static async Task<IActionResult> GetSetPoint(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "setpoint")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            ServiceConnectionString = Environment.GetEnvironmentVariable("InfernoIoTServiceConnection", EnvironmentVariableTarget.Process);
            IoTHubServiceClient = ServiceClient.CreateFromConnectionString(ServiceConnectionString);
            var methodInvocation = new CloudToDeviceMethod("SmokerGetSetPoint") { ResponseTimeout = TimeSpan.FromSeconds(30) };
            // Invoke the direct method asynchronously and get the response from the device.
            var response = await IoTHubServiceClient.InvokeDeviceMethodAsync("inferno1", methodInvocation);
            log.LogInformation("Response status: {0}, payload: {1}", response.Status, response.GetPayloadAsJson());
            return new ObjectResult(response.GetPayloadAsJson());
        }        

    }
}


