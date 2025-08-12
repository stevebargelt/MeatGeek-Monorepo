using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Repositories;
using MeatGeek.Shared;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(builder =>
    {
        builder.UseNewtonsoftJson();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // CosmosDB Configuration
        services.AddSingleton<CosmosClient>((serviceProvider) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration["CosmosDBConnection"];
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("Please specify a value for CosmosDBConnection in the application settings.");
            }

            var cosmosDbConnectionString = new CosmosDbConnectionString(connectionString);
            CosmosClientBuilder configurationBuilder = new CosmosClientBuilder(
                cosmosDbConnectionString.ServiceEndpoint.OriginalString, 
                cosmosDbConnectionString.AuthKey)
                .WithBulkExecution(true);
            
            return configurationBuilder.Build();
        });

        // Register services
        services.AddScoped<ISessionsService, SessionsService>();
        services.AddScoped<ISessionsRepository, SessionsRepository>();
        services.AddScoped<IEventGridPublisherService, EventGridPublisherService>();
    })
    .Build();

host.Run();