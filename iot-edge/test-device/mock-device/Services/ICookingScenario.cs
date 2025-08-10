namespace MeatGeek.MockDevice.Services;

/// <summary>
/// Defines a cooking scenario with specific temperature and timing characteristics
/// </summary>
public interface ICookingScenario
{
    /// <summary>
    /// Name of the cooking scenario (e.g., "Brisket", "Pork Shoulder", "Chicken")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Target temperature for the grill/smoker
    /// </summary>
    int TargetGrillTemperature { get; }

    /// <summary>
    /// Target internal temperature for the meat
    /// </summary>
    int TargetProbeTemperature { get; }

    /// <summary>
    /// Estimated total cooking time in minutes
    /// </summary>
    int EstimatedCookingTimeMinutes { get; }

    /// <summary>
    /// How quickly the grill heats up (degrees per minute when heating)
    /// </summary>
    double GrillHeatingRate { get; }

    /// <summary>
    /// How quickly the grill cools down (degrees per minute when cooling)
    /// </summary>
    double GrillCoolingRate { get; }

    /// <summary>
    /// How quickly the meat heats up (degrees per minute)
    /// </summary>
    double MeatHeatingRate { get; }

    /// <summary>
    /// Temperature tolerance for maintaining steady state (±degrees)
    /// </summary>
    double TemperatureTolerance { get; }

    /// <summary>
    /// Initial ambient temperature
    /// </summary>
    double AmbientTemperature { get; }
}

/// <summary>
/// Pre-defined cooking scenarios for common BBQ recipes
/// </summary>
public static class CookingScenarios
{
    public static readonly ICookingScenario Brisket = new BrisketScenario();
    public static readonly ICookingScenario PorkShoulder = new PorkShoulderScenario();
    public static readonly ICookingScenario Ribs = new RibsScenario();
    public static readonly ICookingScenario Chicken = new ChickenScenario();
    public static readonly ICookingScenario Default = new DefaultScenario();

    private class BrisketScenario : ICookingScenario
    {
        public string Name => "Brisket";
        public int TargetGrillTemperature => 225;
        public int TargetProbeTemperature => 203;
        public int EstimatedCookingTimeMinutes => 720; // 12 hours
        public double GrillHeatingRate => 3.0;
        public double GrillCoolingRate => 2.0;
        public double MeatHeatingRate => 0.5;
        public double TemperatureTolerance => 15.0;
        public double AmbientTemperature => 70.0;
    }

    private class PorkShoulderScenario : ICookingScenario
    {
        public string Name => "Pork Shoulder";
        public int TargetGrillTemperature => 250;
        public int TargetProbeTemperature => 195;
        public int EstimatedCookingTimeMinutes => 480; // 8 hours
        public double GrillHeatingRate => 3.5;
        public double GrillCoolingRate => 2.5;
        public double MeatHeatingRate => 0.7;
        public double TemperatureTolerance => 20.0;
        public double AmbientTemperature => 70.0;
    }

    private class RibsScenario : ICookingScenario
    {
        public string Name => "Ribs";
        public int TargetGrillTemperature => 275;
        public int TargetProbeTemperature => 190;
        public int EstimatedCookingTimeMinutes => 360; // 6 hours
        public double GrillHeatingRate => 4.0;
        public double GrillCoolingRate => 3.0;
        public double MeatHeatingRate => 1.0;
        public double TemperatureTolerance => 25.0;
        public double AmbientTemperature => 70.0;
    }

    private class ChickenScenario : ICookingScenario
    {
        public string Name => "Chicken";
        public int TargetGrillTemperature => 350;
        public int TargetProbeTemperature => 165;
        public int EstimatedCookingTimeMinutes => 90; // 1.5 hours
        public double GrillHeatingRate => 5.0;
        public double GrillCoolingRate => 4.0;
        public double MeatHeatingRate => 2.5;
        public double TemperatureTolerance => 15.0;
        public double AmbientTemperature => 70.0;
    }

    private class DefaultScenario : ICookingScenario
    {
        public string Name => "Default Cook";
        public int TargetGrillTemperature => 225;
        public int TargetProbeTemperature => 165;
        public int EstimatedCookingTimeMinutes => 240; // 4 hours
        public double GrillHeatingRate => 3.0;
        public double GrillCoolingRate => 2.0;
        public double MeatHeatingRate => 0.8;
        public double TemperatureTolerance => 15.0;
        public double AmbientTemperature => 70.0;
    }
}