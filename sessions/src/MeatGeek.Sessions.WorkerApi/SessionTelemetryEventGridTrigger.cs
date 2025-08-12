using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;
using MeatGeek.Sessions.WorkerApi.Models;

namespace MeatGeek.Sessions.WorkerApi
{
    public class SessionTelemetryEventGridTrigger
    {
        private readonly ILogger<SessionTelemetryEventGridTrigger> _log;

        public SessionTelemetryEventGridTrigger(ILogger<SessionTelemetryEventGridTrigger> log)
        {
            _log = log;
        }

        [Function("SessionTelemetryEventGridTrigger")]
        public Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
        {

            // var exceptions = new List<Exception>();
            _log.LogInformation($"SessionTelemetryEventGridTrigger function processing Event ID = {eventGridEvent.Id}");

            _log.LogInformation(eventGridEvent.Data.ToString());

            //we could also try the JObject version 
            
            //var messageBody = Encoding.UTF8.GetString(smokerStatusData.Body.Array, smokerStatusData.Body.Offset, smokerStatusData.Body.Count);
            // var smokerStatusString = JsonConvert.SerializeObject(smokerStatus);
            // _log.LogInformation($"SmokerStatus: {smokerStatusString}"); 
            // _log.LogInformation($"SmokerID: {smokerStatus.SmokerId}");
            
            return Task.CompletedTask;
            // if (smokerStatus.ttl is null || smokerStatus.ttl == 0 || smokerStatus.ttl == -1) {
            //     smokerStatus.ttl = 60 * 60 * 24 * 3;
            // }
            // smokerStatus.Type = "status";
            // await smokerStatusOut.AddAsync(smokerStatus);
 
            // _log.LogInformation("SessionTelemetryEventGridTrigger Called");
            // Additional event processing logic can be added here

        }
             
    }
}
