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
    public class EndSession
    {
        private readonly ISessionsService _sessionsService; 

        public EndSession(ISessionsService sessionsService)
        {
            _sessionsService = sessionsService;
        }

        [FunctionName("EndSession")]
        [OpenApiOperation(operationId: "EndSession", tags: new[] { "session" }, Summary = "Ends an existing session.", Description = "Ends a session (sessions are 'cooks' or BBQ sessions).", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "smokerId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "the Smoker Id", Description = "The Smoker Id", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "SessionID", Description = "The ID of the Session to end (GUID).", Visibility = OpenApiVisibilityType.Important)]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Session ended as requested.", Description = "Session Ended as requested.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.MethodNotAllowed, Summary = "Invalid input", Description = "Invalid input")]         
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Session not found", Description = "Session Not Found")]         
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid input", Description = "Invalid input")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Summary = "An exception or internal server error occurred", Description = "An exception or internal server error occurred.")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", "put", Route = "endsession/{smokerId}/{id}")] HttpRequest req, 
                ILogger log,
                string smokerId,
                string id)
        {
            log.LogInformation("EndSession Called");

            if (string.IsNullOrEmpty(smokerId))
            {
                log.LogError("EndSession: Missing smokerId - url should be /endsession/{smokerId}/{id}");
                return new BadRequestObjectResult(new { error = "Missing required property 'smokerId'." });
            }

            if (string.IsNullOrEmpty(id))
            {
                log.LogError("EndSession: Missing id - url should be /endsession/{smokerId}/{id}");
                return new BadRequestObjectResult(new { error = "Missing required property 'id'." });
            }

            var updateData = new EndSessionRequest {};
            updateData.SmokerId = smokerId;
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                updateData.EndTime = DateTime.UtcNow;
                log.LogInformation($"No JSON body, EndTime not provided using current time: {updateData.EndTime}");           
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
                    log.LogWarning("EndSession: Could not parse JSON");
                    return new BadRequestObjectResult(new { error = "Body should be provided in JSON format." });
                }
                log.LogInformation("Made it past data = JObject.Parse(requestBody)");
                if (!data.HasValues)
                {
                    updateData.EndTime = DateTime.UtcNow;
                    log.LogInformation($"EndTime not provided using current time: {updateData.EndTime}");
                }
                else 
                {
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
                }
            }
            try
            {
                log.LogWarning($"BEFORE: _sessionsService.EndSessionAsync");
                log.LogInformation("updateData.SmokerId = " + updateData.SmokerId);
                log.LogInformation("updateData.StartTime = " +updateData.EndTime.Value);
                var result = await _sessionsService.EndSessionAsync(id, updateData.SmokerId, updateData.EndTime.Value);
                log.LogWarning($"AFTER: _sessionsService.EndSessionAsync");
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
