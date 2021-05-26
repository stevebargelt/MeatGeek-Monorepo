using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
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
    public class Chart
    {
        private readonly CosmosClient _cosmosClient;

        // Use Dependency Injection to inject the HttpClientFactory service and Cosmos DB client that were configured in Startup.cs.
        public Chart(CosmosClient cosmosClient)
        {
            _cosmosClient = cosmosClient;
        }
        
        /// <summary>
        /// Get Session Charts
        /// </summary>
        /// <param name="starttime"></param>
        /// <param name="endtime"></param>
        /// <param name="timeseries"></param>
        /// <returns></returns>
        [FunctionName("GetChart")]
        [OpenApiOperation(operationId: "GetChart", tags: new[] { "IoT" }, Summary = "Get chart data", Description = "Returns a list of SmokerStatus", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "starttime", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "2021-05-12T15%3A53%3A29.991Z", Description = "Where to start the data return URL Encoded ISO-8601.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "timeseries", In = ParameterLocation.Path, Required = false, Type = typeof(int), Summary = "15", Description = "Minutes to group the return data. Integer between 0 and 60.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "endtime", In = ParameterLocation.Path, Required = false, Type = typeof(string), Summary = "2021-05-12T22%3A22%3A15.675Z", Description = "Where to stop the data return. URL Encoded ISO-8601.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<SmokerStatus>), Summary = "successful operation", Description = "successful response")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid starttime supplied", Description = "Invalid starttime supplied")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Data not found", Description = "Data not found")]
        public  async Task<IActionResult> GetChart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "chart/{starttime}/{timeseries:int?}/{endtime?}")] HttpRequest req, 
            string starttime,
            int? timeseries,
#nullable enable            
            string? endtime,
#nullable disable            
            ILogger log)        
        {

            if (starttime == null)
            {
                log.LogInformation($"Start Time (starttime) not found");
                return new NotFoundResult();
            }

            //TODO: try/catch this
            DateTime StartDateTime = DateTime.Parse(starttime, null, System.Globalization.DateTimeStyles.RoundtripKind);
            
            //TODO: Sent SmokerID as a parameter to function call
            var SmokerId = "meatgeek2";
            log.LogInformation("SmokerId = " + SmokerId);

            DateTime EndDateTime;
            //TODO: try/catch this
            if (String.IsNullOrEmpty(endtime)) {
                EndDateTime = DateTime.UtcNow;
            }
            else {
                EndDateTime = DateTime.Parse(endtime, null, System.Globalization.DateTimeStyles.RoundtripKind);
            }

            log.LogInformation($"StartTime = {StartDateTime} EndTime = {EndDateTime}");

            var container = _cosmosClient.GetContainer("iot", "telemetry");

            Microsoft.Azure.Cosmos.FeedIterator<SmokerStatus> query;
            query = container.GetItemLinqQueryable<SmokerStatus>(requestOptions: new QueryRequestOptions { PartitionKey = new Microsoft.Azure.Cosmos.PartitionKey(SmokerId) })
                    .Where(p => p.CurrentTime >= StartDateTime
                            && p.CurrentTime <= EndDateTime)                            
                    .ToFeedIterator();

            List<SmokerStatus> SmokerStatuses = new List<SmokerStatus>();
            var count = 0;
            while (query.HasMoreResults)
            {
                foreach(var status in await query.ReadNextAsync())
                {
                    count++;
                    SmokerStatuses.Add(status);
                }
            }
            log.LogInformation("Statuses " + count);

            if (!timeseries.HasValue) {
                return new OkObjectResult(SmokerStatuses);
            }

            if (timeseries > 0 && timeseries <=60)
            {
                TimeSpan interval = new TimeSpan(0, timeseries.Value, 0); 
                List<SmokerStatus> SortedList = SmokerStatuses.OrderBy(o => o.CurrentTime).ToList();
                var result = SortedList.GroupBy(x=> x.CurrentTime.Ticks/interval.Ticks)
                        .Select(x=>x.First());
                return new OkObjectResult(result);
           
            }
            // Return a 400 bad request result to the client with additional information
            return new BadRequestObjectResult("Please pass a timeseries in range of 1 to 60");

        }  
    }
}
