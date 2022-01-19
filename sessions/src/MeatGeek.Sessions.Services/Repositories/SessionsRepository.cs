using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MeatGeek.Sessions.Services.Models.Data;
using MeatGeek.Sessions.Services.Models.Response;
using MeatGeek.Sessions.Services.Models.Results;
using Microsoft.Azure.Cosmos;

using Microsoft.Azure.Cosmos.Scripts;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;

namespace MeatGeek.Sessions.Services.Repositories
{
    public interface ISessionsRepository
    {
        Task<string> AddSessionAsync(SessionDocument SessionObject);
        Task<DeleteSessionResult> DeleteSessionAsync(string SessionId, string smokerId);
        Task<SessionDocument> UpdateSessionAsync(SessionDocument SessionDocument);
        Task<SessionDocument> GetSessionAsync(string SessionId, string smokerId);
        Task<SessionSummaries> GetSessionsAsync(string smokerId);
        Task<SessionStatuses> GetSessionStatusesAsync(string SessionId, string smokerId);

    }

    public class SessionsRepository : ISessionsRepository
    {
        private static readonly string DatabaseName = Environment.GetEnvironmentVariable("DatabaseName");
        private static readonly string CollectionName = Environment.GetEnvironmentVariable("CollectionName");
        private readonly CosmosClient _cosmosClient;
        private ILogger<SessionsService> _log;
        private Container _container;
        
        public SessionsRepository(CosmosClient cosmosClient, ILogger<SessionsService> logger) 
        {
            _log = logger;
            _cosmosClient = cosmosClient;
            _container = _cosmosClient.GetContainer(DatabaseName, CollectionName);
        }

        public async Task<string> AddSessionAsync(SessionDocument SessionDocument)
        {
            SessionDocument.Id = Guid.NewGuid().ToString();// add the line in your code

            try
            {
                ItemResponse<SessionDocument> response = await _container.CreateItemAsync<SessionDocument>(SessionDocument, new PartitionKey(SessionDocument.SmokerId));
                _log.LogInformation($"AddSessionAsync: RU used: {response.RequestCharge}");
                return response.Resource.Id;
            }
            catch (CosmosException ex) 
            {
                _log.LogError($"Exception in AddSessionAsync StatusCode={ex.StatusCode}");
                throw ex;
            }
            catch (AggregateException ae)
            {
                _log.LogError("Caught aggregate exception-Task.Wait behavior.");
                throw ae;
            }
        }

        public async Task<DeleteSessionResult> DeleteSessionAsync(string SessionId, string smokerId)
        {
            try
            {
                ItemResponse<SessionDocument> response = await _container.DeleteItemAsync<SessionDocument>(SessionId, new PartitionKey(smokerId));
                _log.LogInformation($"DeleteSessionAsync: RU used: {response.RequestCharge}");
                Scripts scripts = _container.Scripts;
                StoredProcedureExecuteResponse<string> sprocResponse = await scripts.ExecuteStoredProcedureAsync<string>(
                        "BulkDelete", 
                        new PartitionKey(smokerId), 
                        new dynamic[] {SessionId});
                _log.LogInformation($"BulkDelete Response:{sprocResponse.Resource}");
                return DeleteSessionResult.Success;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // we return the NotFound result to indicate the document was not found
                return DeleteSessionResult.NotFound;
            }
        }

        public async Task<SessionDocument> UpdateSessionAsync(SessionDocument SessionDocument)
        {
            ItemResponse<SessionDocument> response = await _container.ReplaceItemAsync<SessionDocument>(SessionDocument, SessionDocument.Id, new PartitionKey(SessionDocument.SmokerId));
            _log.LogInformation($"DeleteSessionAsync: RU used: {response.RequestCharge}");
            return response.Resource;
        }

        public async Task<SessionDocument> GetSessionAsync(string SessionId, string smokerId)
        {
            _log.LogInformation($"GetSessionAsync: SessionId = {SessionId}, SmokerId = {smokerId}");
            try
            {
                ItemResponse<SessionDocument> response = await _container.ReadItemAsync<SessionDocument>(SessionId, new PartitionKey(smokerId));
                _log.LogInformation($"GetSessionAsync: RU used: {response.RequestCharge}");
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
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
            double totalRU = 0;
            var list = new SessionSummaries();

            // LINQ query generation
            using (FeedIterator<SessionSummary> setIterator = _container.GetItemLinqQueryable<SessionSummary>()
                                .Where(s => s.SmokerId == smokerId && s.Type == "session" )
                                //.Select(d => new SessionSummary { Id = s.Id, Title = s.Title })
                                .ToFeedIterator())
            {                   
                //Asynchronous query execution
                while (setIterator.HasMoreResults)
                {
                    var response = await setIterator.ReadNextAsync();
                    totalRU += response.RequestCharge;
                    foreach(var item in response)
                    {
                        list.Add(item);
                    }
                }
            }
            _log.LogInformation($"GetSessionsAsync: RU used: {totalRU}");
            return list;
        }

        public async Task<SessionStatuses> GetSessionStatusesAsync(string sessionId, string smokerId)
        {
            _log.LogInformation($"GetSessionStatusesAsynccalled with smokerId = {smokerId} and sessionId = {sessionId}");
            
            double totalRU = 0;
            var list = new SessionStatuses();

            // LINQ query generation
            using (FeedIterator<SessionStatusDocument> setIterator = _container.GetItemLinqQueryable<SessionStatusDocument>()
                                .Where(s => s.SmokerId == smokerId && s.Type == "status" && s.SessionId == sessionId)
                                //.Select(d => new SessionSummary { Id = s.Id, Title = s.Title })
                                .ToFeedIterator())
            {                   
                //Asynchronous query execution
                while (setIterator.HasMoreResults)
                {
                    var response = await setIterator.ReadNextAsync();
                    totalRU += response.RequestCharge;
                    foreach(var item in response)
                    {
                        list.Add(item);
                    }
                }
            }
            _log.LogInformation($"GetSessionsAsync: RU used: {totalRU}");
            return list;

        }        
    }
}
