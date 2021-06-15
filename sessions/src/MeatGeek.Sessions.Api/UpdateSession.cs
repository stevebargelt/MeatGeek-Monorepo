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
    public class UpdateSession
    {
        private readonly ILogger<CreateSession> _log;
        private readonly ISessionsService _sessionsService; 

        public UpdateSession(ILogger<CreateSession> log, ISessionsService sessionsService)
        {
            _log = log;
            _sessionsService = sessionsService;
        }

        [FunctionName("UpdateSession")]
        [OpenApiOperation(operationId: "UpdateSession", tags: new[] { "session" }, Summary = "Updated an existing session.", Description = "Updates a session (sessions are 'cooks' or BBQ sessions).", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SessionDetails), Required = true, Description = "Session object with updated values")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionDetails), Summary = "Session dtails updated", Description = "Session details updated")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.MethodNotAllowed, Summary = "Invalid input", Description = "Invalid input")]         
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", "put", Route = "sessions/{id}")] HttpRequest req, 
                ILogger log,
                string id)
        {
            log.LogInformation("UpdateSession Called");

            // get the request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updateData = new UpdateSessionRequest {};
            JObject data;
            try
            {
                data = JObject.Parse(requestBody);
            }
            catch (JsonReaderException)
            {
                log.LogWarning("UpdateSession: Could not parse JSON");
                return new BadRequestObjectResult(new { error = "Body should be provided in JSON format." });
            }
            log.LogInformation("Made it past data = JObject.Parse(requestBody)");
            if (!data.HasValues)
            {
                log.LogWarning("UpdateSession: data has no values.");
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
                log.LogWarning("UpdateSession: data has no smokerId.");
                return new BadRequestObjectResult(new { error = "Missing required property: smokerId is REQUIRED." });
            }
            JToken titleToken = data["title"];
            if (titleToken != null && titleToken.Type == JTokenType.String && titleToken.ToString() != String.Empty)
            {
                updateData.Title = titleToken.ToString();
                log.LogInformation($"Title will be updated to {updateData.Title}");
            }
            else
            {
                log.LogInformation($"Title will NOT be updated.");
            }
            JToken descriptionToken = data["description"];
            if (descriptionToken != null && descriptionToken.Type == JTokenType.String && descriptionToken.ToString() != String.Empty)
            {
                updateData.Description = descriptionToken.ToString();
                log.LogInformation($"Description will be updated to {updateData.Description}");
            }
            else
            {
                log.LogInformation($"Description will NOT be updated");
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
                log.LogInformation($"EndTime will NOT be updated.");
            }
            try
            {
                log.LogWarning($"BEFORE: _sessionsService.UpdateSessionAsync");
                var result = await _sessionsService.UpdateSessionAsync(id, updateData.SmokerId, updateData.Title, updateData.Description, updateData.EndTime);
                if (result == UpdateSessionResult.NotFound)
                {
                    log.LogWarning($"SessionID {id} not found.");
                    return new NotFoundResult();
                }
                log.LogInformation("UpdateSession completing");
                return new NoContentResult();
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Unhandled exception");
                return new ExceptionResult(ex, false);
            }

        }

    }
}
