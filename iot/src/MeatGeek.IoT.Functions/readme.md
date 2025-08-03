# MeatGeek IoT Functions

The IoT Functions application handles IoT device communication, telemetry processing, and real-time data retrieval for the MeatGeek platform. This Azure Functions app provides both HTTP endpoints and Event Grid triggers for comprehensive IoT device management.

## Features

- **Device Communication**: Real-time temperature and status retrieval from IoT devices
- **Database Integration**: Stores and retrieves telemetry data from Cosmos DB
- **Chart Data**: Provides time-series data for visualization
- **Telemetry Management**: Configures device telemetry intervals
- **Event Processing**: Handles IoT telemetry events via Event Grid

## Nx Development Commands

```bash
# Build IoT Functions
nx build MeatGeek.IoT.Functions

# Build IoT WorkerApi  
nx build MeatGeek.IoT.WorkerApi

# Run unit tests
nx test MeatGeek.IoT.Functions.Tests
nx test MeatGeek.IoT.WorkerApi.Tests

# Serve locally for development
nx serve MeatGeek.IoT.Functions

# Lint/format code
nx lint MeatGeek.IoT.Functions
```

## API Endpoints

### HTTP Functions
- `GET /api/GetStatusFromDb` - Retrieve device status from database
- `GET /api/GetStatusFromDevice/{smokerId}` - Get real-time device status
- `GET /api/GetTempsFromDb/{smokerId}` - Get temperature history from database  
- `GET /api/GetTempsFromDevice/{smokerId}` - Get real-time device temperatures
- `GET /api/GetChart/{smokerId}` - Get chart data for visualization
- `POST /api/SetTelemetryInterval/{smokerId}/{interval}` - Configure telemetry interval

### Event Grid Triggers
- **SessionCreated**: Processes new session creation events
- **SessionUpdated**: Handles session update notifications  
- **SessionEnded**: Manages session completion events

## Azure Setup

### Required Azure Resources
- Azure Functions App (Consumption Plan)
- Azure Cosmos DB (for telemetry storage)
- Azure IoT Hub (for device communication)
- Azure Event Grid (for event processing)
- Azure Application Insights (for monitoring)

### Environment Variables
Configure these in Azure Function App settings:
- `CosmosDBConnectionString`
- `IoTHubConnectionString` 
- `EventGridTopicEndpoint`
- `EventGridAccessKey`
- `APPINSIGHTS_INSTRUMENTATIONKEY`

## Local Development

1. Install Azure Functions Core Tools
2. Copy `local.settings.json.example` to `local.settings.json`
3. Configure IoT Hub and Cosmos DB connection strings
4. Run `nx serve MeatGeek.IoT.Functions`

## Testing

The project includes unit tests covering:
- HTTP endpoint functionality
- Event Grid trigger processing
- Data retrieval and storage
- IoT device communication
- Model validation

Run tests with: `nx test MeatGeek.IoT.Functions.Tests`

## Device Integration

The IoT Functions integrate with BBQ/grill devices that provide:
- Temperature readings (meat, ambient, target)
- Device status (fan, auger, lid position)
- Alarms and notifications
- Configuration settings
