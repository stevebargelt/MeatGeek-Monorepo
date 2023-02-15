using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MeatGeek.IoT.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace MeatGeek.IoT
{
    public class GetStatusFromDb
    {
        private readonly CosmosClient _cosmosClient;

        // Use Dependency Injection to inject the HttpClientFactory service and Cosmos DB client that were configured in Startup.cs.
        public GetStatusFromDb(CosmosClient cosmosClient)
        {
            _cosmosClient = cosmosClient;
        }

        /// <summary>
        /// Get Latest Reported Status from a Smoker
        /// </summary>
        /// <param name="smokerId"></param>
        /// <returns></returns>
        [FunctionName("GetStatusFromDb")]
        [OpenApiOperation(operationId: "GetStatusFromDb", tags: new[] { "MeatGeek" }, Summary = "Returns last reported status from a smoker", Description = "Returns last reported status from a smoker", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<SmokerStatus>), Summary = "successful operation", Description = "successful response")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Data not found", Description = "Data not found")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status/db/{smokerId}")] HttpRequest req,
            string smokerId,
            ILogger log)
        {
            log.LogInformation("SmokerId = " + smokerId);
            var container = _cosmosClient.GetContainer(
                Environment.GetEnvironmentVariable("DatabaseName", EnvironmentVariableTarget.Process),
                Environment.GetEnvironmentVariable("CollectionName", EnvironmentVariableTarget.Process)
            );

            // TODO: Need to create a new CosmosDB container and always repalce the most recent record based
            //          based on the Cosmos DB change feed. MUCH faster. 
            var parameterizedQuery = new QueryDefinition(
                query: @"SELECT TOP 1 * FROM s 
                        WHERE s.smokerId=@partitionKey 
                        AND s.type=@type 
                        ORDER BY s.currentTime DESC"
                )
                .WithParameter("@partitionKey", smokerId)
                .WithParameter("@type", "status");

            // Query multiple items from container
            using FeedIterator<SmokerStatus> filteredFeed = container.GetItemQueryIterator<SmokerStatus>(
                queryDefinition: parameterizedQuery
            );

            SmokerStatus status = null;
            while (filteredFeed.HasMoreResults)
            {
                FeedResponse<SmokerStatus> response = await filteredFeed.ReadNextAsync();
                status = response.First();
            }
            if (status != null) 
            {
                return new JsonResult(status);
            }
            else
            {
                return new NotFoundResult();
            }
        }
    }
}
