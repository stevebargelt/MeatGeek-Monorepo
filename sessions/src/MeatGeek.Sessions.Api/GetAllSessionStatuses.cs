using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;


using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Data;

#nullable enable
namespace MeatGeek.Sessions
{
    public class GetAllSessionStatuses
    {

        private const string JsonContentType = "application/json";
        private readonly ILogger<GetAllSessionStatuses> _log;
        private readonly ISessionsService _sessionsService;
        private readonly CosmosClient _cosmosClient;

        public GetAllSessionStatuses(ILogger<GetAllSessionStatuses> log, ISessionsService sessionsService, CosmosClient cosmosClient)
        {
            _log = log;
            _sessionsService = sessionsService;
            _cosmosClient = cosmosClient;
        }

        [Function("GetAllSessionStatuses")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions/statuses/{smokerId}/{sessionId}")] HttpRequestData req,
                string smokerId,
                string sessionId)
        {
            _log.LogInformation("GetAllSessionStatuses triggered");

            if (string.IsNullOrEmpty(smokerId))
            {
                _log.LogError("GetAllSessionStatuses: Missing smokerId - url should be /sessions/statuses/{smokerId}/{sessionId}");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { error = "Missing required property 'smokerId'." });
                return errorResponse;
            }
            if (string.IsNullOrEmpty(sessionId))
            {
                _log.LogError("GetAllSessionStatuses: Missing sessionId - url should be /sessions/statuses/{smokerId}/{sessionId}");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { error = "Missing required property 'sessionId'." });
                return errorResponse;
            }

            try
            {
                var statuses = await _sessionsService.GetSessionStatusesAsync(sessionId, smokerId);
                if (statuses == null)
                {
                    _log.LogInformation($"GetAllSessionStatuses no statuses found");
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    return notFoundResponse;
                }
                _log.LogInformation($"GetAllSessionStatuses Numer of statuses = {statuses.Count}");

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
                _log.LogError(ex, "<-- GetAllSessionStatuses Unhandled exception");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "Internal server error occurred." });
                return errorResponse;
            }
        }
    }

}
