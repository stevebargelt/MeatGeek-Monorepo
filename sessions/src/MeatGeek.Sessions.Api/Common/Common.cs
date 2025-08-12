using System.Data.Common;

namespace MeatGeek.Sessions
{
    public class CosmosDbConnectionString
    {
        // This class parses a Cosmos DB connection string to its Account Key and Account Endpoint components.
        public CosmosDbConnectionString(string connectionString)
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            if (builder.TryGetValue("AccountKey", out object? key) && key != null)
            {
                AuthKey = key.ToString();
            }

            if (builder.TryGetValue("AccountEndpoint", out object? uri) && uri != null)
            {
                var uriString = uri.ToString();
                if (!string.IsNullOrEmpty(uriString))
                {
                    ServiceEndpoint = new Uri(uriString);
                }
            }
        }

        public Uri? ServiceEndpoint { get; set; }

        public string? AuthKey { get; set; }
    }
}