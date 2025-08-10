using MeatGeek.IoT.Edge.Shared.Models;
using MeatGeek.MockDevice.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register telemetry simulator as singleton
builder.Services.AddSingleton<ITelemetrySimulator, TelemetrySimulator>();

// Register background service for simulation updates
builder.Services.AddHostedService<SimulationUpdateService>();

// Configure the app to listen on port 3000
builder.WebHost.UseUrls("http://*:3000");

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow })
    .WithName("HealthCheck")
    .WithOpenApi();

// Main BBQ device status endpoint that matches what Telemetry module expects
app.MapGet("/api/robots/MeatGeekBot/commands/get_status", (ITelemetrySimulator simulator) =>
{
    var status = simulator.GetCurrentStatus();
    var response = new DeviceResponse
    {
        Result = status
    };

    return response;
})
.WithName("GetBBQStatus")
.WithOpenApi();

// Additional simulation control endpoints
app.MapPost("/api/simulation/start", (string scenario, ITelemetrySimulator simulator) =>
{
    var cookingScenario = scenario.ToLower() switch
    {
        "brisket" => CookingScenarios.Brisket,
        "porkshoulder" => CookingScenarios.PorkShoulder,
        "ribs" => CookingScenarios.Ribs,
        "chicken" => CookingScenarios.Chicken,
        _ => CookingScenarios.Default
    };

    simulator.StartCooking(cookingScenario);
    
    return new { 
        status = "started", 
        scenario = cookingScenario.Name, 
        targetTemp = cookingScenario.TargetGrillTemperature 
    };
})
.WithName("StartCooking")
.WithOpenApi();

app.MapPost("/api/simulation/stop", (ITelemetrySimulator simulator) =>
{
    simulator.StopCooking();
    return new { status = "stopped" };
})
.WithName("StopCooking")
.WithOpenApi();

app.MapPost("/api/simulation/settemp", (int temperature, ITelemetrySimulator simulator) =>
{
    simulator.SetTargetTemperature(temperature);
    return new { status = "temperature set", targetTemperature = temperature };
})
.WithName("SetTemperature")
.WithOpenApi();

app.Run();

// Make Program class public for testing
public partial class Program { }