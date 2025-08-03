using System;
using System.IO;
using System.Net;
using System.Web.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

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

        [FunctionName("CreateSession")]
        [OpenApiOperation(operationId: "CreateSession", tags: new[] { "session" }, Summary = "Start a new session.", Description = "This add a new session (sessions are 'cooks' or BBQ sessions).", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "smokerId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "the Smoker Id", Description = "The Smoker Id. ", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateSessionRequest), Required = true, Description = "Session object that needs to be added to the store")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionCreated), Summary = "New session created", Description = "New session created.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid input", Description = "Invalid input")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Summary = "An exception or internal server error has occurred", Description = "An exception or internal server has occurred.")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sessions/{smokerId}")] HttpRequest req,
            string smokerId)
        {
            _log.LogInformation("CreateSession API Triggered");

            if (string.IsNullOrEmpty(smokerId))
            {
                _log.LogError("CreateSession: Missing smokerId - url should be /sessions/{smokerId}");
                return new BadRequestObjectResult(new { error = "Missing required property 'smokerId'." });
            }

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            CreateSessionRequest newSession;
            try
            {
                newSession = JsonConvert.DeserializeObject<CreateSessionRequest>(requestBody);
            }
            catch (JsonReaderException)
            {
                return new BadRequestObjectResult(new { error = "Body should be provided in JSON format." });
            }

            newSession.SmokerId = smokerId;

            // validate request
            if (newSession == null || string.IsNullOrEmpty(newSession.Title))
            {
                return new BadRequestObjectResult(new { error = "Missing required property 'title'." });
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
                    return new ConflictObjectResult(summaries);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "<-- From CreateSession -> GetRunningSessionsAsync Unhandled exception");
                return new ExceptionResult(ex, false);
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
                return new OkObjectResult(new { id = sessionId });
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "<-- Exception from CreateSession");
                return new ExceptionResult(ex, false);
            }
        }

    }
}
