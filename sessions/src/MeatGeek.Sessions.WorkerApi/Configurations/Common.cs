using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace MeatGeek.Sessions.WorkerApi.Configurations
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
                AuthKey = key.ToString() ?? throw new ArgumentException("AccountKey cannot be null");
            }
            else
            {
                throw new ArgumentException("AccountKey not found in connection string");
            }

            if (builder.TryGetValue("AccountEndpoint", out object? uri) && uri != null)
            {
                var uriString = uri.ToString();
                if (!string.IsNullOrEmpty(uriString))
                {
                    ServiceEndpoint = new Uri(uriString);
                }
                else
                {
                    throw new ArgumentException("AccountEndpoint cannot be empty");
                }
            }
            else
            {
                throw new ArgumentException("AccountEndpoint not found in connection string");
            }
        }

        public Uri ServiceEndpoint { get; set; }

        public string AuthKey { get; set; }
    }
}