# MeatGeek IoT Edge - Azure Test Device Setup

This guide shows how to run a test IoT device with realistic mock BBQ telemetry that connects to your existing Azure IoT Hub.

## Prerequisites

- Existing Azure IoT Hub with Edge devices configured
- Docker and Docker Compose installed  
- Azure CLI installed and authenticated
- Git repository cloned locally

## Quick Start

### 1. Copy Environment Configuration

```bash
cd iot-edge
cp .env.azure .env
```

The `.env.azure` file contains pre-configured settings for your existing Azure IoT Hub:
- Device ID: `e2e-test-device-1`
- IoT Hub: `testhubmeatgeek`
- Connection string already populated
- Mock device configured for brisket scenario

### 2. Start the Test Device

```bash
./start-test-device.sh
```

This script will:
- Validate your environment configuration
- Build Docker containers for MockDevice and Telemetry module
- Start services in the background
- Display monitoring commands

### 3. Monitor Telemetry in Azure

```bash
az iot hub monitor-events --hub-name testhubmeatgeek --device-id e2e-test-device-1
```

You should see realistic BBQ telemetry flowing every 30 seconds with:
- Temperature progression from startup to steady state
- Component states (auger, blower, igniter) cycling realistically
- Fire health status updates

## What This Setup Includes

### MockDevice Container
- **Realistic BBQ Simulation**: Physics-based temperature curves with proper heating/cooling rates
- **Auto-Start Cooking**: Begins with brisket scenario (12-hour cook simulation)
- **Component Logic**: Auger, blower, igniter cycle based on temperature needs
- **Update Frequency**: Simulation updates every 5 seconds
- **API Endpoint**: Available at `http://localhost:3000`

**Test the MockDevice API:**
```bash
# Health check
curl http://localhost:3000/health

# Get current BBQ status
curl http://localhost:3000/api/robots/MeatGeekBot/commands/get_status | jq

# Change target temperature
curl -X POST "http://localhost:3000/api/simulation/settemp?temperature=275"
```

### Direct Telemetry Connection
- **Simplified Architecture**: Bypasses IoT Edge runtime complexity for testing
- **Direct IoT Hub**: Connects directly to your existing `testhubmeatgeek` IoT Hub
- **Existing Device**: Uses your pre-configured `e2e-test-device-1` Edge device
- **Configurable Interval**: Sends telemetry every 30 seconds (adjustable)
- **Session Support**: Handles cooking session association and TTL logic

### Remote Control via Direct Methods

Control the device remotely using Azure CLI direct method calls:

#### Start a Cooking Session
```bash
az iot hub invoke-device-method \
  --hub-name testhubmeatgeek \
  --device-id e2e-test-device-1 \
  --method-name SetSessionId \
  --method-payload '"test-cook-001"'
```

#### Change Telemetry Frequency
```bash
# Set to 10-second intervals for faster testing
az iot hub invoke-device-method \
  --hub-name testhubmeatgeek \
  --device-id e2e-test-device-1 \
  --method-name SetTelemetryInterval \
  --method-payload '10'
```

#### Get Current Device Status
```bash
az iot hub invoke-device-method \
  --hub-name testhubmeatgeek \
  --device-id e2e-test-device-1 \
  --method-name GetStatus
```

#### End Cooking Session
```bash
az iot hub invoke-device-method \
  --hub-name testhubmeatgeek \
  --device-id e2e-test-device-1 \
  --method-name EndSession
```

## Data Format

The telemetry matches your existing MeatGeek schema with proper session handling:

### Session Mode (Active Cooking)
```json
{
  "id": "uuid",
  "smokerId": "e2e-test-device-1",
  "sessionId": "test-cook-001",
  "type": "status",
  "ttl": -1,
  "augerOn": true,
  "blowerOn": false,
  "igniterOn": false,
  "temps": {
    "grillTemp": 225.3,
    "probe1Temp": 165.7,
    "probe2Temp": 0.0,
    "probe3Temp": 0.0,
    "probe4Temp": 0.0
  },
  "fireHealthy": true,
  "mode": "cooking",
  "setPoint": 225,
  "modeTime": "2024-01-15T10:30:00Z",
  "currentTime": "2024-01-15T14:22:30Z"
}
```

### Non-Session Mode (General Telemetry)
```json
{
  "id": "uuid",
  "smokerId": "e2e-test-device-1",
  "sessionId": null,
  "type": "telemetry",
  "ttl": 259200,
  "augerOn": false,
  "blowerOn": false,
  "igniterOn": false,
  "temps": {
    "grillTemp": 75.2,
    "probe1Temp": 72.1,
    "probe2Temp": 0.0,
    "probe3Temp": 0.0,
    "probe4Temp": 0.0
  },
  "fireHealthy": false,
  "mode": "idle",
  "setPoint": 225,
  "modeTime": "2024-01-15T14:22:30Z",
  "currentTime": "2024-01-15T14:22:30Z"
}
```

