using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
// using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
// using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using MeatGeek.Sessions.Services;

// using Microsoft.OpenApi.Models;


namespace MeatGeek.Sessions.Api
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
        // [OpenApiOperation(operationId: "GetSessionById", tags: new[] { "session" }, Summary = "Find Session by ID", Description = "Returns a single session. Sessions are cooking / BBQ Sessions or cooks.", Visibility = OpenApiVisibilityType.Important)]
        // [OpenApiParameter(name: "smokerid", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "The ID of the Smoker the session belings to", Description = "The ID of the Smoker the session belings to", Visibility = OpenApiVisibilityType.Important)]
        // [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "ID of the Session to return", Description = "The ID of the session to return", Visibility = OpenApiVisibilityType.Important)]
        // [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SessionDetails), Summary = "successful operation", Description = "successful response")]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid ID supplied", Description = "Invalid ID supplied")]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Summary = "Session not found", Description = "Session not found")]
        // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Summary = "An exception occurred", Description = "An exception occurred.")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions/{smokerId}/{id}")] HttpRequestData req,
            string smokerId,
            string id)
        {
            _log.LogInformation("GetSessionById triggered");

            if (string.IsNullOrEmpty(smokerId))
            {
                _log.LogError("GetSessionById: Missing smokerId - url should be /sessions/{smokerId}/{id}");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Missing required property 'smokerId'." });
                return badResponse;
            }

            if (string.IsNullOrEmpty(id))
            {
                _log.LogError("GetSessionById: Missing id - url should be /sessions/{smokerId}/{id}");
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Missing required property 'id'." });
                return badResponse;
            }

            try
            {
                var document = await _sessionsService.GetSessionAsync(id, smokerId);
                if (document == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    return notFoundResponse;
                }

                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                await okResponse.WriteAsJsonAsync(document);
                return okResponse;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "<-- GetSessionById Unhandled exception");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { error = "An internal server error occurred." });
                return errorResponse;
            }

        }

    }
}