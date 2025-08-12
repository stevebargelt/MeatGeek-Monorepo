using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MeatGeek.Sessions.Services;
using MeatGeek.Sessions.Services.Repositories;
using MeatGeek.Shared;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();

        // Register services
        services.AddScoped<ISessionsService, SessionsService>();
        services.AddScoped<ISessionsRepository, SessionsRepository>();
        services.AddScoped<IEventGridPublisherService, EventGridPublisherService>();
    })
    .Build();

host.Run();