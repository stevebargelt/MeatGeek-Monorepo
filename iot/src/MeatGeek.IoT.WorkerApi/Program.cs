using Microsoft.Azure.Devices;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;


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

        // Register the IoT Hub ServiceClient as a Singleton
        services.AddSingleton<ServiceClient>((s) =>
        {
            var connectionString = Environment.GetEnvironmentVariable("IOT_SERVICE_CONNECTION");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("Please specify a value for IOT_SERVICE_CONNECTION in the local.settings.json file or your Azure Functions Settings.");
            }

            return ServiceClient.CreateFromConnectionString(connectionString);
        });
    })
    .ConfigureFunctionsWebApplication()
        // Must be set at the end of the chain
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