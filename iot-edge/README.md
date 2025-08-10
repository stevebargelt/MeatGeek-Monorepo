# MeatGeek IoT Edge

This repository contains the IoT Edge components for the MeatGeek BBQ platform, including production modules and comprehensive testing infrastructure.

## Structure

- **[production/](production/)** - Production IoT Edge modules and deployment manifests
- **[test-device/](test-device/)** - Complete test device setup with mock BBQ simulator
- **[integration-tests/](integration-tests/)** - End-to-end integration testing framework
- **[unit-tests/](unit-tests/)** - Unit test projects
- **[development/](development/)** - Development tools and debug configurations
- **[docs/](docs/)** - Technical documentation and architecture guides

## Quick Start

### Production Deployment
See [production/README.md](production/README.md) for deploying to real IoT Edge devices.

### Test Device Setup
Quick start: `./start-test-device.sh` 

See [test-device/README.md](test-device/README.md) for detailed documentation.

## Architecture Overview

This document provides a comprehensive analysis of the MeatGeek IoT Edge components, their architecture, communication patterns, and operational details.

## Architecture Overview

The MeatGeek IoT Edge system acts as a bridge between physical BBQ/grilling devices and the cloud platform, providing real-time telemetry collection, session management, and remote device control capabilities.

### Core Components

1. **Telemetry Module** - Primary edge module for data collection and device communication
2. **Edge Hub** - Message routing and store-and-forward capabilities
3. **Edge Agent** - Container lifecycle management and deployment orchestration

## Telemetry Module Deep Dive

### Primary Responsibilities

- **Data Collection**: Polls BBQ device status every 30 seconds (configurable)
- **Session Management**: Associates telemetry with active cooking sessions
- **Cloud Communication**: Routes data through Edge Hub to IoT Hub
- **Remote Control**: Exposes direct methods for cloud-based device management

### Data Collection Flow

```
BBQ Device API (localhost:3000) → HTTP GET → Telemetry Module → JSON Processing → IoT Hub Message
```

**Source Endpoint**: `http://host.docker.internal:3000/api/robots/MeatGeekBot/commands/get_status`

### Direct Method Handlers

The module exposes five IoT Hub direct methods for remote control:

1. **`SetTelemetryInterval`** (`Program.cs:236`)
   - Adjusts polling frequency (1-60 seconds)
   - Validates input range and updates internal timer
   - Returns success/error response

2. **`SetSessionId`** (`Program.cs:257`)
   - Associates telemetry with specific cooking session
   - Stores session ID in environment variable for persistence
   - Enables session-aware data tagging

3. **`EndSession`** (`Program.cs:293`)
   - Clears session association
   - Resets session environment variable
   - Returns telemetry to non-session mode

4. **`GetTemps`** (`Program.cs:279`)
   - Returns current temperature readings only
   - Provides quick temperature check without full status

5. **`GetStatus`** (`Program.cs:286`)
   - Returns complete smoker status object
   - Includes all sensors, settings, and operational state

## Communication Patterns

### Cloud-to-Device Communication

**Telemetry Interval Control**:
- **Source**: `iot/src/MeatGeek.IoT.Functions/SetTelemetryInterval.cs:72`
- **Trigger**: HTTP API call to Azure Function
- **Flow**: API → IoT Hub Service Client → Direct Method → Telemetry Module

**Session Lifecycle Management**:
- **Session Start**: `iot/src/MeatGeek.IoT.WorkerApi/SessionCreated.cs:49`
  - Event Grid trigger from Sessions API
  - Calls `SetSessionId` direct method
- **Session End**: `iot/src/MeatGeek.IoT.WorkerApi/SessionEnded.cs:49`
  - Event Grid trigger from Sessions API
  - Calls `EndSession` direct method

### Device-to-Cloud Communication

**Message Routing Configuration**:
```json
"routes": {
  "TelemetryToIoTHub": "FROM /messages/modules/Telemetry/outputs/* INTO $upstream"
}
```

**Message Properties**:
- `correlationId`: Unique identifier for request tracking
- `sequenceNumber`: Message ordering (incremental counter)
- `SessionId`: Session association for data context
- `ContentType`: "application/json"
- `ContentEncoding`: "UTF-8"

## Session State Management

### Session-Aware Data Tagging

**Active Session Mode**:
- Data Type: `"status"`
- TTL: `-1` (permanent retention)
- Session ID: Associated with active cooking session
- Purpose: Historical session data preservation

**Non-Session Mode**:
- Data Type: `"telemetry"`
- TTL: `259200` seconds (3 days)
- Session ID: Empty/null
- Purpose: General device monitoring with automatic cleanup

### State Persistence

- Session ID stored in `SESSION_ID` environment variable
- Persists across module restarts via container environment
- Twin property synchronization for distributed state management

