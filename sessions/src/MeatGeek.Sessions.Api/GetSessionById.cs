using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Models.Response;


namespace MeatGeek.Sessions
{
    public class GetSessionById
    {

        private const string JsonContentType = "application/json";
        private readonly ILogger<GetSessionById> _log;
        private readonly ISessionsService _sessionsService;

        public GetSessionById(ILogger<GetSessionById> log, ISessionsService sessionsService)
        {
            _log = log;
            _sessionsService = sessionsService;
        }

        [Function("GetSessionById")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions/{smokerId}/{id}")] HttpRequestData req,
            string smokerId,
            string id)
        {
            _log.LogInformation("GetSessionById triggered");

            if (string.IsNullOrEmpty(smokerId))
            {
                _log.LogError("GetSessionById: Missing smokerId - url should be /sessions/{smokerId}/{id}");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { error = "Missing required property 'smokerId'." });
                return errorResponse;
            }

            if (string.IsNullOrEmpty(id))
            {
                _log.LogError("GetSessionById: Missing id - url should be /sessions/{smokerId}/{id}");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteAsJsonAsync(new { error = "Missing required property 'id'." });
                return errorResponse;
            }

            try
            {
                var document = await _sessionsService.GetSessionAsync(id, smokerId);
                if (document == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    return notFoundResponse;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(document);
                return response;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "<-- GetSessionById Unhandled exception");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "Internal server error occurred." });
                return errorResponse;
            }

        }

    }
}