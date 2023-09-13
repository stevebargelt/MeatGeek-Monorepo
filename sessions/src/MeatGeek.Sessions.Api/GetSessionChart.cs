using System;
using System.IO;
using System.Net;
using System.Web.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;


using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Data;

#nullable enable
namespace MeatGeek.Sessions
{
   public class GetSessionChart
    {
        private const string JsonContentType = "application/json";
        private readonly ILogger<CreateSession> _log;
        private readonly ISessionsService _sessionsService; 
        private readonly CosmosClient _cosmosClient;

        public GetSessionChart(ILogger<CreateSession> log, ISessionsService sessionsService, CosmosClient cosmosClient)
        {
            _log = log;
            _sessionsService = sessionsService;
            _cosmosClient = cosmosClient;
        }

        [FunctionName("GetSessionChart")]
        [OpenApiOperation(operationId: "GetSessionChart", tags: new[] { "Session Chart" }, Summary = "Returns all session statuses", Description = "Returns all statues for a given session.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "smokerid", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "The ID of the Smoker the session belings to", Description = "The ID of the Smoker the session belings to", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "sessionid", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "ID of the Session to return", Description = "The ID of the session to return", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "timeseries", In = ParameterLocation.Path, Required = false, Type = typeof(int), Summary = "Minutes to group the return data. Integer between 1 and 60.", Description = "Minutes to group the return data. Integer between 1 and 60.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionStatuses), Summary = "successful operation", Description = "successful response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string), Summary = "Invalid input", Description = "Invalid input")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Session Statuses not found", Description = "Session Statuses Not Found")]         
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Summary = "An exception occurred", Description = "An exception occurred.")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions/statuses/{smokerId}/{sessionId}/{timeseries:int?}")] HttpRequest req,
                string smokerId, 
                string sessionId,
                int? timeSeries,
                ILogger log)
        {
            log.LogInformation("GetSessionChart triggered");

            if (string.IsNullOrEmpty(smokerId))
            {
                _log.LogError("GetSessionChart: Missing smokerId - url should be /sessions/statuses/{smokerId}/{sessionId}");
                return new BadRequestObjectResult(new { error = "Missing required property 'smokerId'." });
            }
            if (string.IsNullOrEmpty(sessionId))
            {
                _log.LogError("GetSessionChart: Missing sessionId - url should be /sessions/statuses/{smokerId}/{sessionId}");
                return new BadRequestObjectResult(new { error = "Missing required property 'sessionId'." });
            }
            if (!timeSeries.HasValue || timeSeries <= 0) {
                _log.LogInformation($"GetSessionChart timeSeries not sent or == 0 so setting to 1");
                timeSeries = 1;
            }
            if (timeSeries > 60) {
                _log.LogInformation($"GetSessionChart timeSeries > 60 so setting to 60");
                timeSeries = 60;
            }
            try
            {
                var statuses = await _sessionsService.GetSessionChartAsync(sessionId, smokerId, timeSeries);
                if (statuses == null)
                {
                    _log.LogInformation($"GetSessionChart no statuses found");
                    return new NotFoundResult();
                }
                _log.LogInformation($"GetSessionChart Numer of statuses = {statuses.Count}");
                
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                };
                //settings.Converters.Add(new SessionSummariesConverter());
                var json = JsonConvert.SerializeObject(statuses, settings);

                return new ContentResult
                {
                    Content = json,
                    ContentType = JsonContentType,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                log.LogError(ex, "<-- GetSessionChart Unhandled exception");
                return new ExceptionResult(ex, false);
            }
        }
    }       

}
