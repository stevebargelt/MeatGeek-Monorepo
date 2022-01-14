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
        private readonly ILogger<EndSession> _log;
        private readonly ISessionsService _sessionsService; 

        public EndSession(ILogger<EndSession> log, ISessionsService sessionsService)
        {
            _log = log;
            _sessionsService = sessionsService;
        }

        [FunctionName("EndSession")]
        [OpenApiOperation(operationId: "EndSession", tags: new[] { "session" }, Summary = "Ends an existing session.", Description = "Ends a session (sessions are 'cooks' or BBQ sessions).", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SessionDetails), Required = true, Description = "Session object with EndTime")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionDetails), Summary = "Session ended.", Description = "Session Ended")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.MethodNotAllowed, Summary = "Invalid input", Description = "Invalid input")]         
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", "put", Route = "endsession/{smokerId}/{id}")] HttpRequest req, 
                ILogger log,
                string smokerId,
                string id)
        {
            log.LogInformation("EndSession Called");

            if (string.IsNullOrEmpty(smokerId))
            {
                _log.LogError("EndSession: Missing smokerId - url should be /endsession/{smokerId}/{id}");
                return new BadRequestObjectResult(new { error = "Missing required property 'smokerId'." });
            }

            if (string.IsNullOrEmpty(id))
            {
                _log.LogError("EndSession: Missing id - url should be /endsession/{smokerId}/{id}");
                return new BadRequestObjectResult(new { error = "Missing required property 'id'." });
            }

            var updateData = new EndSessionRequest {};
            updateData.SmokerId = smokerId;
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                updateData.EndTime = DateTime.UtcNow;
                _log.LogInformation($"No JSON body, EndTime not provided using current time: {updateData.EndTime}");           
            }
            else 
            {
                JObject data;
                try
                {
                    data = JObject.Parse(requestBody);
                }
                catch (JsonReaderException)
                {
                    _log.LogWarning("EndSession: Could not parse JSON");
                    return new BadRequestObjectResult(new { error = "Body should be provided in JSON format." });
                }
                log.LogInformation("Made it past data = JObject.Parse(requestBody)");
                if (!data.HasValues)
                {
                    updateData.EndTime = DateTime.UtcNow;
                    _log.LogInformation($"EndTime not provided using current time: {updateData.EndTime}");
                }
                else 
                {
                    JToken endTimeToken = data["endTime"];
                    _log.LogInformation($"endTimeToken Type = {endTimeToken.Type}");
                    if (endTimeToken != null && endTimeToken.Type == JTokenType.Date)
                    {
                        _log.LogInformation($"endTime= {endTimeToken.ToString()}");
                        try 
                        {                                   
                            DateTimeOffset dto = DateTimeOffset.Parse(endTimeToken.ToString());
                            updateData.EndTime = dto.UtcDateTime;
                        }
                        catch(ArgumentNullException argNullEx)
                        {
                            _log.LogError(argNullEx, $"Argument NUll exception");
                            throw argNullEx;
                        }
                        catch(ArgumentException argEx)
                        {
                            _log.LogError(argEx, $"Argument exception");
                            throw argEx;
                        }                
                        catch(FormatException formatEx)
                        {
                            _log.LogError(formatEx, $"Format exception");
                            throw formatEx;
                        }
                        catch(Exception ex)
                        {
                            _log.LogError(ex, $"Unhandled Exception from DateTimeParse");
                            throw ex;
                        }
                        log.LogInformation($"EndTime will be updated to {updateData.EndTime.ToString()}");
                    }
                    else
                    {
                        updateData.EndTime = DateTime.UtcNow;
                        _log.LogInformation($"EndTime not provided using current time: {updateData.EndTime}");
                    }
                }
            }
            try
            {
                _log.LogWarning($"BEFORE: _sessionsService.EndSessionAsync");
                var result = await _sessionsService.EndSessionAsync(id, updateData.SmokerId, updateData.EndTime);
                if (result == EndSessionResult.NotFound)
                {
                    _log.LogWarning($"SessionID {id} not found.");
                    return new NotFoundResult();
                }
                _log.LogInformation("EndSession completing");
                return new NoContentResult();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "EndSession: Unhandled exception");
                return new ExceptionResult(ex, false);
            }

        }

    }
}
