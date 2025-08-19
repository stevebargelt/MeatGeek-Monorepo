using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MeatGeek.Shared;
using MeatGeek.Shared.EventSchemas.Sessions;
using Newtonsoft.Json.Linq;

namespace MeatGeek.IoT.WorkerApi
{
    public class SessionCreatedTrigger
    {
        private readonly ServiceClient _iothubServiceClient;
        private readonly ILogger<SessionCreatedTrigger> _logger;
        private const string METHOD_NAME = "SetSessionId";
        private const string MODULE_NAME = "Telemetry";

        public SessionCreatedTrigger(ServiceClient iothubServiceClient, ILogger<SessionCreatedTrigger> logger)
        {
            _iothubServiceClient = iothubServiceClient;
            _logger = logger;
        }

        [Function("SessionCreated")]
        public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent, FunctionContext context)
        {
            _logger.LogInformation("SessionCreated Called");
            _logger.LogInformation(eventGridEvent.Data.ToString());
            
            try
            {
                SessionCreatedEventData? sessionCreatedEventData;

                //TODO: Maybe some error/exception handling here??
                var data = eventGridEvent.Data as JObject;
                if (data == null)
                {
                    _logger.LogError("Event data is null");
                    return;
                }
                
                sessionCreatedEventData = data.ToObject<SessionCreatedEventData>();
                if (sessionCreatedEventData == null)
                {
                    _logger.LogError("Failed to deserialize event data");
                    return;
                }

                var smokerId = sessionCreatedEventData.SmokerId;
                var sessionId = sessionCreatedEventData.Id;
                _logger.LogInformation("SmokerID = " + smokerId);
                _logger.LogInformation("SessionID = " + sessionId);
                
                try
                {
                    var methodRequest = new CloudToDeviceMethod(METHOD_NAME, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));
                    methodRequest.SetPayloadJson($"\"{sessionId}\"");

                    _logger.LogInformation($"Invoking method Session on module {smokerId}/{MODULE_NAME}.");
                    // Invoke direct method
                    var result = await _iothubServiceClient.InvokeDeviceMethodAsync(smokerId, MODULE_NAME, methodRequest).ConfigureAwait(false);

                    if (IsSuccessStatusCode(result.Status))
                    {
                        _logger.LogInformation($"[{smokerId}/{MODULE_NAME}] Successful direct method call result code={result.Status}");
                    }
                    else
                    {
                        _logger.LogWarning($"[{smokerId}/{MODULE_NAME}] Unsuccessful direct method call result code={result.Status}");
                    }
                    //return new ObjectResult(result);
                }
                catch(ArgumentException e) 
                {
                    _logger.LogError(e, $"Argument exception methodRequest = new CloudToDeviceMethod...");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"[{smokerId}/{MODULE_NAME}] Exeception on direct method call");
                    //return new BadRequestObjectResult("Exception was caught in function app.");
                }               

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "<-- SessionCreated Event Grid Trigger: Unhandled exception");
                //return new BadRequestObjectResult("SessionCreated: Unhandled Exception in function app.");
            }
        }
        
        private bool IsSuccessStatusCode(int statusCode)
        {
            return (statusCode >= 200) && (statusCode <= 299);
        }        
    }
}
