using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using MeatGeek.Sessions.Services.Models;
using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Request;
using MeatGeek.Sessions.Services.Models.Results;
using MeatGeek.Sessions.Services.Models.Response;
using MeatGeek.Sessions.Services.Repositories;
using MeatGeek.Shared;

namespace MeatGeek.Sessions
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
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sessions/{smokerId}")] HttpRequestData req,
            string smokerId)
        {
            _log.LogInformation("CreateSession API Triggered");

            if (string.IsNullOrEmpty(smokerId))
            {
                _log.LogError("CreateSession: Missing smokerId - url should be /sessions/{smokerId}");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { error = "Missing required property 'smokerId'." });
                return errorResponse;
            }

            var requestBody = await req.ReadAsStringAsync();
            CreateSessionRequest newSession;
            try
            {
                newSession = JsonConvert.DeserializeObject<CreateSessionRequest>(requestBody);
            }
            catch (JsonReaderException)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { error = "Body should be provided in JSON format." });
                return errorResponse;
            }

            newSession.SmokerId = smokerId;

            // validate request
            if (newSession == null || string.IsNullOrEmpty(newSession.Title))
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { error = "Missing required property 'title'." });
                return errorResponse;
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
                await errorResponse.WriteAsJsonAsync(new { error = "Internal server error occurred." });
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
                _log.LogInformation("AFTER SessionService Call");
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { id = sessionId });
                return response;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "<-- Exception from CreateSession");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "Internal server error occurred." });
                return errorResponse;
            }
        }

    }
}
