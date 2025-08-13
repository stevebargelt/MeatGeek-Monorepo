using System.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
// using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
// using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
// using Microsoft.OpenApi.Models;

using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Request;

namespace MeatGeek.Sessions.Api
{

    public class CreateSession
    {
        private readonly ILogger<CreateSession> _log;
        private const string JsonContentType = "application/json";
        private readonly ISessionsService _sessionsService;

        public CreateSession(ILogger<CreateSession> log, ISessionsService sessionsService)
        {
            _log = log;
            _sessionsService = sessionsService;
        }

        [Function("CreateSession")]
        // [OpenApiOperation(operationId: "CreateSession", tags: new[] { "session" }, Summary = "Start a new session.", Description = "This add a new session (sessions are 'cooks' or BBQ sessions).", Visibility = OpenApiVisibilityType.Important)]
        // [OpenApiParameter(name: "smokerId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "the Smoker Id", Description = "The Smoker Id. ", Visibility = OpenApiVisibilityType.Important)]
        // [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateSessionRequest), Required = true, Description = "Session object that needs to be added to the store")]
        // [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionCreated), Summary = "New session created", Description = "New session created.")]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid input", Description = "Invalid input")]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Summary = "An exception or internal server error has occurred", Description = "An exception or internal server has occurred.")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sessions/{smokerId}")] HttpRequestData req,
            string smokerId)
        {
            _log.LogInformation("CreateSession API Triggered");

            if (string.IsNullOrEmpty(smokerId))
            {
                _log.LogInformation("GetAllSessions: Missing smokerId - url should be /sessions/{smokerId}");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Missing required property 'smokerId'." });
                return badResponse;
            }

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            CreateSessionRequest? newSession;
            try
            {
                newSession = JsonConvert.DeserializeObject<CreateSessionRequest>(requestBody);
            }
            catch (JsonReaderException)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Body should be provided in JSON format." });
                return badResponse;
            }

            if (newSession == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Missing required data to create a session." });
                return badResponse;
            }

            newSession.SmokerId = smokerId;
            if (string.IsNullOrEmpty(newSession.Title))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Missing required property 'title'." });
                return badResponse;
            }

            if (!newSession.StartTime.HasValue)
            {
                newSession.StartTime = DateTime.UtcNow;
            }

            try
            {
                var summaries = await _sessionsService.GetRunningSessionsAsync(smokerId);
                _log.LogInformation($"summaries.Count={summaries.Count}");
                _log.LogInformation($"summaries={summaries}");
                if (summaries != null && summaries.Count > 0)
                {
                    _log.LogError($"CreateSession: Will not create a new session when there is already an active session.");
                    var conflictResponse = req.CreateResponse(HttpStatusCode.Conflict);
                    await conflictResponse.WriteAsJsonAsync(summaries);
                    return conflictResponse;
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "<-- From CreateSession -> GetRunningSessionsAsync Unhandled exception");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "An internal server error occurred." });
                return errorResponse;
            }

            // create session
            try
            {
                _log.LogInformation("BEFORE SessionService Call");
                _log.LogInformation("data.Title = " + newSession.Title);
                _log.LogInformation("data.SmokerId = " + newSession.SmokerId);
                _log.LogInformation("data.StartTime = " + newSession.StartTime.Value);
                var sessionId = await _sessionsService.AddSessionAsync(newSession.Title, newSession.Description, newSession.SmokerId, newSession.StartTime.Value);
                
                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                await okResponse.WriteAsJsonAsync(new { sessionId = sessionId });
                return okResponse;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "<-- Exception from CreateSession");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "An internal server error occurred." });
                return errorResponse;
            }
        }

    }
}
