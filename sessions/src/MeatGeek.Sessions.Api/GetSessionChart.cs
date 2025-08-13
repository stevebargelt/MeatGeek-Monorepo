using System.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
// using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
// using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
// using Microsoft.OpenApi.Models;


using MeatGeek.Sessions.Services;

#nullable enable
namespace MeatGeek.Sessions.Api
{
    public class GetSessionChart
    {
        private const string JsonContentType = "application/json";
        private readonly ILogger<GetSessionChart> _log;
        private readonly ISessionsService _sessionsService;

        public GetSessionChart(ILogger<GetSessionChart> log, ISessionsService sessionsService)
        {
            _log = log;
            _sessionsService = sessionsService;
        }

        [Function("GetSessionChart")]
        // [OpenApiOperation(operationId: "GetSessionChart", tags: new[] { "Session Chart" }, Summary = "Returns all session statuses", Description = "Returns all statues for a given session.", Visibility = OpenApiVisibilityType.Important)]
        // [OpenApiParameter(name: "smokerid", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "The ID of the Smoker the session belings to", Description = "The ID of the Smoker the session belings to", Visibility = OpenApiVisibilityType.Important)]
        // [OpenApiParameter(name: "sessionid", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "ID of the Session to return", Description = "The ID of the session to return", Visibility = OpenApiVisibilityType.Important)]
        // [OpenApiParameter(name: "timeseries", In = ParameterLocation.Path, Required = false, Type = typeof(int), Summary = "Minutes to group the return data. Integer between 1 and 60.", Description = "Minutes to group the return data. Integer between 1 and 60.", Visibility = OpenApiVisibilityType.Important)]
        // [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionStatuses), Summary = "successful operation", Description = "successful response")]
        // [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string), Summary = "Invalid input", Description = "Invalid input")]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Session Statuses not found", Description = "Session Statuses Not Found")]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Summary = "An exception occurred", Description = "An exception occurred.")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions/statuses/{smokerId}/{sessionId}/{timeseries:int?}")] HttpRequestData req,
                string smokerId,
                string sessionId,
                int? timeSeries)
        {
            _log.LogInformation("GetSessionChart triggered");

            if (string.IsNullOrEmpty(smokerId))
            {
                _log.LogError("GetSessionChart: Missing smokerId - url should be /sessions/statuses/{smokerId}/{sessionId}");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Missing required property 'smokerId'." });
                return badResponse;
            }
            if (string.IsNullOrEmpty(sessionId))
            {
                _log.LogError("GetSessionChart: Missing sessionId - url should be /sessions/statuses/{smokerId}/{sessionId}");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Missing required property 'sessionId'." });
                return badResponse;
            }
            if (!timeSeries.HasValue || timeSeries <= 0)
            {
                _log.LogInformation($"GetSessionChart timeSeries not sent or == 0 so setting to 1");
                timeSeries = 1;
            }
            if (timeSeries > 60)
            {
                _log.LogInformation($"GetSessionChart timeSeries > 60 so setting to 60");
                timeSeries = 60;
            }
            try
            {
                var statuses = await _sessionsService.GetSessionChartAsync(sessionId, smokerId, timeSeries);
                if (statuses == null)
                {
                    _log.LogInformation($"GetSessionChart no statuses found");
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    return notFoundResponse;
                }
                _log.LogInformation($"GetSessionChart Numer of statuses = {statuses.Count}");

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                };
                //settings.Converters.Add(new SessionSummariesConverter());
                
                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                okResponse.Headers.Add("Content-Type", JsonContentType);
                await okResponse.WriteStringAsync(JsonConvert.SerializeObject(statuses, settings));
                return okResponse;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "<-- GetSessionChart Unhandled exception");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "An internal server error occurred." });
                return errorResponse;
            }
        }
    }

}
