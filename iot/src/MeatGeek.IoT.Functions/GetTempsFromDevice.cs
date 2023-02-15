using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.AspNetCore.Http;
// Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

using Microsoft.Azure.Devices;

namespace MeatGeek.IoT
{
    public class GetTempsFromDevice
    {
        private readonly ILogger<GetTempsFromDevice> _log;

        public GetTempsFromDevice(ILogger<GetTempsFromDevice> log) {
            _log = log;
        }

        private static ServiceClient _iotHubServiceClient = ServiceClient.CreateFromConnectionString(Environment.GetEnvironmentVariable("IOT_SERVICE_CONNECTION", EnvironmentVariableTarget.Process));
        private const string METHOD_NAME = "GetTemps";
        private const string MODULE_NAME = "Telemetry";
        
        [FunctionName("GetTempsFromDevice")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "temps/{device}")] HttpRequest req,
            string device)
        {
            _log.LogInformation($"HTTP trigger function processed a request. GetTempsFromDevice which calls {METHOD_NAME} in module {MODULE_NAME}.");
            if (string.IsNullOrEmpty(device))
            {
                _log.LogError($"Must include the deviceid / name in route - https://address.com/api/temps/deviceid");
                return new BadRequestObjectResult("deviceid was not included in the route | https://address.com/api/temps/deviceid");
            }
            _log.LogInformation($"DeviceId = {device}");
            var methodInvocation = new CloudToDeviceMethod(METHOD_NAME) { ResponseTimeout = TimeSpan.FromSeconds(30) };
            try
            {
                _log.LogInformation($"Invoking method {METHOD_NAME} on module {device}/{MODULE_NAME}.");
                var result = await _iotHubServiceClient.InvokeDeviceMethodAsync(device, MODULE_NAME, methodInvocation).ConfigureAwait(false);
                if (IsSuccessStatusCode(result.Status))
                {
                    _log.LogInformation($"[{device}/{MODULE_NAME}] Successful direct method call result code={result.Status}");                    
                }
                else
                {
                    _log.LogWarning($"[{device}/{MODULE_NAME}] Unsuccessful direct method call result code={result.Status}");
                }
                return new JsonResult(result.GetPayloadAsJson());
            }
            catch (Exception e)
            {
                _log.LogError(e, $"[{device}/{MODULE_NAME}] Exeception on direct method call: {e.Message} | {e.InnerException}");
                return new BadRequestObjectResult("Exception was caught in function app.");
            }
        }

        private static bool IsSuccessStatusCode(int statusCode)
        {
            return (statusCode >= 200) && (statusCode <= 299);
        }
    }
}