## Data Models

### SmokerStatus Object (`Program.cs:347-377`)

```csharp
{
  "id": "string",                    // Unique document identifier
  "ttl": int?,                       // Time-to-live for Cosmos DB
  "smokerId": "string",              // Device identifier
  "sessionId": "string",             // Associated session (if any)
  "type": "string",                  // "status" or "telemetry"
  "augerOn": bool,                   // Pellet auger status
  "blowerOn": bool,                  // Combustion fan status
  "igniterOn": bool,                 // Igniter element status
  "temps": Temps,                    // Temperature readings
  "fireHealthy": bool,               // Fire condition assessment
  "mode": "string",                  // Operating mode
  "setPoint": int,                   // Target temperature
  "modeTime": DateTime,              // Mode change timestamp
  "currentTime": DateTime            // Current device time
}
```

### Temperature Readings (`Program.cs:378-391`)

```csharp
{
  "grillTemp": double,               // Main grill temperature
  "probe1Temp": double,              // Food probe 1
  "probe2Temp": double,              // Food probe 2
  "probe3Temp": double,              // Food probe 3
  "probe4Temp": double               // Food probe 4
}
```

## Deployment Architecture

### Container Configuration

**Multi-Platform Support**:
- `Dockerfile.amd64` - x64 Linux systems
- `Dockerfile.arm32v7` - ARM 32-bit (Raspberry Pi)
- `Dockerfile.arm64v8` - ARM 64-bit systems
- `Dockerfile.windows-amd64` - Windows containers

**Base Images**:
- Build: `mcr.microsoft.com/dotnet/sdk:8.0`
- Runtime: `mcr.microsoft.com/dotnet/runtime:8.0`
- Security: Non-root user (`moduleuser`)

### Network Configuration

**Host Network Integration**:
- Network Mode: `host` for Edge Agent connectivity
- Extra Hosts: `host.docker.internal:host-gateway`
- Port Bindings: `3000/tcp` exposed for device communication

**Container Registry**:
- Azure Container Registry integration
- Build tag format: `${Build.DefinitionName}-${Build.BuildId}-${platform}`
- Automated credential management

## Logging & Monitoring

### Structured Logging Implementation

**Serilog Configuration** (`Program.cs:309-345`):
- Configurable log levels via `RuntimeLogLevel` environment variable
- Console output with structured formatting
- Severity mapping to Syslog standards (RFC 3164)

**Log Level Mapping**:
```csharp
Fatal → 0 (Emergency)
Error → 3 (Error)
Warning → 4 (Warning)
Information → 6 (Informational)
Debug → 7 (Debug)
Verbose → 7 (Debug)
```

### ELMS Integration

**Edge Log Management System**:
- Function App: `iotedgelogsapp-d589c907`
- Configuration: `LogsRegex` app setting for log filtering
- Device Twin Requirement: `tags.logPullEnabled='true'`

**Log Collection Flow**:
```
Module Logs → Docker Log Driver → ELMS Function → Centralized Storage
```

## Connection Resilience

### Connection Status Monitoring (`Program.cs:126-137`)

**Automatic Recovery**:
- Connection status change handler
- Logs status transitions and reasons
- Module restart on critical connection failures

**Critical Failure Conditions**:
- `ConnectionStatusChangeReason.Retry_Expired`
- `ConnectionStatusChangeReason.Client_Close`

**Recovery Strategy**:
- Module self-termination on unrecoverable failures
- Edge Agent automatic restart with exponential backoff
- State preservation through environment variables

## Twin Property Synchronization

### Desired Properties Handler (`Program.cs:214-234`)

**Supported Properties**:
- `TelemetryInterval`: Polling frequency adjustment
- `SessionId`: Session association management

**Reported Properties**:
- Acknowledges desired property changes
- Maintains cloud-edge state synchronization
- Enables distributed configuration management

## Operational Considerations

### Performance Characteristics

**Default Configuration**:
- Telemetry Interval: 30 seconds
- Method Timeout: 15 seconds (connection + response)
- Store-and-Forward: 7200 seconds (2 hours)

**Scaling Considerations**:
- Single device per module instance
- HTTP client reuse for efficiency
- Asynchronous message processing

### Error Handling

**HTTP Communication**:
- Success validation before JSON processing
- Graceful degradation on communication failures
- Retry logic handled by Edge Hub store-and-forward

**Direct Method Invocation**:
- Input validation and range checking
- Structured error responses
- Exception logging with correlation IDs

### Security Best Practices

**Container Security**:
- Non-root user execution (`moduleuser`)
- Minimal runtime container footprint
- Secure credential management through environment variables

**Communication Security**:
- TLS encryption for all IoT Hub communication
- Container registry authentication
- Device certificate-based authentication (Edge runtime)

