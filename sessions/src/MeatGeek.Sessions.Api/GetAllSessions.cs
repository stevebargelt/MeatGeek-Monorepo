using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;


using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Converters;
using MeatGeek.Sessions.Services.Models.Response;

#nullable enable
namespace MeatGeek.Sessions
{
    public class GetAllSessions
    {
        private const string JsonContentType = "application/json";
        private readonly ILogger<GetAllSessions> _log;
        private readonly ISessionsService _sessionsService;

        public GetAllSessions(ILogger<GetAllSessions> log, ISessionsService sessionsService)
        {
            _log = log;
            _sessionsService = sessionsService;
        }

        [Function("GetAllSessions")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions/{smokerId}")] HttpRequestData req,
                string smokerId)
        {
            _log.LogInformation("GetAllSessions triggered");

            if (string.IsNullOrEmpty(smokerId))
            {
                _log.LogInformation("GetAllSessions: Missing smokerId - url should be /sessions/{smokerId}");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { error = "Missing required property 'smokerId'." });
                return errorResponse;
            }

            try
            {
                var summaries = await _sessionsService.GetSessionsAsync(smokerId);
                if (summaries == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    return notFoundResponse;
                }

                // serialise the summaries using a custom converter
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                };
                settings.Converters.Add(new SessionSummariesConverter());
                var json = JsonConvert.SerializeObject(summaries, settings);

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", JsonContentType);
                await response.WriteStringAsync(json);
                return response;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "<-- GetAllSessions Unhandled exception");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "Internal server error occurred." });
                return errorResponse;
            }
        }
    }

}
