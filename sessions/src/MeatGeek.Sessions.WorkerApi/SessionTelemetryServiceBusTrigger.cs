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
            SmokerStatus[] smokerStatuses,
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
            log.LogInformation($"IoT Hub trigger function processing {smokerStatuses.Length} events.");

            foreach (var smokerStatus in smokerStatuses)
            {
                try
                {
                    //var messageBody = Encoding.UTF8.GetString(smokerStatusData.Body.Array, smokerStatusData.Body.Offset, smokerStatusData.Body.Count);
                    var smokerStatusString = JsonConvert.SerializeObject(smokerStatus);
                    log.LogInformation($"SmokerStatus: {smokerStatusString}"); 
                    log.LogInformation($"SmokerID: {smokerStatus.SmokerId}");
                    if (smokerStatus.ttl is null || smokerStatus.ttl == 0 || smokerStatus.ttl == -1) {
                        smokerStatus.ttl = 60 * 60 * 24 * 3;
                    }
                    smokerStatus.Type = "status";
                    await smokerStatusOut.AddAsync(smokerStatus);
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }
 
            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that 
            //      there is a record of the failure.
            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();            

            // log.LogInformation("SessionTelemetryServiceBusTrigger Called");
            // log.LogInformation($"EnqueuedTimeUtc={enqueuedTimeUtc}");
            // log.LogInformation($"DeliveryCount={deliveryCount}");
            // log.LogInformation($"MessageId={messageId}");

        }
             
    }
}
