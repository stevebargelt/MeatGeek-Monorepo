using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
// using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
// using Microsoft.OpenApi.Models;


using MeatGeek.Sessions.Services;
// using MeatGeek.Sessions.Services.Models.Data;

#nullable enable
namespace MeatGeek.Sessions.Api
{
    public class GetAllSessionStatuses
    {

        private const string JsonContentType = "application/json";
        private readonly ILogger<GetAllSessionStatuses> _log;
        private readonly ISessionsService _sessionsService;

        public GetAllSessionStatuses(ILogger<GetAllSessionStatuses> log, ISessionsService sessionsService)
        {
            _log = log;
            _sessionsService = sessionsService;
        }

        [Function("GetAllSessionStatuses")]
        // [OpenApiOperation(operationId: "GetAllSessionStatuses", tags: new[] { "Session Status" }, Summary = "Returns all session statuses", Description = "Returns all statues for a given session.", Visibility = OpenApiVisibilityType.Important)]
        // [OpenApiParameter(name: "smokerid", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "The ID of the Smoker the session belings to", Description = "The ID of the Smoker the session belings to", Visibility = OpenApiVisibilityType.Important)]
        // [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "ID of the Session to return", Description = "The ID of the session to return", Visibility = OpenApiVisibilityType.Important)]
        // [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionStatuses), Summary = "successful operation", Description = "successful response")]
        // [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string), Summary = "Invalid input", Description = "Invalid input")]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Session Statuses not found", Description = "Session Statuses Not Found")]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Summary = "An exception occurred", Description = "An exception occurred.")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions/statuses/{smokerId}/{sessionId}")] HttpRequestData req,
                string smokerId,
                string sessionId)
        {
            _log.LogInformation("GetAllSessionStatuses triggered");

            if (string.IsNullOrEmpty(smokerId))
            {
                _log.LogError("GetAllSessionStatuses: Missing smokerId - url should be /sessions/statuses/{smokerId}/{sessionId}");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Missing required property 'smokerId'." });
                return badResponse;
            }
            if (string.IsNullOrEmpty(sessionId))
            {
                _log.LogError("GetAllSessionStatuses: Missing sessionId - url should be /sessions/statuses/{smokerId}/{sessionId}");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Missing required property 'sessionId'." });
                return badResponse;
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
                
                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                okResponse.Headers.Add("Content-Type", JsonContentType);
                await okResponse.WriteStringAsync(JsonConvert.SerializeObject(statuses, settings));
                return okResponse;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "<-- GetAllSessionStatuses Unhandled exception");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "An internal server error occurred." });
                return errorResponse;
            }
        }
    }

}
