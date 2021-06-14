using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Repositories;
using MeatGeek.Shared;

using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using MeatGeek.Sessions.WorkerApi.Configurations;

[assembly: FunctionsStartup(typeof(MeatGeek.Sessions.Startup))]

namespace MeatGeek.Sessions
{
    public class Startup : FunctionsStartup
    {

        private static IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<AppSettings>();
            builder.Services.AddApplicationInsightsTelemetry();

            // Register the Cosmos DB client as a Singleton.
            builder.Services.AddSingleton<CosmosClient>((s) => {
                var connectionString = configuration["CosmosDBConnection"];
                var cosmosDbConnectionString = new CosmosDbConnectionString(connectionString);

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new ArgumentNullException("Please specify a value for CosmosDBConnection in the local.settings.json file or your Azure Functions Settings.");
                }

                CosmosClientBuilder configurationBuilder = new CosmosClientBuilder(cosmosDbConnectionString.ServiceEndpoint.OriginalString, cosmosDbConnectionString.AuthKey).WithBulkExecution(true);
                return configurationBuilder
                    .Build();
            });            
            //builder.Services.AddScoped<ISessionsService, SessionsService>();
            //builder.Services.AddScoped<ISessionsRepository, SessionsRepository>();
            //builder.Services.AddScoped<IEventGridPublisherService, EventGridPublisherService>();
            //builder.Services.AddScoped<IEventGridSubscriberServiceDI, EventGridSubscriberServiceDI>();

        }

    }
}