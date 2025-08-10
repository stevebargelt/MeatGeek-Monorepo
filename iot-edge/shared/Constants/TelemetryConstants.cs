namespace MeatGeek.IoT.Edge.Shared.Constants;

/// <summary>
/// Constants used across IoT Edge telemetry components.
/// </summary>
public static class TelemetryConstants
{
    /// <summary>
    /// Telemetry data type constants.
    /// </summary>
    public static class Types
    {
        /// <summary>
        /// Status data type - permanent session data with TTL = -1
        /// </summary>
        public const string Status = "status";

        /// <summary>
        /// Telemetry data type - temporary data with TTL = 259200 (3 days)
        /// </summary>
        public const string Telemetry = "telemetry";
    }

    /// <summary>
    /// Smoker operating mode constants.
    /// </summary>
    public static class Modes
    {
        /// <summary>
        /// Device is idle/standby
        /// </summary>
        public const string Idle = "idle";

        /// <summary>
        /// Device is starting up
        /// </summary>
        public const string Startup = "startup";

        /// <summary>
        /// Device is heating to target temperature
        /// </summary>
        public const string Heating = "heating";

        /// <summary>
        /// Device is in active cooking mode
        /// </summary>
        public const string Cooking = "cooking";

        /// <summary>
        /// Device is cooling down
        /// </summary>
        public const string Cooling = "cooling";
    }

    /// <summary>
    /// Time-to-live constants for Cosmos DB documents.
    /// </summary>
    public static class Ttl
    {
        /// <summary>
        /// Permanent retention for session data
        /// </summary>
        public const int SessionData = -1;

        /// <summary>
        /// 3 days retention for telemetry data (259200 seconds)
        /// </summary>
        public const int TelemetryData = 259200;
    }

    /// <summary>
    /// Default telemetry collection intervals.
    /// </summary>
    public static class Intervals
    {
        /// <summary>
        /// Default telemetry collection interval in seconds
        /// </summary>
        public const int DefaultTelemetrySeconds = 30;

        /// <summary>
        /// Minimum allowed telemetry interval in seconds
        /// </summary>
        public const int MinTelemetrySeconds = 1;

        /// <summary>
        /// Maximum allowed telemetry interval in seconds
        /// </summary>
        public const int MaxTelemetrySeconds = 60;
    }
}