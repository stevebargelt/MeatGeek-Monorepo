using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<EndSession> __log;
        private readonly ISessionsService _sessionsService;

        public EndSession(ILogger<EndSession> _log, ISessionsService sessionsService)
        {
            __log = _log;
            _sessionsService = sessionsService;
        }

        [Function("EndSession")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", "put", Route = "endsession/{smokerId}/{id}")] HttpRequestData req,
                string smokerId,
                string id)
        {
            __log.LogInformation("EndSession Called");

            if (string.IsNullOrEmpty(smokerId))
            {
                __log.LogError("EndSession: Missing smokerId - url should be /endsession/{smokerId}/{id}");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { error = "Missing required property 'smokerId'." });
                return errorResponse;
            }

            if (string.IsNullOrEmpty(id))
            {
                __log.LogError("EndSession: Missing id - url should be /endsession/{smokerId}/{id}");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { error = "Missing required property 'id'." });
                return errorResponse;
            }

            var updateData = new EndSessionRequest { };
            updateData.SmokerId = smokerId;
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                updateData.EndTime = DateTime.UtcNow;
                __log.LogInformation($"No JSON body, EndTime not provided using current time: {updateData.EndTime}");
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
                    __log.LogWarning("EndSession: Could not parse JSON");
                    var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await errorResponse.WriteAsJsonAsync(new { error = "Body should be provided in JSON format." });
                    return errorResponse;
                }
                __log.LogInformation("Made it past data = JObject.Parse(requestBody)");
                if (!data.HasValues)
                {
                    updateData.EndTime = DateTime.UtcNow;
                    __log.LogInformation($"EndTime not provided using current time: {updateData.EndTime}");
                }
                else
                {
                    JToken endTimeToken = data["endTime"];
                    _log.LogInformation($"endTimeToken Type = {endTimeToken.Type}");
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
                _log.LogInformation("updateData.StartTime = " + updateData.EndTime.Value);
                var result = await _sessionsService.EndSessionAsync(id, updateData.SmokerId, updateData.EndTime.Value);
                __log.LogWarning($"AFTER: _sessionsService.EndSessionAsync");
                if (result == EndSessionResult.NotFound)
                {
                    __log.LogWarning($"SessionID {id} not found.");
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    return notFoundResponse;
                }
                __log.LogInformation("EndSession completing");
                var response = req.CreateResponse(HttpStatusCode.NoContent);
                return response;
            }
            catch (Exception ex)
            {
                __log.LogError(ex, "EndSession: Unhandled exception");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "Internal server error occurred." });
                return errorResponse;
            }

        }

    }
}
