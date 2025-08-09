# MeatGeek IoT Test Device Implementation Plan

## Overview

This plan creates a **Mock BBQ Device API** that simulates a real grilling device by exposing the HTTP endpoint that the existing Telemetry module expects (`http://host.docker.internal:3000/api/robots/MeatGeekBot/commands/get_status`). This approach maintains the current architecture while enabling local testing of the complete telemetry pipeline from device → Edge → Azure.

The solution will use Docker containers for both the mock device and existing Telemetry module, with Docker networking to enable communication. The mock device will generate realistic BBQ telemetry data with configurable cooking scenarios.

## Implementation Plan

### Files to Create

**Mock Device API:**
- `iot-edge/mock-device/Program.cs` - ASP.NET Core minimal API server
- `iot-edge/mock-device/MockDevice.csproj` - .NET project configuration
- `iot-edge/mock-device/Dockerfile` - Container build definition
- `iot-edge/mock-device/Models/MockSmokerStatus.cs` - Data model matching existing SmokerStatus
- `iot-edge/mock-device/Services/TelemetrySimulator.cs` - Simulation logic engine
- `iot-edge/mock-device/project.json` - Nx build target configuration

**Docker & Deployment:**
- `iot-edge/docker-compose.test.yml` - Local multi-container orchestration
- `iot-edge/deployment.test.template.json` - Azure IoT Edge test deployment manifest
- `iot-edge/.env.test` - Test environment variables

**Testing:**
- `iot-edge/mock-device/Tests/MockDevice.Tests.csproj` - Test project
- `iot-edge/mock-device/Tests/MockDeviceApiTests.cs` - API endpoint tests
- `iot-edge/mock-device/Tests/TelemetrySimulatorTests.cs` - Simulation logic tests

### Files to Modify

- `iot-edge/nx.json` or project configuration - Add mock-device build target
- `iot-edge/deployment.template.json` - Optional: add mock-device module reference

## Core Functions

**MockDevice API Functions:**
- `GetStatusAsync()` - HTTP GET handler that returns current simulated SmokerStatus JSON response with realistic BBQ data
- `StartCookingSession()` - Initiates a simulated cooking cycle with temperature progression over time
- `UpdateSimulationState()` - Background service that continuously updates device state based on cooking timeline

**TelemetrySimulator Functions:**
- `GenerateGrillTemperature()` - Calculates realistic grill temperature with heating/cooling curves and setpoint tracking
- `GenerateProbeTemperatures()` - Simulates food probe readings with different cooking rates per probe
- `CalculateComponentStates()` - Determines auger/blower/igniter on/off states based on temperature and mode

**Docker Functions:**
- `ConfigureContainerNetworking()` - Sets up Docker network bridge for mock-device to telemetry-module communication
- `MapEnvironmentVariables()` - Passes Azure IoT connection strings and device configuration to containers

## Test Coverage

**API Tests:**
- `GetStatusReturnsValidJsonStructure` - API response matches SmokerStatus schema
- `GetStatusIncludesAllRequiredFields` - All temperature probes and status fields present
- `GetStatusRespondsWithin500ms` - Performance validation for telemetry polling

**Simulation Tests:**
- `TelemetrySimulatorGeneratesRealisticTemperatureRanges` - Temperatures within BBQ operating bounds
- `TelemetrySimulatorSimulatesCookingProgression` - Temperature increases over time during cook
- `TelemetrySimulatorHandlesModeTransitions` - Proper state changes between idle/heating/cooking

**Integration Tests:**
- `DockerContainersCanCommunicateOverNetwork` - Mock device reachable from telemetry module
- `TelemetryModuleSuccessfullyPollsMockDevice` - End-to-end local data flow
- `SimulatedTelemetryFlowsToAzureIoTHub` - Complete pipeline to Azure validation

## Azure IoT Edge Registration Process

**Registration Steps:**
1. Create IoT Edge device in Azure IoT Hub with device-specific connection string
2. Configure Edge device with connection string via environment variables in deployment manifest
3. Use existing `deployment.test.template.json` to deploy both Telemetry module and mock-device module
4. Verify Edge runtime connects and modules are running via Azure portal device twin

**Docker Networking Strategy:**
- Use `docker-compose.test.yml` to create shared network between mock-device (port 3000) and Telemetry module
- Mock-device container accessible via `host.docker.internal:3000` from Telemetry module
- Edge Hub container routes telemetry messages to Azure IoT Hub using existing message routing configuration

## Detailed Implementation Steps

### Step 1: Create Mock Device API

Create a minimal ASP.NET Core API that matches the expected endpoint structure:

**Endpoint**: `GET /api/robots/MeatGeekBot/commands/get_status`
**Response Format**: JSON matching the existing `SmokerStatus` model

```json
{
  "result": {
    "augerOn": true,
    "blowerOn": false,
    "igniterOn": false,
    "temps": {
      "grillTemp": 225.5,
      "probe1Temp": 165.2,
      "probe2Temp": 0.0,
      "probe3Temp": 0.0,
      "probe4Temp": 0.0
    },
    "fireHealthy": true,
    "mode": "cooking",
    "setPoint": 225,
    "modeTime": "2024-01-01T12:00:00Z",
    "currentTime": "2024-01-01T14:30:00Z"
  }
}
```

### Step 2: Implement Telemetry Simulation

**Simulation Features:**
- Realistic temperature curves for heating, cooking, and cooling phases
- Component state logic (auger cycles, blower control, igniter startup)
- Multiple cooking scenarios (startup, steady-state, temperature changes)
- Configurable cook parameters (setpoint, probe targets, cooking duration)

### Step 3: Docker Configuration

**docker-compose.test.yml Structure:**
```yaml
version: '3.8'
services:
  mock-device:
    build: ./mock-device
    ports:
      - "3000:3000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    networks:
      - iot-test-network

  telemetry-module:
    build: ./modules/Telemetry
    depends_on:
      - mock-device
    environment:
      - IOTEDGE_DEVICEID=test-device
      - ClientTransportType=AMQP
    networks:
      - iot-test-network

networks:
  iot-test-network:
    driver: bridge
```

### Step 4: Azure IoT Edge Deployment

**Test Deployment Manifest:**
- Configure Edge Agent and Edge Hub for test environment
- Deploy Telemetry module with test device connection string
- Optional: Deploy mock-device as Edge module for cloud testing
- Configure message routing to Azure IoT Hub

### Step 5: Testing Strategy

**Local Testing:**
1. Run `docker-compose -f docker-compose.test.yml up`
2. Verify mock device responds at `http://localhost:3000/api/robots/MeatGeekBot/commands/get_status`
3. Monitor Telemetry module logs for successful HTTP polling
4. Confirm telemetry data structure matches expected format

**Azure Integration Testing:**
1. Create test IoT Hub and Edge device registration
2. Deploy test manifest to Edge device
3. Monitor IoT Hub for incoming telemetry messages
4. Verify message routing to downstream Azure Functions

## Success Criteria

1. **Mock Device**: Responds with realistic BBQ telemetry data in correct JSON format
2. **Docker Integration**: Containers communicate successfully over Docker network
3. **Telemetry Flow**: Data flows from mock device → Telemetry module → Azure IoT Hub
4. **Session Management**: Direct method calls work for session start/end scenarios
5. **Performance**: Maintains 30-second polling interval without errors
6. **Monitoring**: Logs provide clear visibility into data flow and any issues

This approach leverages the existing Telemetry module unchanged, creates a realistic BBQ simulation that matches the expected data format, and enables full end-to-end testing of the telemetry pipeline from local device through Azure to the cloud functions.