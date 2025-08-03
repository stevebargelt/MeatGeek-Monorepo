# MeatGeek Device API

The Device API provides direct communication with BBQ/grill IoT devices using Azure Relay Service. This Azure Functions application enables real-time device control, status monitoring, and configuration management for MeatGeek IoT devices.

## Features

- **Direct Device Communication**: Real-time communication via Azure Relay Service
- **Device Control**: Set cooking modes, temperatures, and telemetry intervals
- **Status Monitoring**: Retrieve current device status and health information
- **Health Checks**: Built-in health monitoring for the API service
- **Device Management**: Configure and manage multiple IoT devices

## Nx Development Commands

```bash
# Build Device API
nx build MeatGeek.Device.Api

# Run unit tests
nx test MeatGeek.Device.Api.Tests

# Serve locally for development
nx serve MeatGeek.Device.Api

# Lint/format code
nx lint MeatGeek.Device.Api
```

## API Endpoints

### HTTP Functions
- `GET /api/healthcheck` - API health status
- `GET /api/IoTGetStatus/{smokerId}` - Get device status
- `GET /api/IoTGetTemps/{smokerId}` - Get device temperatures
- `POST /api/IoTSetMode/{smokerId}/{mode}` - Set device cooking mode
- `POST /api/IoTSetPoint/{smokerId}/{setpoint}` - Set target temperature
- `POST /api/telemetryinterval/{value}/{smokerId}` - Configure telemetry interval

## Azure Setup

### Required Azure Resources
- Azure Functions App (Consumption Plan)
- Azure Relay Service (for device communication)
- Azure IoT Hub (for device management)
- Azure Application Insights (for monitoring)
- Azure Key Vault (for connection strings)

### Environment Variables
Configure these in Azure Function App settings:
- `IoTHubConnectionString`
- `RelayConnectionString`
- `RelayPath`
- `APPINSIGHTS_INSTRUMENTATIONKEY`

## Local Development

1. Install Azure Functions Core Tools
2. Copy `local.settings.json.example` to `local.settings.json`
3. Configure Azure Relay and IoT Hub connection strings
4. Run `nx serve MeatGeek.Device.Api`

## Testing

The project includes comprehensive unit tests covering:
- API endpoint functionality
- Device communication protocols
- Health check operations
- Error handling scenarios
- Function attribute validation

Run tests with: `nx test MeatGeek.Device.Api.Tests`

## Device Communication

The Device API communicates with IoT devices through Azure Relay Service, providing:
- **Low Latency**: Direct real-time communication
- **Secure Connection**: Encrypted communication channels
- **Bidirectional**: Send commands and receive responses
- **Reliable**: Built-in retry and error handling

### Supported Device Operations
- Temperature monitoring (meat, ambient, target)
- Cooking mode control (smoke, grill, hold)
- Target temperature setting
- Telemetry interval configuration
- Device status and health monitoring