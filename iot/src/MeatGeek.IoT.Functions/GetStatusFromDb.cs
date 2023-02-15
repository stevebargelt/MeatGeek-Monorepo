using System;
using System.Linq;
using System.Collections.Generic;
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
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status/db/{smokerId}")] HttpRequest req,
            string smokerId,
            ILogger log)
        {
            log.LogInformation("SmokerId = " + smokerId);
            var container = _cosmosClient.GetContainer(
                Environment.GetEnvironmentVariable("DatabaseName", EnvironmentVariableTarget.Process),
                Environment.GetEnvironmentVariable("CollectionName", EnvironmentVariableTarget.Process)
            );

            SmokerStatus latestStatus = container.GetItemLinqQueryable<SmokerStatus>(requestOptions: new QueryRequestOptions { PartitionKey = new Microsoft.Azure.Cosmos.PartitionKey(smokerId) })
                    .OrderByDescending(p => p.CurrentTime)
                    .AsEnumerable()
                    .FirstOrDefault();

            return new JsonResult(latestStatus);
        }
    }
}
