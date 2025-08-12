using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;


using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Data;

#nullable enable
namespace MeatGeek.Sessions
{
    public class GetSessionChart
    {
        private const string JsonContentType = "application/json";
        private readonly ILogger<GetSessionChart> _log;
        private readonly ISessionsService _sessionsService;
        private readonly CosmosClient _cosmosClient;

        public GetSessionChart(ILogger<GetSessionChart> log, ISessionsService sessionsService, CosmosClient cosmosClient)
        {
            _log = log;
            _sessionsService = sessionsService;
            _cosmosClient = cosmosClient;
        }

        [Function("GetSessionChart")]
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
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { error = "Missing required property 'smokerId'." });
                return errorResponse;
            }
            if (string.IsNullOrEmpty(sessionId))
            {
                _log.LogError("GetSessionChart: Missing sessionId - url should be /sessions/statuses/{smokerId}/{sessionId}");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { error = "Missing required property 'sessionId'." });
                return errorResponse;
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
                var json = JsonConvert.SerializeObject(statuses, settings);

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", JsonContentType);
                await response.WriteStringAsync(json);
                return response;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "<-- GetSessionChart Unhandled exception");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "Internal server error occurred." });
                return errorResponse;
            }
        }
    }

}
