using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using Microsoft.Azure.Devices;

namespace Inferno.Functions
{

    public static class TelemetryInterval
    {
        private static ServiceClient IoTHubServiceClient;
        private static string ServiceConnectionString;

        [FunctionName("telemetryinterval")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "telemetryinterval/{smokerId}")][FromBody] string value,
            string smokerId,
            ILogger log)
        {
            log.LogInformation("TelemetryInterval called");

            if (string.IsNullOrEmpty(smokerId))
            {
                log.LogError("TelemetryInterval: Missing smokerId - url should be /telemetryinterval/{smokerId}");
                return new BadRequestObjectResult(new { error = "Missing required property 'smokerId'." });
            }

            ServiceConnectionString = Environment.GetEnvironmentVariable("IOT_SERVICE_CONNECTION", EnvironmentVariableTarget.Process);
            IoTHubServiceClient = ServiceClient.CreateFromConnectionString(ServiceConnectionString);
            log.LogInformation("ServiceConnectionString" + ServiceConnectionString);
            log.LogInformation("value = " + value);
          
            if (string.IsNullOrEmpty(value)) 
            {
                log.LogWarning($"telemetryinterval : missing body value.");
                return new BadRequestObjectResult("Missing body value. Body should be a single integer.");
            }

            int interval;
            bool success = int.TryParse(value, out interval);
            if (!success)
            {
                log.LogWarning($"telemetryinterval : could not parse body value to integer");
                return new BadRequestObjectResult("Could not parse body value to integer. Body should be a single integer.");
            }

            if (interval < 1 || interval > 60) 
            {
                log.LogWarning($"telemetryinterval : interval out of range (1-60)");
                return new BadRequestObjectResult("Value out of range. Body should be a single integer 1-60.");
            }

            var methodInvocation = new CloudToDeviceMethod("SetTelemetryInterval", TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));
            methodInvocation.SetPayloadJson(interval.ToString());

            var response = await IoTHubServiceClient.InvokeDeviceMethodAsync(smokerId, "Telemetry", methodInvocation).ConfigureAwait(false);

            log.LogInformation("Response status: {0}, payload:", response.Status);
            log.LogInformation(response.GetPayloadAsJson());
            return new ObjectResult(response);

        }

    }
}


