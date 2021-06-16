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


using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models;
using MeatGeek.Sessions.Services.Converters;
using MeatGeek.Sessions.Services.Models.Request;
using MeatGeek.Sessions.Services.Models.Results;
using MeatGeek.Sessions.Services.Models.Response;
using MeatGeek.Sessions.Services.Repositories;
using MeatGeek.Shared;

using Microsoft.OpenApi.Models;


namespace MeatGeek.Sessions
{
    public class GetSessionById
    {

        private const string JsonContentType = "application/json";
        private readonly ILogger<CreateSession> _log;
        private readonly ISessionsService _sessionsService; 

        public GetSessionById(ILogger<CreateSession> log, ISessionsService sessionsService)
        {
            _log = log;
            _sessionsService = sessionsService;
        }

        [FunctionName("GetSessionById")]
        [OpenApiOperation(operationId: "GetSessionById", tags: new[] { "session" }, Summary = "Find Session by ID", Description = "Returns a single session. Sessions are cooking / BBQ Sessions or cooks.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "snokerid", In = ParameterLocation.Query, Required = true, Type = typeof(string), Summary = "The ID of the Smoker the session belings to", Description = "The ID of the Smoker the session belings to", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "id", In = ParameterLocation.Query, Required = true, Type = typeof(string), Summary = "ID of the Session to return", Description = "The ID of the session to return", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionDetails), Summary = "successful operation", Description = "successful response")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid ID supplied", Description = "Invalid ID supplied")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Session not found", Description = "Session not found")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions/{smokerId}/{id}")] HttpRequest req, 
            string smokerId,
            string id,
            ILogger log)
        {
            log.LogInformation("GetSessionById triggered");

            if (string.IsNullOrEmpty(smokerId))
            {
                _log.LogError("GetSessionById: Missing smokerId - url should be /sessions/{smokerId}/{id}");
                return new BadRequestObjectResult(new { error = "Missing required property 'smokerId'." });
            }

            if (string.IsNullOrEmpty(id))
            {
                _log.LogError("GetSessionById: Missing id - url should be /sessions/{smokerId}/{id}");
                return new BadRequestObjectResult(new { error = "Missing required property 'smokerId'." });
            }

            try
            {
                var document = await _sessionsService.GetSessionAsync(id, smokerId);
                if (document == null)
                {
                    return new NotFoundResult();
                }

                return new OkObjectResult(document);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "<-- GetSessionById Unhandled exception");
                return new ExceptionResult(ex, false);
            }

        }

    }
}