using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Web.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MeatGeek.Sessions
{
    public class DeleteSession
    {
        private readonly ISessionsService _sessionsService; 

        public DeleteSession(ISessionsService sessionsService)
        {
            _sessionsService = sessionsService;
        }

        [FunctionName("DeleteSession")]
        [OpenApiOperation(operationId: "DeleteSession", tags: new[] { "session" }, Summary = "Deletes an existing session.", Description = "Deletes a session (sessions are 'cooks' or BBQ sessions) and all associated status entries related to that session.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "smokerId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "the Smoker Id", Description = "The Smoker Id", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "SessionID", Description = "The ID of the Session to delete (GUID).", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Session deleted as requested.", Description = "Session deleted as requested.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.MethodNotAllowed, Summary = "Invalid input", Description = "Invalid input")]         
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Session not found", Description = "Session Not Found")]         
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid input", Description = "Invalid input")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Summary = "An exception or internal server error occurred", Description = "An exception or internal server error occurred.")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "sessions/{smokerId}/{id}")] HttpRequest req, 
                ILogger log,
                string smokerId,
                string id)
        {
            log.LogInformation("DeleteSession Called");

            if (string.IsNullOrEmpty(smokerId))
            {
                log.LogError("DeleteSession: Missing smokerId - url should be /sessions/{smokerId}/{id}");
                return new BadRequestObjectResult(new { error = "Missing required property 'smokerId'." });
            }

            if (string.IsNullOrEmpty(id))
            {
                log.LogError("SeleteSession: Missing id - url should be /sessions/{smokerId}/{id}");
                return new BadRequestObjectResult(new { error = "Missing required property 'id'." });
            }
     
            try
            {
                log.LogWarning($"BEFORE: _sessionsService.DeleteSessionAsync");
                log.LogInformation("smokerId = " + smokerId);
                log.LogInformation("Session Id = " + id);
                var result = await _sessionsService.DeleteSessionAsync(id, smokerId);
                log.LogWarning($"AFTER: _sessionsService.DeleteSessionAsync");
                if (result == DeleteSessionResult.NotFound)
                {
                    log.LogWarning($"SessionID {id} not found.");
                    return new NotFoundResult();
                }
                log.LogInformation("DeleteSession completing");
                return new NoContentResult();
            }
            catch (Exception ex)
            {
                log.LogError(ex, "DeleteSession: Unhandled exception");
                return new ExceptionResult(ex, false);
            }

        }

    }
}
