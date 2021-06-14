using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using MeatGeek.Sessions.WorkerApi.Models;

namespace MeatGeek.Sessions.WorkerApi
{
    public class SessionTelemetryServiceBusTrigger
    {
        // private readonly CosmosClient _cosmosClient;

        // public SessionTelemetryServiceBusTrigger(CosmosClient cosmosClient)
        // {
        //     _cosmosClient = cosmosClient;
        // }

        [FunctionName("SessionTelemetryServiceBusTrigger")]
        public static async Task Run(
            [ServiceBusTrigger("sessiontelemetry", "sessiontelemetry", Connection = "SessionsServiceBus")] 
            SmokerStatus smokerStatus,
            Int32 deliveryCount,
            DateTime enqueuedTimeUtc,
            string messageId,
            [CosmosDB(
                databaseName: "Sessions",
                collectionName: "sessions",
                ConnectionStringSetting = "CosmosDBConnection")]
            IAsyncCollector<SmokerStatus> smokerStatusOut,            
            ILogger log)
        {

            var exceptions = new List<Exception>();
            log.LogInformation($"SessionTelemetryServiceBusTrigger function processing Message ID = {messageId}");

            //var messageBody = Encoding.UTF8.GetString(smokerStatusData.Body.Array, smokerStatusData.Body.Offset, smokerStatusData.Body.Count);
            var smokerStatusString = JsonConvert.SerializeObject(smokerStatus);
            log.LogInformation($"SmokerStatus: {smokerStatusString}"); 
            log.LogInformation($"SmokerID: {smokerStatus.SmokerId}");
            if (smokerStatus.ttl is null || smokerStatus.ttl == 0 || smokerStatus.ttl == -1) {
                smokerStatus.ttl = 60 * 60 * 24 * 3;
            }
            smokerStatus.Type = "status";
            await smokerStatusOut.AddAsync(smokerStatus);
 
            // log.LogInformation("SessionTelemetryServiceBusTrigger Called");
            // log.LogInformation($"EnqueuedTimeUtc={enqueuedTimeUtc}");
            // log.LogInformation($"DeliveryCount={deliveryCount}");
            // log.LogInformation($"MessageId={messageId}");

        }
             
    }
}
