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
    public class EndSession
    {
        private readonly ILogger<CreateSession> _log;
        private readonly ISessionsService _sessionsService; 

        public EndSession(ILogger<CreateSession> log, ISessionsService sessionsService)
        {
            _log = log;
            _sessionsService = sessionsService;
        }

        [FunctionName("EndSession")]
        [OpenApiOperation(operationId: "EndSession", tags: new[] { "session" }, Summary = "Ends an existing session.", Description = "Ends a session (sessions are 'cooks' or BBQ sessions).", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SessionDetails), Required = true, Description = "Session object with optional EndTime")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionDetails), Summary = "Session ended.", Description = "Session Ended")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.MethodNotAllowed, Summary = "Invalid input", Description = "Invalid input")]         
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", "put", Route = "endsession/{id}")] HttpRequest req, 
                ILogger log,
                string id)
        {
            log.LogInformation("EndSession Called");

            // get the request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updateData = new EndSessionRequest {};
            JObject data;
            try
            {
                data = JObject.Parse(requestBody);
            }
            catch (JsonReaderException)
            {
                log.LogWarning("EndSession: Could not parse JSON");
                return new BadRequestObjectResult(new { error = "Body should be provided in JSON format." });
            }
            log.LogInformation("Made it past data = JObject.Parse(requestBody)");
            if (!data.HasValues)
            {
                log.LogWarning("EndSession: data has no values.");
                return new BadRequestObjectResult(new { error = "Missing required properties. Nothing to update." });
            }
            JToken smokerIdToken = data["smokerId"];
            if (smokerIdToken != null && smokerIdToken.Type == JTokenType.String && smokerIdToken.ToString() != String.Empty)
            {
                updateData.SmokerId = smokerIdToken.ToString();
                log.LogInformation($"SmokerId = {updateData.SmokerId}");
            }
            else
            {
                log.LogWarning("EndSession: data has no smokerId.");
                return new BadRequestObjectResult(new { error = "Missing required property: smokerId is REQUIRED." });
            }
            JToken endTimeToken = data["endTime"];
            log.LogInformation($"endTimeToken Type = {endTimeToken.Type}");
            if (endTimeToken != null && endTimeToken.Type == JTokenType.Date)
            {
                log.LogInformation($"endTime= {endTimeToken.ToString()}");
                try 
                {                                   
                    DateTimeOffset dto = DateTimeOffset.Parse(endTimeToken.ToString());
                    updateData.EndTime = dto.UtcDateTime;
                }
                catch(ArgumentNullException argNullEx)
                {
                    log.LogError(argNullEx, $"Argument NUll exception");
                    throw argNullEx;
                }
                catch(ArgumentException argEx)
                {
                    log.LogError(argEx, $"Argument exception");
                    throw argEx;
                }                
                catch(FormatException formatEx)
                {
                    log.LogError(formatEx, $"Format exception");
                    throw formatEx;
                }
                catch(Exception ex)
                {
                    log.LogError(ex, $"Unhandled Exception from DateTimeParse");
                    throw ex;
                }
                log.LogInformation($"EndTime will be updated to {updateData.EndTime.ToString()}");
            }
            else
            {
                updateData.EndTime = DateTime.UtcNow;
                log.LogInformation($"EndTime not provided using current time: {updateData.EndTime}");
            }
            try
            {
                log.LogWarning($"BEFORE: _sessionsService.EndSessionAsync");
                var result = await _sessionsService.EndSessionAsync(id, updateData.SmokerId, updateData.EndTime);
                if (result == EndSessionResult.NotFound)
                {
                    log.LogWarning($"SessionID {id} not found.");
                    return new NotFoundResult();
                }
                log.LogInformation("EndSession completing");
                return new NoContentResult();
            }
            catch (Exception ex)
            {
                log.LogError(ex, "EndSession: Unhandled exception");
                return new ExceptionResult(ex, false);
            }

        }

    }
}