## Testing Scenarios

### Scenario 1: Quick Cooking Test
```bash
# Start chicken scenario (1.5 hours)
curl -X POST "http://localhost:3000/api/simulation/start?scenario=chicken"

# Set fast telemetry
az iot hub invoke-device-method \
  --hub-name testhubmeatgeek \
  --device-id e2e-test-device-1 \
  --method-name SetTelemetryInterval \
  --method-payload '5'

# Start session
az iot hub invoke-device-method \
  --hub-name testhubmeatgeek \
  --device-id e2e-test-device-1 \
  --method-name SetSessionId \
  --method-payload '"quick-test-chicken"'
```

### Scenario 2: Temperature Change Testing
```bash
# Change target temperature
curl -X POST "http://localhost:3000/api/simulation/settemp?temperature=275"

# Watch components respond to temperature change
watch -n 2 'curl -s http://localhost:3000/api/robots/MeatGeekBot/commands/get_status | jq ".result | {mode, augerOn, blowerOn, igniterOn, fireHealthy}"'
```

### Scenario 3: Long Cook Simulation
```bash
# Start brisket (12-hour cook)
curl -X POST "http://localhost:3000/api/simulation/start?scenario=brisket"

# Monitor temperature progression
watch -n 30 'curl -s http://localhost:3000/api/robots/MeatGeekBot/commands/get_status | jq ".result.temps"'
```

## Monitoring and Logs

### View Container Logs
```bash
# All services
docker-compose -f docker-compose.azure-test.yml logs -f

# Just telemetry module
docker-compose -f docker-compose.azure-test.yml logs -f telemetry-direct

# Just mock device
docker-compose -f docker-compose.azure-test.yml logs -f mock-device
```

### Monitor Azure IoT Hub
```bash
# Real-time telemetry
az iot hub monitor-events \
  --hub-name testhubmeatgeek \
  --device-id e2e-test-device-1

# Monitor with message properties
az iot hub monitor-events \
  --hub-name testhubmeatgeek \
  --device-id e2e-test-device-1 \
  --properties all

# Monitor for specific session
az iot hub monitor-events \
  --hub-name testhubmeatgeek \
  --device-id e2e-test-device-1 \
  --properties all | grep sessionId
```

### Check Device Twin
```bash
# View current device twin
az iot hub device-twin show \
  --hub-name testhubmeatgeek \
  --device-id e2e-test-device-1

# Update desired properties
az iot hub device-twin update \
  --hub-name testhubmeatgeek \
  --device-id e2e-test-device-1 \
  --set properties.desired.TelemetryInterval=15
```

## Troubleshooting

### Issue: Connection to IoT Hub Failed
```bash
# Verify connection string
echo $DEVICE_CONNECTION_STRING

# Test device exists
az iot hub device-identity show \
  --device-id e2e-test-device-1 \
  --hub-name testhubmeatgeek

# Check container logs for authentication errors
docker-compose -f docker-compose.azure-test.yml logs telemetry-direct
```

### Issue: MockDevice Not Responding
```bash
# Check health endpoint
curl http://localhost:3000/health

# Restart mock device
docker-compose -f docker-compose.azure-test.yml restart mock-device

# Check container status
docker-compose -f docker-compose.azure-test.yml ps
```

### Issue: No Telemetry Visible in Azure
```bash
# Verify direct method works
az iot hub invoke-device-method \
  --hub-name testhubmeatgeek \
  --device-id e2e-test-device-1 \
  --method-name GetStatus

# Check telemetry module logs
docker-compose -f docker-compose.azure-test.yml logs -f telemetry-direct | grep "Message.*sent"
```

## Cleanup

### Stop Services
```bash
cd iot-edge
docker-compose -f docker-compose.azure-test.yml down
```

### Remove Container Images (Optional)
```bash
docker-compose -f docker-compose.azure-test.yml down --rmi all -v
```

### Reset Environment
```bash
# Remove custom .env
rm .env

# Copy fresh template
cp .env.azure .env
```

## Next Steps

Once your test device is working:

1. **Integrate with Sessions API** - Test session creation/management workflow
2. **Configure Event Grid** - Set up event routing for session events
3. **Test IoT Functions** - Verify telemetry processing in Azure Functions
4. **Add Application Insights** - Monitor telemetry flow and performance
5. **Scale to Multiple Devices** - Test with multiple mock devices simultaneously

This setup provides a complete testing environment that mirrors your production IoT Edge architecture while using your existing Azure resources.