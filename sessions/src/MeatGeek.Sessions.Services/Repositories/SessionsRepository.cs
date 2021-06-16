using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MeatGeek.Sessions.Services.Models;
using MeatGeek.Sessions.Services.Models.Data;
using MeatGeek.Sessions.Services.Models.Response;
using MeatGeek.Sessions.Services.Models.Results;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Logging;

namespace MeatGeek.Sessions.Services.Repositories
{
    public interface ISessionsRepository
    {
        Task<string> AddSessionAsync(SessionDocument SessionObject);
        Task<DeleteSessionResult> DeleteSessionAsync(string SessionId, string smokerId);
        Task UpdateSessionAsync(SessionDocument SessionDocument);
        Task<SessionDocument> GetSessionAsync(string SessionId, string smokerId);
        Task<SessionSummaries> GetSessionsAsync(string smokerId);
        // Task<SessionDocument> FindSessionWithItemAsync(string itemId, ItemType itemType, string userId);
    }

    public class SessionsRepository : ISessionsRepository
    {
        private static readonly string EndpointUrl = Environment.GetEnvironmentVariable("CosmosDBAccountEndpointUrl");
        private static readonly string AccountKey = Environment.GetEnvironmentVariable("CosmosDBAccountKey");
        private static readonly string DatabaseName = Environment.GetEnvironmentVariable("DatabaseName");
        private static readonly string CollectionName = Environment.GetEnvironmentVariable("CollectionName");
        private static readonly DocumentClient DocumentClient = new DocumentClient(new Uri(EndpointUrl), AccountKey);
        private ILogger<SessionsService> _log;
        
        public SessionsRepository(ILogger<SessionsService> logger) 
        {
            _log = logger;
        }

        public async Task<string> AddSessionAsync(SessionDocument SessionDocument)
        {
            var documentUri = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);
            var result = await DocumentClient.CreateDocumentAsync(documentUri, SessionDocument);
            _log.LogInformation($"AddSessionAsync: RU used: {result.RequestCharge}");
            Document doc = result.Resource;
            return doc.Id;
        }

        public async Task<DeleteSessionResult> DeleteSessionAsync(string SessionId, string smokerId)
        {
            var documentUri = UriFactory.CreateDocumentUri(DatabaseName, CollectionName, SessionId);
            try
            {
                await DocumentClient.DeleteDocumentAsync(documentUri, new RequestOptions { PartitionKey = new PartitionKey(smokerId) });
                return DeleteSessionResult.Success;
            }
            catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // we return the NotFound result to indicate the document was not found
                return DeleteSessionResult.NotFound;
            }
        }

        public Task UpdateSessionAsync(SessionDocument SessionDocument)
        {
            var documentUri = UriFactory.CreateDocumentUri(DatabaseName, CollectionName, SessionDocument.Id);
            var concurrencyCondition = new AccessCondition
            {
                Condition = SessionDocument.ETag,
                Type = AccessConditionType.IfMatch
            };
            return DocumentClient.ReplaceDocumentAsync(documentUri, SessionDocument, new RequestOptions { AccessCondition = concurrencyCondition });
        }

        public async Task<SessionDocument> GetSessionAsync(string SessionId, string smokerId)
        {
            _log.LogInformation("GetSessionAsync called");
            _log.LogInformation($"GetSessionAsync: SessionId = {SessionId}, SmokerId = {smokerId}");
            var documentUri = UriFactory.CreateDocumentUri(DatabaseName, CollectionName, SessionId);
            _log.LogInformation($"documentUri = {documentUri}");
            try
            {
                var documentResponse = await DocumentClient.ReadDocumentAsync<SessionDocument>(documentUri, new RequestOptions { PartitionKey = new PartitionKey(smokerId) });
                _log.LogInformation($"GetSessionAsync: RU used: {documentResponse.RequestCharge}");
                return documentResponse.Document;
            }
            catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // we return null to indicate the document was not found
                _log.LogError(ex, "<-- exception GetsessionAsync. Not Found");
                return null;
            }
            catch (Exception exc)
            {
                _log.LogError(exc, "<-- unhandled exception GetsessionAsync. Throwing...");
                throw exc;
            }
        }

        public async Task<SessionSummaries> GetSessionsAsync(string smokerId)
        {
            var documentUri = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);

            // create a query to just get the document ids
            var query = DocumentClient
                .CreateDocumentQuery<SessionSummary>(documentUri)
                .Where(d => d.SmokerId == smokerId && d.Type == "session" )
                .Select(d => new SessionSummary { Id = d.Id, Title = d.Title })
                .AsDocumentQuery();
            
            // iterate until we have all of the ids
            double totalRU = 0;
            var list = new SessionSummaries();
            while (query.HasMoreResults)
            {
                var summaries = await query.ExecuteNextAsync<SessionSummary>();
                totalRU += summaries.RequestCharge;
                list.AddRange(summaries);
            }
            _log.LogInformation($"GetSessionsAsync: RU used: {totalRU}");
            return list;
        }
    }
}
