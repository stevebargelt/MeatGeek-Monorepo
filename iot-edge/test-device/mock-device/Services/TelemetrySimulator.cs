using MeatGeek.MockDevice.Models;

namespace MeatGeek.MockDevice.Services;

/// <summary>
/// Simulates realistic BBQ telemetry data with temperature progression and component state logic
/// </summary>
public interface ITelemetrySimulator
{
    /// <summary>
    /// Gets the current simulated smoker status
    /// </summary>
    MockSmokerStatus GetCurrentStatus();

    /// <summary>
    /// Starts a cooking session with the specified scenario
    /// </summary>
    void StartCooking(ICookingScenario scenario);

    /// <summary>
    /// Stops the current cooking session
    /// </summary>
    void StopCooking();

    /// <summary>
    /// Changes the target temperature (setpoint)
    /// </summary>
    void SetTargetTemperature(int targetTemperature);

    /// <summary>
    /// Updates the simulation state (called by background service)
    /// </summary>
    void UpdateSimulation();

    /// <summary>
    /// Gets whether the simulator is currently cooking
    /// </summary>
    bool IsCooking { get; }
}

public class TelemetrySimulator : ITelemetrySimulator
{
    private readonly object _lock = new();
    private ICookingScenario _currentScenario;
    private DateTime _cookingStartTime;
    private DateTime _lastUpdateTime;
    private double _currentGrillTemp;
    private double _currentProbeTemp;
    private int _targetTemperature;
    private string _currentMode;
    private bool _augerOn;
    private bool _blowerOn;
    private bool _igniterOn;
    private bool _isCooking;
    private Random _random;

    public TelemetrySimulator()
    {
        _random = new Random();
        _currentScenario = CookingScenarios.Default;
        _currentGrillTemp = _currentScenario.AmbientTemperature;
        _currentProbeTemp = _currentScenario.AmbientTemperature;
        _targetTemperature = _currentScenario.TargetGrillTemperature;
        _currentMode = "idle";
        _lastUpdateTime = DateTime.UtcNow;
        _cookingStartTime = DateTime.UtcNow; // Initialize to current time instead of MinValue
        _isCooking = false;
    }

    public bool IsCooking 
    { 
        get 
        { 
            lock (_lock) 
            { 
                return _isCooking; 
            } 
        } 
    }

    public MockSmokerStatus GetCurrentStatus()
    {
        lock (_lock)
        {
            return new MockSmokerStatus
            {
                Id = Guid.NewGuid().ToString(),
                Ttl = _isCooking ? -1 : 259200, // -1 for session data, 3 days for telemetry
                SmokerId = "test-device-001",
                SessionId = _isCooking ? "simulation-session" : null,
                Type = _isCooking ? "status" : "telemetry",
                AugerOn = _augerOn,
                BlowerOn = _blowerOn,
                IgniterOn = _igniterOn,
                Temps = new MockTemps
                {
                    GrillTemp = Math.Round(_currentGrillTemp, 1),
                    Probe1Temp = Math.Round(_currentProbeTemp, 1),
                    Probe2Temp = 0.0,
                    Probe3Temp = 0.0,
                    Probe4Temp = 0.0
                },
                FireHealthy = CalculateFireHealth(),
                Mode = _currentMode,
                SetPoint = _targetTemperature,
                ModeTime = _cookingStartTime,
                CurrentTime = DateTime.UtcNow
            };
        }
    }

    public void StartCooking(ICookingScenario scenario)
    {
        lock (_lock)
        {
            _currentScenario = scenario;
            _targetTemperature = scenario.TargetGrillTemperature;
            _cookingStartTime = DateTime.UtcNow;
            _lastUpdateTime = DateTime.UtcNow;
            _isCooking = true;
            _currentMode = "startup";
            _igniterOn = true;
            _augerOn = true;
            _blowerOn = false;
        }
    }

    public void StopCooking()
    {
        lock (_lock)
        {
            _isCooking = false;
            _currentMode = "idle";
            _igniterOn = false;
            _augerOn = false;
            _blowerOn = false;
        }
    }

    public void SetTargetTemperature(int targetTemperature)
    {
        lock (_lock)
        {
            _targetTemperature = targetTemperature;
        }
    }

    public void UpdateSimulation()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var timeDelta = (now - _lastUpdateTime).TotalMinutes;
            _lastUpdateTime = now;

            if (!_isCooking)
            {
                // Gradual cooling to ambient when not cooking
                CoolToAmbient(timeDelta);
                return;
            }

            // Update temperatures based on cooking phase
            UpdateGrillTemperature(timeDelta);
            UpdateProbeTemperature(timeDelta);
            
            // Update component states based on temperature needs
            UpdateComponentStates();
            