## Build and Development

### Nx Integration

**Build Command**: `nx build Telemetry`
**Project Type**: .NET 6.0 Console Application
**Runtime**: `linux-arm` (configurable per platform)

### Development Workflow

1. **Local Development**: Use Edge simulator or development environment
2. **Container Build**: Multi-platform Docker builds via Azure DevOps
3. **Registry Push**: Automated container registry deployment
4. **Edge Deployment**: IoT Hub deployment manifest updates

### Debugging and Troubleshooting

**Log Collection**:
```bash
# Remote log collection (from README)
scp pi@10.0.20.30:/home/pi/support_bundle.zip ./support_bundle.zip
```

**Direct Method Testing**:
- Use Azure CLI `az iot hub invoke-device-method`
- Test individual methods before integration
- Monitor module logs for method execution

**Connection Diagnostics**:
- Check Edge Agent logs for deployment status
- Verify network connectivity to `host.docker.internal:3000`
- Validate container registry credentials

This comprehensive architecture enables robust, scalable, and maintainable IoT edge computing for the MeatGeek BBQ platform, with enterprise-grade logging, monitoring, and operational capabilities.

## Testing and Development

### Local Testing with Mock Device

The MeatGeek IoT Edge solution includes a comprehensive testing framework using a mock BBQ device that simulates realistic telemetry data.

#### Quick Start - Local Testing

1. **Set up test environment variables:**
   ```bash
   export TEST_DEVICE_CONNECTION_STRING="HostName=your-iothub.azure-devices.net;DeviceId=test-device;SharedAccessKey=your-key"
   ```

2. **Run integration tests:**
   ```bash
   cd iot-edge/integration-tests
   ./test-runner.sh
   ```

3. **View test results:**
   ```bash
   ls -la test-results/
   ```

### Mock Device Features

The mock device (`iot-edge/mock-device/`) provides:
- **Realistic BBQ simulation** with temperature curves, component cycling, and cooking scenarios
- **Configurable parameters** for different test scenarios
- **Health check endpoint** for container orchestration
- **Multiple cooking scenarios** (startup, steady-state, temperature ramping)

#### Mock Device API Endpoints

- **Status**: `GET /api/robots/MeatGeekBot/commands/get_status` - Returns complete SmokerStatus JSON
- **Health**: `GET /health` - Container health check endpoint

### Testing Strategies

#### 1. Unit Testing

Run unit tests for individual components:

```bash
# Mock device tests
nx test MockDevice.Tests

# Telemetry module tests (if available)
nx test Telemetry.Tests
```

#### 2. Integration Testing

**Full Stack Testing** (mock device + telemetry module + edge hub simulation):
```bash
cd iot-edge/integration-tests
./test-runner.sh
```

**Docker Compose Testing** (simplified local testing):
```bash
cd iot-edge
docker-compose -f docker-compose.test.yml up -d
```

**Manual Testing** (individual components):
```bash
# Start mock device only
cd iot-edge/mock-device
nx serve MockDevice

# Test telemetry collection manually
curl http://localhost:3000/api/robots/MeatGeekBot/commands/get_status
```

#### 3. Azure IoT Hub Testing

**Prerequisites:**
- Azure IoT Hub with device registration
- Device connection string configured
- Azure CLI installed and authenticated

**Register test device:**
```bash
az iot hub device-identity create --hub-name your-iothub --device-id integration-test-device
```

**Deploy to test device:**
```bash
az iot edge set-modules --device-id integration-test-device --hub-name your-iothub --content iot-edge/config/deployment.test.amd64.json
```

**Monitor telemetry:**
```bash
az iot hub monitor-events --hub-name your-iothub --device-id integration-test-device
```

### Test Scenarios

#### Session Management Testing

1. **Session Start/End Workflow:**
   ```bash
   # Call SetSessionId direct method
   az iot hub invoke-device-method --device-id your-device --hub-name your-iothub --method-name SetSessionId --method-payload '{"sessionId":"test-session-001"}'
   
   # Verify session tagging in telemetry
   az iot hub monitor-events --hub-name your-iothub --device-id your-device
   
   # End session
   az iot hub invoke-device-method --device-id your-device --hub-name your-iothub --method-name EndSession
   ```

2. **Telemetry Interval Adjustment:**
   ```bash
   # Change polling frequency to 10 seconds
   az iot hub invoke-device-method --device-id your-device --hub-name your-iothub --method-name SetTelemetryInterval --method-payload '{"interval":10}'
   ```

#### Performance Testing

**Load Testing** (multiple concurrent sessions):
```bash
# Start multiple mock devices
for i in {1..5}; do
  docker run -d -p $((3000+i)):3000 --name mock-device-$i mock-device:latest
done
```

