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
using MeatGeek.Sessions.Services.Models.Data;
using MeatGeek.Sessions.Services.Models.Request;
using MeatGeek.Sessions.Services.Models.Results;
using MeatGeek.Sessions.Services.Models.Response;
using MeatGeek.Sessions.Services.Repositories;
using MeatGeek.Shared;

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
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionStatuses), Summary = "successful operation", Description = "successful response")]
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
                    return new NotFoundResult();
                }

                // serialise the summaries using a custom converter
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                };
                //settings.Converters.Add(new SessionSummariesConverter());
                var json = JsonConvert.SerializeObject(statuses);

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
