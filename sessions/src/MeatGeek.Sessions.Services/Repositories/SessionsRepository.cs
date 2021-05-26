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
        
        public async Task<string> AddSessionAsync(SessionDocument SessionDocument)
        {
            var documentUri = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);
            Document doc = await DocumentClient.CreateDocumentAsync(documentUri, SessionDocument);
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
            var documentUri = UriFactory.CreateDocumentUri(DatabaseName, CollectionName, SessionId);
            try
            {
                var documentResponse = await DocumentClient.ReadDocumentAsync<SessionDocument>(documentUri, new RequestOptions { PartitionKey = new PartitionKey(smokerId) });
                return documentResponse.Document;
            }
            catch (DocumentClientException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // we return null to indicate the document was not found
                return null;
            }
        }

        public async Task<SessionSummaries> GetSessionsAsync(string smokerId)
        {
            var documentUri = UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName);

            // create a query to just get the document ids
            var query = DocumentClient
                .CreateDocumentQuery<SessionSummary>(documentUri)
                .Where(d => d.SmokerId == smokerId)
                .Select(d => new SessionSummary { Id = d.Id, Title = d.Title })
                .AsDocumentQuery();
            
            // iterate until we have all of the ids
            var list = new SessionSummaries();
            while (query.HasMoreResults)
            {
                var summaries = await query.ExecuteNextAsync<SessionSummary>();
                list.AddRange(summaries);
            }
            return list;
        }
    }
}
