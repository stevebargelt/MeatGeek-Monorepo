using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using MeatGeek.Shared;
using MeatGeek.Shared.EventSchemas.Sessions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using MeatGeek.IoT.WorkerApi.Configurations;

namespace MeatGeek.IoT.WorkerApi
{
    public class SessionCreatedTrigger
    {


        private static ServiceClient _iothubServiceClient = ServiceClient.CreateFromConnectionString(Environment.GetEnvironmentVariable("IOT_SERVICE_CONNECTION", EnvironmentVariableTarget.Process));
        private const string METHOD_NAME = "SetSessionId";
        private const string MODULE_NAME = "Telemetry";

        [FunctionName("SessionCreated")]
        public static async Task Run(
            [EventGridTrigger]EventGridEvent eventGridEvent,
            ILogger log)
        {
            log.LogInformation("SessionCreated Called");
            log.LogInformation(eventGridEvent.Data.ToString());
            
            try
            {
                SessionCreatedEventData sessionCreatedEventData;

                //TODO: Maybe some error/exception handling here??
                var data = eventGridEvent.Data as JObject;
                sessionCreatedEventData = data.ToObject<SessionCreatedEventData>();

                var smokerId = sessionCreatedEventData.SmokerId;
                var sessionId = sessionCreatedEventData.Id;
                log.LogInformation("SmokerID = " + smokerId);
                log.LogInformation("SessionID = " + sessionId);
                
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
                    //return new ObjectResult(result);
                }
                catch(ArgumentException e) 
                {
                    log.LogError(e, $"Argument exception methodRequest = new CloudToDeviceMethod...");
                }
                catch (Exception e)
                {
                    log.LogError(e, $"[{smokerId}/{MODULE_NAME}] Exeception on direct method call");
                    //return new BadRequestObjectResult("Exception was caught in function app.");
                }               

            }
            catch (Exception ex)
            {
                log.LogError(ex, "<-- SessionCreated Event Grid Trigger: Unhandled exception");
                //return new BadRequestObjectResult("SessionCreated: Unhandled Exception in function app.");
            }
        }
        
        private static bool IsSuccessStatusCode(int statusCode)
        {
            return (statusCode >= 200) && (statusCode <= 299);
        }        
    }
}
