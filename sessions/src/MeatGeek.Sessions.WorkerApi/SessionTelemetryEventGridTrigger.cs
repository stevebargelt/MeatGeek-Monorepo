using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MeatGeek.Sessions.WorkerApi.Models;

namespace MeatGeek.Sessions.WorkerApi
{
    public class SessionTelemetryEventGridTrigger
    {
        [FunctionName("SessionTelemetryEventGridTrigger")]
        public static async Task Run(            
            [EventGridTrigger] EventGridEvent eventGridEvent,
            SmokerStatus smokerStatus,
            Int32 deliveryCount,
            DateTime enqueuedTimeUtc,
            string messageId,
            IAsyncCollector<SmokerStatus> smokerStatusOut,            
            ILogger log)
        {

            // var exceptions = new List<Exception>();
            log.LogInformation($"SessionTelemetryEventGridTrigger function processing Message ID = {messageId}");

            log.LogInformation(eventGridEvent.Data.ToString());

            //var messageBody = Encoding.UTF8.GetString(smokerStatusData.Body.Array, smokerStatusData.Body.Offset, smokerStatusData.Body.Count);
            // var smokerStatusString = JsonConvert.SerializeObject(smokerStatus);
            // log.LogInformation($"SmokerStatus: {smokerStatusString}"); 
            // log.LogInformation($"SmokerID: {smokerStatus.SmokerId}");
            // if (smokerStatus.ttl is null || smokerStatus.ttl == 0 || smokerStatus.ttl == -1) {
            //     smokerStatus.ttl = 60 * 60 * 24 * 3;
            // }
            // smokerStatus.Type = "status";
            // await smokerStatusOut.AddAsync(smokerStatus);
 
            // log.LogInformation("SessionTelemetryEventGridTrigger Called");
            // log.LogInformation($"EnqueuedTimeUtc={enqueuedTimeUtc}");
            // log.LogInformation($"DeliveryCount={deliveryCount}");
            // log.LogInformation($"MessageId={messageId}");

        }
             
    }
}
