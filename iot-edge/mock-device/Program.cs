using MeatGeek.MockDevice.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
app.MapGet("/api/robots/MeatGeekBot/commands/get_status", () =>
{
    var response = new MockDeviceResponse
    {
        Result = new MockSmokerStatus
        {
            AugerOn = true,
            BlowerOn = false,
            IgniterOn = false,
            Temps = new MockTemps
            {
                GrillTemp = 225.5,
                Probe1Temp = 165.2,
                Probe2Temp = 0.0,
                Probe3Temp = 0.0,
                Probe4Temp = 0.0
            },
            FireHealthy = true,
            Mode = "cooking",
            SetPoint = 225,
            ModeTime = DateTime.UtcNow.AddHours(-2),
            CurrentTime = DateTime.UtcNow
        }
    };

    return response;
})
.WithName("GetBBQStatus")
.WithOpenApi();

app.Run();

// Make Program class public for testing
public partial class Program { }