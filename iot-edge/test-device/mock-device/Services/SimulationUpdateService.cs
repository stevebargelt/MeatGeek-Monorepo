namespace MeatGeek.MockDevice.Services;

/// <summary>
/// Background service that updates the telemetry simulation every 5 seconds
/// </summary>
public class SimulationUpdateService : BackgroundService
{
    private readonly ITelemetrySimulator _simulator;
    private readonly ILogger<SimulationUpdateService> _logger;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(5);

    public SimulationUpdateService(ITelemetrySimulator simulator, ILogger<SimulationUpdateService> logger)
    {
        _simulator = simulator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Simulation update service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _simulator.UpdateSimulation();
                
                if (_simulator.IsCooking)
                {
                    _logger.LogDebug("Simulation updated - cooking in progress");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating simulation");
            }

            await Task.Delay(_updateInterval, stoppingToken);
        }

        _logger.LogInformation("Simulation update service stopping");
    }
}