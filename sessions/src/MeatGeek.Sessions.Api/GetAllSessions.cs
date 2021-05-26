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

#nullable enable
namespace MeatGeek.Sessions
{
   public class GetAllSessions
    {

        private const string JsonContentType = "application/json";
        private readonly ILogger<CreateSession> _log;
        private readonly ISessionsService _sessionsService; 

        public GetAllSessions(ILogger<CreateSession> log, ISessionsService sessionsService)
        {
            _log = log;
            _sessionsService = sessionsService;
        }

        [FunctionName("GetAllSessions")]
        [OpenApiOperation(operationId: "GetAllSessions", tags: new[] { "session" }, Summary = "Returns all sessions", Description = "Returns all sessions. Sessions are cooking / BBQ Sessions or cooks.", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionSummaries), Summary = "successful operation", Description = "successful response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions")] HttpRequest req,
                ILogger log)
        {
            log.LogInformation("GetAllSessions triggered");

            var smokerID = "meatgeek2";
            //TODO: Get SmokerID
            // get the user ID
            // if (! await UserAuthenticationService.GetUserIdAsync(req, out var userId, out var responseResult))
            // {
            //     return responseResult;
            // }

            // list the categories
            try
            {
                var summaries = await _sessionsService.GetSessionsAsync(smokerID);
                if (summaries == null)
                {
                    return new NotFoundResult();
                }

                // serialise the summaries using a custom converter
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                };
                settings.Converters.Add(new SessionSummariesConverter());
                var json = JsonConvert.SerializeObject(summaries, settings);

                return new ContentResult
                {
                    Content = json,
                    ContentType = JsonContentType,
                    StatusCode = StatusCodes.Status200OK
                };
            }
            catch (Exception ex)
            {
                log.LogError("Unhandled exception", ex);
                return new ExceptionResult(ex, false);
            }
        }
    }       

}
