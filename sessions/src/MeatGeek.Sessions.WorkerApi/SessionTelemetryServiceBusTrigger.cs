// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

// using MeatGeek.Shared;
// using MeatGeek.Shared.EventSchemas.Sessions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MeatGeek.Sessions.WorkerApi
{
    public class SessionTelemetryServiceBusTrigger
    {

        [FunctionName("SessionTelemetryServiceBusTrigger")]
        public static void Run(
            [ServiceBusTrigger("sessiontelemetry", "sessiontelemetry", Connection = "SessionsServiceBus")] 
            string myTopicItem,
            Int32 deliveryCount,
            DateTime enqueuedTimeUtc,
            string messageId,
            ILogger log)
        {
            log.LogInformation("SessionTelemetryServiceBusTrigger Called");
            log.LogInformation($"Processed message: {myTopicItem}");
            log.LogInformation($"EnqueuedTimeUtc={enqueuedTimeUtc}");
            log.LogInformation($"DeliveryCount={deliveryCount}");
            log.LogInformation($"MessageId={messageId}");
        }
             
    }
}
