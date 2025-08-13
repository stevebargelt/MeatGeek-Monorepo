using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
// using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
// using Microsoft.OpenApi.Models;

using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Request;
using MeatGeek.Sessions.Services.Models.Results;

namespace MeatGeek.Sessions.Api
{
    public class EndSession
    {
        private readonly ISessionsService _sessionsService;
        private readonly ILogger<EndSession> _log;

        public EndSession(ISessionsService sessionsService, ILogger<EndSession> log)
        {
            _sessionsService = sessionsService;
            _log = log;
        }

        [Function("EndSession")]
        // [OpenApiOperation(operationId: "EndSession", tags: new[] { "session" }, Summary = "Ends an existing session.", Description = "Ends a session (sessions are 'cooks' or BBQ sessions).", Visibility = OpenApiVisibilityType.Important)]
        // [OpenApiParameter(name: "smokerId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "the Smoker Id", Description = "The Smoker Id", Visibility = OpenApiVisibilityType.Important)]
        // [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "SessionID", Description = "The ID of the Session to end (GUID).", Visibility = OpenApiVisibilityType.Important)]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Summary = "Session ended as requested.", Description = "Session Ended as requested.")]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.MethodNotAllowed, Summary = "Invalid input", Description = "Invalid input")]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Session not found", Description = "Session Not Found")]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid input", Description = "Invalid input")]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Summary = "An exception or internal server error occurred", Description = "An exception or internal server error occurred.")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", "put", Route = "endsession/{smokerId}/{id}")] HttpRequestData req,
                string smokerId,
                string id)
        {
            _log.LogInformation("EndSession Called");

            if (string.IsNullOrEmpty(smokerId))
            {
                _log.LogError("EndSession: Missing smokerId - url should be /endsession/{smokerId}/{id}");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Missing required property 'smokerId'." });
                return badResponse;
            }

            if (string.IsNullOrEmpty(id))
            {
                _log.LogError("EndSession: Missing id - url should be /endsession/{smokerId}/{id}");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Missing required property 'id'." });
                return badResponse;
            }

            var updateData = new EndSessionRequest { };
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
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { error = "Body should be provided in JSON format." });
                    return badResponse;
                }
                _log.LogInformation("Made it past data = JObject.Parse(requestBody)");
                if (!data.HasValues)
                {
                    updateData.EndTime = DateTime.UtcNow;
                    _log.LogInformation($"EndTime not provided using current time: {updateData.EndTime}");
                }
                else
                {
                    JToken? endTimeToken = data["endTime"];
                    _log.LogInformation($"endTimeToken Type = {endTimeToken?.Type}");
                    if (endTimeToken != null && (endTimeToken.Type == JTokenType.Date || endTimeToken.Type == JTokenType.String))
                    {
                        _log.LogInformation($"endTime= {endTimeToken.ToString()}");
                        try
                        {
                            DateTimeOffset dto = DateTimeOffset.Parse(endTimeToken.ToString(), null, DateTimeStyles.RoundtripKind);
                            updateData.EndTime = dto.UtcDateTime;
                        }
                        catch (ArgumentNullException argNullEx)
                        {
                            _log.LogError(argNullEx, $"Argument NUll exception");
                            updateData.EndTime = DateTime.UtcNow;
                            _log.LogInformation($"Failed to parse endTime, using current time: {updateData.EndTime}");
                        }
                        catch (ArgumentException argEx)
                        {
                            _log.LogError(argEx, $"Argument exception");
                            updateData.EndTime = DateTime.UtcNow;
                            _log.LogInformation($"Failed to parse endTime, using current time: {updateData.EndTime}");
                        }
                        catch (FormatException formatEx)
                        {
                            _log.LogError(formatEx, $"Format exception");
                            updateData.EndTime = DateTime.UtcNow;
                            _log.LogInformation($"Failed to parse endTime, using current time: {updateData.EndTime}");
                        }
                        catch (Exception ex)
                        {
                            _log.LogError(ex, $"Unhandled Exception from DateTimeParse");
                            updateData.EndTime = DateTime.UtcNow;
                            _log.LogInformation($"Failed to parse endTime, using current time: {updateData.EndTime}");
                        }
                        _log.LogInformation($"EndTime will be updated to {updateData.EndTime.ToString()}");
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
                _log.LogInformation("updateData.SmokerId = " + updateData.SmokerId);
                _log.LogInformation("updateData.EndTime = " + updateData.EndTime.Value);
                
                var result = await _sessionsService.EndSessionAsync(id, smokerId, updateData.EndTime.Value);
                _log.LogWarning($"AFTER: _sessionsService.EndSessionAsync");
                
                if (result == EndSessionResult.NotFound)
                {
                    _log.LogWarning($"SessionID {id} not found.");
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    return notFoundResponse;
                }
                
                _log.LogInformation("EndSession completing");
                var successResponse = req.CreateResponse(HttpStatusCode.NoContent);
                return successResponse;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "EndSession: Unhandled exception");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "An internal server error occurred." });
                return errorResponse;
            }

        }

    }
}
