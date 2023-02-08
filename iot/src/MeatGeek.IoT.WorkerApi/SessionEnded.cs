using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using MeatGeek.Shared;
using MeatGeek.Shared.EventSchemas.Sessions;
using Newtonsoft.Json.Linq;

namespace MeatGeek.IoT.WorkerApi
{
    public class SessionEndedTrigger
    {
        private static ServiceClient _iothubServiceClient = ServiceClient.CreateFromConnectionString(Environment.GetEnvironmentVariable("IOT_SERVICE_CONNECTION", EnvironmentVariableTarget.Process));
        private const string METHOD_NAME = "EndSession";
        private const string MODULE_NAME = "Telemetry";

        [FunctionName("SessionEnded")]
        public static async Task Run(
            [EventGridTrigger]EventGridEvent eventGridEvent,
            ILogger log)
        {
            log.LogInformation("SessionEnded Called");
            log.LogInformation(eventGridEvent.Data.ToString());
            
            try
            {
                SessionEndedEventData sessionEndedEventData;

                //TODO: Maybe some error/exception handling here??
                var data = eventGridEvent.Data as JObject;
                sessionEndedEventData = data.ToObject<SessionEndedEventData>();

                var smokerId = sessionEndedEventData.SmokerId;
                var sessionId = sessionEndedEventData.Id;
                log.LogInformation("SmokerID = " + smokerId);
                log.LogInformation("SessionID = " + sessionId);
                if (sessionEndedEventData.EndTime.HasValue) 
                {
                    log.LogInformation("Processing EndSession: EndTime has a value");
                    try
                    {
                        var methodRequest = new CloudToDeviceMethod(METHOD_NAME, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));
                        methodRequest.SetPayloadJson($"\"{sessionId}\"");

                        log.LogInformation($"Invoking method Session on module {smokerId}/{MODULE_NAME}.");
                        // Invoke direct method
                        var result = await _iothubServiceClient.InvokeDeviceMethodAsync(smokerId, MODULE_NAME, methodRequest).ConfigureAwait(false);

                        if (IsSuccessStatusCode(result.Status))
                        {
                            log.LogInformation($"[{smokerId}/{MODULE_NAME}] Successful direct method call result code={result.Status}");
                        }
                        else
                        {
                            log.LogWarning($"[{smokerId}/{MODULE_NAME}] Unsuccessful direct method call result code={result.Status}");
                        }
                    }
                    catch(ArgumentException e) 
                    {
                        log.LogError(e, $"Argument exception methodRequest = new CloudToDeviceMethod...");
                    }
                    catch (Exception e)
                    {
                        log.LogError(e, $"[{smokerId}/{MODULE_NAME}] Exeception on direct method call");
                    }               
                }
                else
                {
                    throw new ArgumentException($"Missing EndTime");
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "<-- SessionEnded Event Grid Trigger: Unhandled exception");
            }
        }
        
        private static bool IsSuccessStatusCode(int statusCode)
        {
            return (statusCode >= 200) && (statusCode <= 299);
        }        
    }
}
