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


using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Data;

#nullable enable
namespace MeatGeek.Sessions
{
   public class GetAllSessionStatuses
    {

        private const string JsonContentType = "application/json";
        private readonly ILogger<CreateSession> _log;
        private readonly ISessionsService _sessionsService; 

        public GetAllSessionStatuses(ILogger<CreateSession> log, ISessionsService sessionsService)
        {
            _log = log;
            _sessionsService = sessionsService;
        }

        [FunctionName("GetAllSessionStatuses")]
        [OpenApiOperation(operationId: "GetAllSessionStatuses", tags: new[] { "sessionstatus" }, Summary = "Returns all session statuses", Description = "Returns all statues for a given session.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "smokerid", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "The ID of the Smoker the session belings to", Description = "The ID of the Smoker the session belings to", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "ID of the Session to return", Description = "The ID of the session to return", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionStatuses), Summary = "successful operation", Description = "successful response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string), Summary = "Invalid input", Description = "Invalid input")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Session Statuses not found", Description = "Session Statuses Not Found")]         
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Summary = "An exception occurred", Description = "An exception occurred.")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions/statuses/{smokerId}/{sessionId}")] HttpRequest req,
                string smokerId, 
                string sessionId,
                ILogger log)
        {
            log.LogInformation("GetAllSessionStatuses triggered");

            if (string.IsNullOrEmpty(smokerId))
            {
                _log.LogError("GetAllSessionStatuses: Missing smokerId - url should be /sessions/statuses/{smokerId}/{sessionId}");
                return new BadRequestObjectResult(new { error = "Missing required property 'smokerId'." });
            }
            if (string.IsNullOrEmpty(sessionId))
            {
                _log.LogError("GetAllSessionStatuses: Missing sessionId - url should be /sessions/statuses/{smokerId}/{sessionId}");
                return new BadRequestObjectResult(new { error = "Missing required property 'sessionId'." });
            }

            try
            {
                var statuses = await _sessionsService.GetSessionStatusesAsync(sessionId, smokerId);
                if (statuses == null)
                {
                    _log.LogInformation($"GetAllSessionStatuses no statuses found");
                    return new NotFoundResult();
                }
                _log.LogInformation($"GetAllSessionStatuses Numer of statuses = {statuses.Count}");
                
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                };
                //settings.Converters.Add(new SessionSummariesConverter());
                var json = JsonConvert.SerializeObject(statuses, settings);

                return new ContentResult
                {
                    Content = json,
                    ContentType = JsonContentType,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                log.LogError(ex, "<-- GetAllSessionStatuses Unhandled exception");
                return new ExceptionResult(ex, false);
            }
        }
    }       

}
