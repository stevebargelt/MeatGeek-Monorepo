// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
// using MeatGeek.Shared;
// using MeatGeek.Shared.EventSchemas.Sessions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MeatGeek.Sessions.WorkerApi
{
    public class SessionTelemetryEventGridTrigger
    {

        [FunctionName("SessionTelemetryEventGridTrigger")]
        public static async Task Run(
            [EventGridTrigger]EventGridEvent eventGridEvent,
            ILogger log)
        {
            log.LogInformation("SessionTelemetryEventGridTrigger Called");
            log.LogInformation(eventGridEvent.Data.ToString());

        }
             
    }
}
