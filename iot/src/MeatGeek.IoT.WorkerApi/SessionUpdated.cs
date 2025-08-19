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
    public class SessionUpdatedTrigger
    {
        private readonly ServiceClient _iothubServiceClient;
        private readonly ILogger<SessionUpdatedTrigger> _logger;
        private const string METHOD_NAME = "EndSession";
        private const string MODULE_NAME = "Telemetry";

        public SessionUpdatedTrigger(ServiceClient iothubServiceClient, ILogger<SessionUpdatedTrigger> logger)
        {
            _iothubServiceClient = iothubServiceClient;
            _logger = logger;
        }

        [Function("SessionUpdated")]
        public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent, FunctionContext context)
        {
            _logger.LogInformation("SessionUpdated Called");
            _logger.LogInformation(eventGridEvent.Data.ToString());
            
            try
            {
                SessionUpdatedEventData? sessionUpdatedEventData;

                //TODO: Maybe some error/exception handling here??
                var data = eventGridEvent.Data as JObject;
                if (data == null)
                {
                    _logger.LogError("Event data is null");
                    return;
                }
                
                sessionUpdatedEventData = data.ToObject<SessionUpdatedEventData>();
                if (sessionUpdatedEventData == null)
                {
                    _logger.LogError("Failed to deserialize event data");
                    return;
                }

                var smokerId = sessionUpdatedEventData.SmokerId;
                var sessionId = sessionUpdatedEventData.Id;
                _logger.LogInformation("SmokerID = " + smokerId);
                _logger.LogInformation("SessionID = " + sessionId);
                if (sessionUpdatedEventData.EndTime.HasValue) 
                {
                    _logger.LogInformation("Processing EndSession: EndTime has a value");
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
                    }
                    catch(ArgumentException e) 
                    {
                        _logger.LogError(e, $"Argument exception methodRequest = new CloudToDeviceMethod...");
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"[{smokerId}/{MODULE_NAME}] Exeception on direct method call");
                    }               
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "<-- SessionUpdated Event Grid Trigger: Unhandled exception");
            }
        }
        
        private bool IsSuccessStatusCode(int statusCode)
        {
            return (statusCode >= 200) && (statusCode <= 299);
        }        
    }
}
