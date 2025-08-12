using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

using MeatGeek.Sessions.Services.Models;
using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Request;
using MeatGeek.Sessions.Services.Models.Results;
using MeatGeek.Sessions.Services.Models.Response;
using MeatGeek.Sessions.Services.Repositories;
using MeatGeek.Shared;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MeatGeek.Sessions
{
    public class DeleteSession
    {
        private readonly ILogger<DeleteSession> _log;
        private readonly ISessionsService _sessionsService;

        public DeleteSession(ILogger<DeleteSession> log, ISessionsService sessionsService)
        {
            _log = log;
            _sessionsService = sessionsService;
        }

        [Function("DeleteSession")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "sessions/{smokerId}/{id}")] HttpRequestData req,
                string smokerId,
                string id)
        {
            _log.LogInformation("DeleteSession Called");

            if (string.IsNullOrEmpty(smokerId))
            {
                _log.LogError("DeleteSession: Missing smokerId - url should be /sessions/{smokerId}/{id}");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { error = "Missing required property 'smokerId'." });
                return errorResponse;
            }

            if (string.IsNullOrEmpty(id))
            {
                _log.LogError("DeleteSession: Missing id - url should be /sessions/{smokerId}/{id}");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { error = "Missing required property 'id'." });
                return errorResponse;
            }

            try
            {
                _log.LogWarning($"BEFORE: _sessionsService.DeleteSessionAsync");
                _log.LogInformation("smokerId = " + smokerId);
                _log.LogInformation("Session Id = " + id);
                var result = await _sessionsService.DeleteSessionAsync(id, smokerId);
                _log.LogWarning($"AFTER: _sessionsService.DeleteSessionAsync");
                if (result == DeleteSessionResult.NotFound)
                {
                    _log.LogWarning($"SessionID {id} not found.");
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    return notFoundResponse;
                }
                _log.LogInformation("DeleteSession completing");
                var response = req.CreateResponse(HttpStatusCode.NoContent);
                return response;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "DeleteSession: Unhandled exception");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "Internal server error occurred." });
                return errorResponse;
            }

        }

    }
}
