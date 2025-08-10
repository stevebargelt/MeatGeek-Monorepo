using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MeatGeek.IoT.TelemetryDirect
{
    /// <summary>
    /// Simplified telemetry module that connects directly to IoT Hub without Edge runtime
    /// Pulls data from MockDevice and sends to Azure IoT Hub
    /// </summary>
    class Program
    {
        private static HttpClient _httpClient = new HttpClient();
        private static DeviceClient _deviceClient;
        private static string _deviceId;
        private static string _sessionId;
        private static int _telemetryInterval = 30;
        private static CancellationTokenSource _cts = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            Console.WriteLine("MeatGeek Telemetry Direct - Starting up...");

            // Get configuration from environment
            var connectionString = Environment.GetEnvironmentVariable("DEVICE_CONNECTION_STRING");
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("ERROR: DEVICE_CONNECTION_STRING environment variable not set!");
                Environment.Exit(1);
            }

            _deviceId = Environment.GetEnvironmentVariable("DEVICE_ID") ?? "test-device";
            _sessionId = Environment.GetEnvironmentVariable("SESSION_ID") ?? "";
            
            var intervalStr = Environment.GetEnvironmentVariable("TELEMETRY_INTERVAL");
            if (!string.IsNullOrEmpty(intervalStr) && int.TryParse(intervalStr, out var interval))
            {
                _telemetryInterval = interval;
            }

            var mockDeviceUrl = Environment.GetEnvironmentVariable("MOCK_DEVICE_URL") ?? "http://mock-device:3000";
            var statusEndpoint = Environment.GetEnvironmentVariable("STATUS_ENDPOINT") ?? "/api/robots/MeatGeekBot/commands/get_status";
            var fullUrl = $"{mockDeviceUrl}{statusEndpoint}";

            Console.WriteLine($"Configuration:");
            Console.WriteLine($"  Device ID: {_deviceId}");
            Console.WriteLine($"  Mock Device URL: {fullUrl}");
            Console.WriteLine($"  Telemetry Interval: {_telemetryInterval} seconds");
            Console.WriteLine($"  Session ID: {(string.IsNullOrEmpty(_sessionId) ? "(none)" : _sessionId)}");

            // Initialize IoT Hub connection
            try
            {
                var transportType = TransportType.Mqtt_Tcp_Only;
                var clientTransport = Environment.GetEnvironmentVariable("CLIENT_TRANSPORT_TYPE");
                if (!string.IsNullOrEmpty(clientTransport) && clientTransport.ToUpper() == "AMQP")
                {
                    transportType = TransportType.Amqp_Tcp_Only;
                }

                _deviceClient = DeviceClient.CreateFromConnectionString(connectionString, transportType);
                await _deviceClient.OpenAsync();
                Console.WriteLine($"Connected to IoT Hub using {transportType}");

                // Set up direct method handlers
                await _deviceClient.SetMethodHandlerAsync("SetTelemetryInterval", SetTelemetryInterval, null);
                await _deviceClient.SetMethodHandlerAsync("SetSessionId", SetSessionId, null);
                await _deviceClient.SetMethodHandlerAsync("EndSession", EndSession, null);
                await _deviceClient.SetMethodHandlerAsync("GetStatus", GetStatus, null);
                Console.WriteLine("Direct method handlers registered");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to connect to IoT Hub: {ex.Message}");
                Environment.Exit(1);
            }

            // Handle graceful shutdown
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                _cts.Cancel();
                Console.WriteLine("Shutting down...");
            };

            // Main telemetry loop
            await SendTelemetryLoop(fullUrl);

            // Cleanup
            await _deviceClient.CloseAsync();
            _deviceClient.Dispose();
            Console.WriteLine("Telemetry Direct stopped");
        }

        private static async Task SendTelemetryLoop(string statusUrl)
        {
            var messageCount = 0;
            
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    // Get status from mock device
                    var response = await _httpClient.GetAsync(statusUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonContent = await response.Content.ReadAsStringAsync();
                        var jsonObj = JObject.Parse(jsonContent);
                        var statusObj = jsonObj["result"];

                        // Add/modify fields for IoT Hub
                        statusObj["smokerId"] = _deviceId;
                        statusObj["id"] = Guid.NewGuid().ToString();
                        
                        if (!string.IsNullOrEmpty(_sessionId))
                        {
                            statusObj["sessionId"] = _sessionId;
                            statusObj["type"] = "status";
                            statusObj["ttl"] = -1; // Permanent for session data
                        }
                        else
                        {
                            statusObj["sessionId"] = null;
                            statusObj["type"] = "telemetry";
                            statusObj["ttl"] = 259200; // 3 days for telemetry
                        }

                        // Create IoT Hub message
                        var messageString = statusObj.ToString();
                        var message = new Message(Encoding.UTF8.GetBytes(messageString))
                        {
                            ContentType = "application/json",
                            ContentEncoding = "utf-8"
                        };

                        // Add message properties
                        message.Properties.Add("messageType", statusObj["type"]?.ToString() ?? "telemetry");
                        message.Properties.Add("deviceId", _deviceId);
                        if (!string.IsNullOrEmpty(_sessionId))
                        {
                            message.Properties.Add("sessionId", _sessionId);
                        }

                        // Send to IoT Hub
                        await _deviceClient.SendEventAsync(message);
                        messageCount++;
                        
                        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Message {messageCount} sent - Type: {statusObj["type"]}, Grill: {statusObj["temps"]?["grillTemp"]}°F, Mode: {statusObj["mode"]}");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to get status from mock device: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending telemetry: {ex.Message}");
                }

                // Wait for next interval
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_telemetryInterval), _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        // Direct method handlers
        private static Task<MethodResponse> SetTelemetryInterval(MethodRequest methodRequest, object userContext)
        {
            var payload = Encoding.UTF8.GetString(methodRequest.Data);
            if (int.TryParse(payload, out var newInterval) && newInterval > 0 && newInterval <= 3600)
            {
                _telemetryInterval = newInterval;
                Console.WriteLine($"Telemetry interval updated to {newInterval} seconds");
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"status\":\"success\"}"), 200));
            }
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"error\":\"Invalid interval\"}"), 400));
        }

        private static Task<MethodResponse> SetSessionId(MethodRequest methodRequest, object userContext)
        {
            var payload = Encoding.UTF8.GetString(methodRequest.Data);
            _sessionId = payload.Trim('"');
            Console.WriteLine($"Session ID set to: {_sessionId}");
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"status\":\"success\"}"), 200));
        }

        private static Task<MethodResponse> EndSession(MethodRequest methodRequest, object userContext)
        {
            _sessionId = "";
            Console.WriteLine("Session ended");
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes("{\"status\":\"success\"}"), 200));
        }

        private static async Task<MethodResponse> GetStatus(MethodRequest methodRequest, object userContext)
        {
            try
            {
                var mockDeviceUrl = Environment.GetEnvironmentVariable("MOCK_DEVICE_URL") ?? "http://mock-device:3000";
                var statusEndpoint = Environment.GetEnvironmentVariable("STATUS_ENDPOINT") ?? "/api/robots/MeatGeekBot/commands/get_status";
                var response = await _httpClient.GetAsync($"{mockDeviceUrl}{statusEndpoint}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return new MethodResponse(Encoding.UTF8.GetBytes(content), 200);
                }
                return new MethodResponse(Encoding.UTF8.GetBytes("{\"error\":\"Failed to get status\"}"), 500);
            }
            catch (Exception ex)
            {
                return new MethodResponse(Encoding.UTF8.GetBytes($"{{\"error\":\"{ex.Message}\"}}"), 500);
            }
        }
    }
}