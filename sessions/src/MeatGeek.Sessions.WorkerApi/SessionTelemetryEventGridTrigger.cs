using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using MeatGeek.Sessions.WorkerApi.Models;

namespace MeatGeek.Sessions.WorkerApi
{
    public class SessionTelemetryEventGridTrigger
    {
        [Function("SessionTelemetryEventGridTrigger")]
        public Task Run(
            [EventGridTrigger] EventGridEvent eventGridEvent,
            FunctionContext context)
        {
            var log = context.GetLogger("SessionTelemetryEventGridTrigger");

            // var exceptions = new List<Exception>();
            log.LogInformation($"SessionTelemetryEventGridTrigger function processing Event ID = {eventGridEvent.Id}");

            log.LogInformation(eventGridEvent.Data.ToString());

            //we could also try the JObject version 

            //var messageBody = Encoding.UTF8.GetString(smokerStatusData.Body.Array, smokerStatusData.Body.Offset, smokerStatusData.Body.Count);
            // var smokerStatusString = JsonConvert.SerializeObject(smokerStatus);
            // log.LogInformation($"SmokerStatus: {smokerStatusString}"); 
            // log.LogInformation($"SmokerID: {smokerStatus.SmokerId}");

            return Task.CompletedTask;
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
