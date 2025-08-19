using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MeatGeek.Sessions;
using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Repositories;
using MeatGeek.Shared;
using MeatGeek.Sessions.WorkerApi.Configurations;

var host = new HostBuilder()
    .ConfigureHostConfiguration(configHost =>
    {
        configHost.SetBasePath(Environment.CurrentDirectory);
        configHost.AddEnvironmentVariables();
        configHost.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureAppConfiguration(configBuilder =>
    {
        configBuilder.AddEnvironmentVariables();
    })
    .ConfigureServices(services =>
    {
        var appInsightsConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
        if (string.IsNullOrEmpty(appInsightsConnectionString))
        {
            throw new ArgumentNullException("Please specify a value for APPLICATIONINSIGHTS_CONNECTION_STRING in the local.settings.json file or your Azure Functions Settings.");
        }
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Register the Cosmos DB client as a Singleton
        services.AddSingleton<CosmosClient>((s) =>
        {
            var connectionString = Environment.GetEnvironmentVariable("CosmosDBConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("Please specify a value for CosmosDBConnection in the local.settings.json file or your Azure Functions Settings.");
            }

            var cosmosDbConnectionString = new CosmosDbConnectionString(connectionString);
            return new CosmosClientBuilder(cosmosDbConnectionString.ServiceEndpoint?.OriginalString ?? throw new ArgumentNullException(nameof(connectionString), "ServiceEndpoint is null"), cosmosDbConnectionString.AuthKey)
                .WithBulkExecution(true)
                .Build();
        });

        // Uncomment and add other services as needed
        // services.AddScoped<ISessionsService, SessionsService>();
        // services.AddScoped<ISessionsRepository, SessionsRepository>();
        // services.AddScoped<IEventGridPublisherService, EventGridPublisherService>();
    })
    .ConfigureFunctionsWebApplication()
    .ConfigureLogging(logging =>
    {
        logging.Services.Configure<LoggerFilterOptions>(options =>
        {
            LoggerFilterRule? defaultRule = options.Rules.FirstOrDefault(rule => rule.ProviderName
                == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
            if (defaultRule is not null)
            {
                options.Rules.Remove(defaultRule);
            }
        });
    })
    .Build();
host.Run();