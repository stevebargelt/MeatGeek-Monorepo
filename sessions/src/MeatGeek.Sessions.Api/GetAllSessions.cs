using System.Net;
// using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
// using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;

using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Converters;
// using MeatGeek.Sessions.Services.Models.Response;

#nullable enable
namespace MeatGeek.Sessions.Api
{
    public class GetAllSessions
    {
        private const string JsonContentType = "application/json";
        private readonly ILogger<GetAllSessions> _log;
        private readonly ISessionsService _sessionsService;

        public GetAllSessions(ILogger<GetAllSessions> log, ISessionsService sessionsService)
        {
            _log = log;
            _sessionsService = sessionsService;
        }

        [Function("GetAllSessions")]
        // [OpenApiOperation(operationId: "GetAllSessions", tags: new[] { "session" }, Summary = "Returns all sessions", Description = "Returns all sessions. Sessions are cooking / BBQ Sessions or cooks.", Visibility = OpenApiVisibilityType.Important)]
        // [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionSummaries), Summary = "successful operation", Description = "successful response")]
        // [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string), Summary = "Invalid input", Description = "Invalid input")]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Session not found", Description = "Session Not Found")]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Summary = "An exception occurred", Description = "An exception occurred.")]
       public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions/{smokerId}")] HttpRequestData req,
                string smokerId)
        {
            _log.LogInformation("GetAllSessions triggered");

            if (string.IsNullOrEmpty(smokerId))
            {
                _log.LogInformation("GetAllSessions: Missing smokerId - url should be /sessions/{smokerId}");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Missing required property 'smokerId'." });
                return badResponse;
            }

            try
            {
                var summaries = await _sessionsService.GetSessionsAsync(smokerId);
                if (summaries == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteAsJsonAsync(new { error = "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!Sessions not found." });
                    return notFoundResponse;
                }

                // serialise the summaries using a custom converter
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                };
                settings.Converters.Add(new SessionSummariesConverter());
                var json = JsonConvert.SerializeObject(summaries, settings);

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", JsonContentType);
                await response.WriteStringAsync(json);
                return response;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "<-- GetAllSessions Unhandled exception");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "An internal server error occurred." });
                return errorResponse;
            }
        }
    }

}
