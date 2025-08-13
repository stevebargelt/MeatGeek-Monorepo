using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Request;
using MeatGeek.Sessions.Services.Models.Results;

using Newtonsoft.Json.Linq;

namespace MeatGeek.Sessions.Api
{
    public class UpdateSession
    {
        private readonly ILogger<UpdateSession> _log;
        private readonly ISessionsService _sessionsService;

        public UpdateSession(ILogger<UpdateSession> log, ISessionsService sessionsService)
        {
            _log = log;
            _sessionsService = sessionsService;
        }

        [Function("UpdateSession")]
        // [OpenApiOperation(operationId: "UpdateSession", tags: new[] { "session" }, Summary = "Updated an existing session.", Description = "Updates a session (sessions are 'cooks' or BBQ sessions).", Visibility = OpenApiVisibilityType.Important)]
        // [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SessionDetails), Required = true, Description = "Session object with updated values")]
        // [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionDetails), Summary = "Session dtails updated", Description = "Session details updated")]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.MethodNotAllowed, Summary = "Invalid input", Description = "Invalid input")]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Session not found", Description = "Session not found")]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Summary = "An exception occurred", Description = "An exception occurred.")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", "put", Route = "sessions/{smokerId}/{id}")] HttpRequestData req,
                string smokerId,
                string id)
        {
            _log.LogInformation("UpdateSession: Called");
            _log.LogInformation($"UpdateSession: SmokerID = {smokerId} SessionID = {id}");

            // get the request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updateData = new UpdateSessionRequest { };
            updateData.SmokerId = smokerId;
            JObject data;
            try
            {
                data = JObject.Parse(requestBody);
            }
            catch (JsonReaderException)
            {
                _log.LogWarning("UpdateSession: Could not parse JSON");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Body should be provided in JSON format." });
                return badResponse;
            }
            _log.LogInformation("Made it past data = JObject.Parse(requestBody)");
            if (!data.HasValues)
            {
                _log.LogWarning("UpdateSession: data has no values.");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Missing required properties. Nothing to update." });
                return badResponse;
            }
            JToken titleToken = data["title"];
            if (titleToken != null && titleToken.Type == JTokenType.String && titleToken.ToString() != String.Empty)
            {
                updateData.Title = titleToken.ToString();
                _log.LogInformation($"Title will be updated to {updateData.Title}");
            }
            else
            {
                _log.LogInformation($"Title will NOT be updated.");
            }
            JToken descriptionToken = data["description"];
            if (descriptionToken != null && descriptionToken.Type == JTokenType.String && descriptionToken.ToString() != String.Empty)
            {
                updateData.Description = descriptionToken.ToString();
                _log.LogInformation($"Description will be updated to {updateData.Description}");
            }
            else
            {
                _log.LogInformation($"Description will NOT be updated");
            }
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
                catch (ArgumentNullException argNullEx)
                {
                    _log.LogError(argNullEx, $"Argument NUll exception");
                    throw;
                }
                catch (ArgumentException argEx)
                {
                    _log.LogError(argEx, $"Argument exception");
                    throw;
                }
                catch (FormatException formatEx)
                {
                    _log.LogError(formatEx, $"Format exception");
                    throw;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, $"Unhandled Exception from DateTimeParse");
                    throw;
                }
                _log.LogInformation($"EndTime will be updated to {updateData.EndTime.ToString()}");
            }
            else
            {
                _log.LogInformation($"EndTime will NOT be updated.");
            }
            try
            {
                _log.LogWarning($"BEFORE: _sessionsService.UpdateSessionAsync");
                var result = await _sessionsService.UpdateSessionAsync(id, updateData.SmokerId, updateData.Title, updateData.Description, updateData.EndTime);
                if (result == UpdateSessionResult.NotFound)
                {
                    _log.LogWarning($"SessionID {id} not found.");
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    return notFoundResponse;
                }
                _log.LogInformation("UpdateSession completing");
                var successResponse = req.CreateResponse(HttpStatusCode.NoContent);
                return successResponse;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unhandled exception");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "An internal server error occurred." });
                return errorResponse;
            }

        }

    }
}