**Stress Testing** (high frequency telemetry):
```bash
export TELEMETRY_INTERVAL=1  # 1-second intervals
./test-runner.sh
```

### Troubleshooting

#### Common Issues

1. **Mock Device Not Responding:**
   ```bash
   # Check container status
   docker-compose -f docker-compose.test.yml ps
   
   # Check logs
   docker-compose -f docker-compose.test.yml logs mock-device
   
   # Test directly
   curl -v http://localhost:3000/health
   ```

2. **Telemetry Module Connection Issues:**
   ```bash
   # Check environment variables
   docker-compose -f docker-compose.test.yml exec telemetry-module env | grep IOTEDGE
   
   # Check connectivity to mock device
   docker-compose -f docker-compose.test.yml exec telemetry-module wget -q -O- http://mock-device:3000/health
   ```

3. **Azure IoT Hub Connectivity:**
   ```bash
   # Validate connection string
   echo $TEST_DEVICE_CONNECTION_STRING | grep -o 'HostName=.*\.azure-devices\.net'
   
   # Test connection with Azure CLI
   az iot hub device-identity show --device-id your-device --hub-name your-iothub
   ```

#### Log Collection and Analysis

**Integration Test Logs:**
```bash
# View test results
cat integration-tests/test-results/integration_test_report_*.json

# Extract performance metrics
jq '.testRun.duration' integration-tests/test-results/integration_test_report_*.json
```

**Component Logs:**
```bash
# Mock device logs
docker-compose -f integration-tests/docker-compose.integration.yml logs mock-device

# Telemetry module logs
docker-compose -f integration-tests/docker-compose.integration.yml logs telemetry-module

# Edge hub simulation logs
docker-compose -f integration-tests/docker-compose.integration.yml logs edgehub
```

### CI/CD Integration

#### GitHub Actions Workflow

The integration tests can be incorporated into CI/CD pipelines:

```yaml
- name: Run IoT Edge Integration Tests
  env:
    TEST_DEVICE_CONNECTION_STRING: ${{ secrets.TEST_DEVICE_CONNECTION_STRING }}
  run: |
    cd iot-edge/integration-tests
    ./test-runner.sh
```

#### Test Result Artifacts

Integration tests generate the following artifacts:
- **JSON test report** with metrics and status
- **Service logs** for debugging
- **Performance measurements** for trend analysis
- **Docker container health status**

### Development Workflow

#### Adding New Test Scenarios

1. **Extend Mock Device** (`iot-edge/mock-device/Services/TelemetrySimulator.cs`):
   ```csharp
   public class CustomScenario : ICookingScenario
   {
       public SmokerStatus GenerateStatus(DateTime currentTime) { /* implementation */ }
   }
   ```

2. **Add Integration Test** (`iot-edge/integration-tests/test-runner.sh`):
   ```bash
   test_custom_scenario() {
       # Test implementation
   }
   ```

3. **Update Docker Configuration** (`integration-tests/docker-compose.integration.yml`):
   ```yaml
   environment:
     - SIMULATION_SCENARIO=custom_scenario
   ```

#### Best Practices

- **Test Environment Isolation**: Each test run uses fresh containers and networks
- **Deterministic Testing**: Mock device provides consistent, predictable data
- **Comprehensive Validation**: Tests cover API responses, message flow, and error handling
- **Performance Monitoring**: Track response times and throughput metrics
- **Log Correlation**: Use correlation IDs to track messages through the pipeline
- **Health Checks**: Implement proper readiness and liveness probes

### Azure Deployment Testing

#### Edge Device Registration

```bash
# Create IoT Hub (if needed)
az iot hub create --name your-iothub --resource-group your-rg --sku S1

# Register Edge device
az iot hub device-identity create --hub-name your-iothub --device-id your-edge-device --edge-enabled

# Get connection string
az iot hub device-identity connection-string show --device-id your-edge-device --hub-name your-iothub
```

#### Deployment Manifest Validation

```bash
# Validate deployment manifest
az iot edge deployment create --deployment-id test-deployment --hub-name your-iothub --content ./config/deployment.test.amd64.json --target-condition "deviceId='your-edge-device'" --priority 10
```

#### Remote Monitoring and Diagnostics

```bash
# Monitor device telemetry
az iot hub monitor-events --hub-name your-iothub --device-id your-edge-device

# Check module status
az iot hub module-identity list --device-id your-edge-device --hub-name your-iothub

# Collect device logs remotely
az iot hub invoke-device-method --device-id your-edge-device --hub-name your-iothub --method-name GetLogs
```

This comprehensive testing framework ensures the MeatGeek IoT Edge solution is robust, reliable, and ready for production deployment with full observability and debugging capabilities.