            // Update cooking mode based on temperature and time
            UpdateCookingMode();
        }
    }

    private void UpdateGrillTemperature(double timeDeltaMinutes)
    {
        var tempDifference = _targetTemperature - _currentGrillTemp;
        var tolerance = _currentScenario.TemperatureTolerance;
        
        if (Math.Abs(tempDifference) <= tolerance)
        {
            // Within tolerance - small random fluctuations
            var fluctuation = (_random.NextDouble() - 0.5) * (tolerance / 2);
            _currentGrillTemp += fluctuation;
        }
        else if (tempDifference > 0)
        {
            // Need to heat up
            var heatingRate = _currentScenario.GrillHeatingRate;
            var heatIncrease = heatingRate * timeDeltaMinutes;
            
            // Add some randomness to make it more realistic
            var randomFactor = 0.8 + (_random.NextDouble() * 0.4); // 0.8 to 1.2
            heatIncrease *= randomFactor;
            
            _currentGrillTemp += heatIncrease;
            
            // Don't overshoot too much
            if (_currentGrillTemp > _targetTemperature + tolerance)
            {
                _currentGrillTemp = _targetTemperature + (_random.NextDouble() * tolerance);
            }
        }
        else
        {
            // Need to cool down
            var coolingRate = _currentScenario.GrillCoolingRate;
            var coolDecrease = coolingRate * timeDeltaMinutes;
            
            var randomFactor = 0.8 + (_random.NextDouble() * 0.4);
            coolDecrease *= randomFactor;
            
            _currentGrillTemp -= coolDecrease;
            
            if (_currentGrillTemp < _targetTemperature - tolerance)
            {
                _currentGrillTemp = _targetTemperature - (_random.NextDouble() * tolerance);
            }
        }

        // Ensure reasonable bounds
        _currentGrillTemp = Math.Max(_currentScenario.AmbientTemperature, _currentGrillTemp);
        _currentGrillTemp = Math.Min(500, _currentGrillTemp); // Max reasonable temp
    }

    private void UpdateProbeTemperature(double timeDeltaMinutes)
    {
        // Probe temperature follows grill temperature but lags behind
        var targetProbeTemp = Math.Min(_currentProbeTemp + (_currentScenario.MeatHeatingRate * timeDeltaMinutes), 
                                     _currentScenario.TargetProbeTemperature);
        
        // Probe temp can't exceed grill temp (with some physics-based logic)
        if (targetProbeTemp > _currentGrillTemp - 20) // Meat is typically 20 degrees cooler than grill
        {
            targetProbeTemp = Math.Max(_currentGrillTemp - 20 - (_random.NextDouble() * 10), _currentProbeTemp);
        }

        _currentProbeTemp = targetProbeTemp;
        _currentProbeTemp = Math.Max(_currentScenario.AmbientTemperature, _currentProbeTemp);
    }

    private void CoolToAmbient(double timeDeltaMinutes)
    {
        var ambientTemp = _currentScenario.AmbientTemperature;
        var coolingRate = _currentScenario.GrillCoolingRate * 0.5; // Slower cooling when idle
        
        if (_currentGrillTemp > ambientTemp)
        {
            _currentGrillTemp = Math.Max(ambientTemp, _currentGrillTemp - (coolingRate * timeDeltaMinutes));
        }
        
        if (_currentProbeTemp > ambientTemp)
        {
            _currentProbeTemp = Math.Max(ambientTemp, _currentProbeTemp - (coolingRate * timeDeltaMinutes * 0.3));
        }
    }

    private void UpdateComponentStates()
    {
        var tempDifference = _targetTemperature - _currentGrillTemp;
        var cookingTime = DateTime.UtcNow - _cookingStartTime;

        // Igniter logic - only during startup
        _igniterOn = _currentMode == "startup" && cookingTime.TotalMinutes < 15;

        // Auger logic - cycles on/off to maintain temperature
        if (tempDifference > 10)
        {
            _augerOn = true;
        }
        else if (tempDifference < -10)
        {
            _augerOn = false;
        }
        // else maintain current state for hysteresis

        // Blower logic - helps with airflow when heating
        _blowerOn = tempDifference > 15 || (_currentMode == "startup" && cookingTime.TotalMinutes < 10);
    }

    private void UpdateCookingMode()
    {
        var tempDifference = Math.Abs(_targetTemperature - _currentGrillTemp);
        var tolerance = _currentScenario.TemperatureTolerance;
        var cookingTime = DateTime.UtcNow - _cookingStartTime;

        if (cookingTime.TotalMinutes < 5)
        {
            _currentMode = "startup";
        }
        else if (tempDifference <= tolerance)
        {
            _currentMode = "cooking";
        }
        else if (_currentGrillTemp < _targetTemperature - tolerance)
        {
            _currentMode = "heating";
        }
        else
        {
            _currentMode = "cooling";
        }
    }

    private bool CalculateFireHealth()
    {
        // Fire is healthy if temperature is reasonable and components are working
        var hasReasonableTemp = _currentGrillTemp >= _currentScenario.AmbientTemperature + 50;
        var componentsWorking = _isCooking ? (_augerOn || _currentMode == "cooking") : true;
        
        return hasReasonableTemp && componentsWorking;
    }
}