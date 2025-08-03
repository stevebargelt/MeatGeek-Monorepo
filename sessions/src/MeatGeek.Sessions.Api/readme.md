# MeatGeek Sessions API

The Sessions API manages cooking sessions (cooks, smokes, BBQs) for the MeatGeek platform. This Azure Functions application provides comprehensive CRUD operations for session management and integrates with IoT telemetry data.

## Features

- **Session Management**: Create, read, update, and delete cooking sessions
- **Chart Data**: Retrieve time-series temperature and status data
- **Event Integration**: Publishes session events to Event Grid for downstream processing
- **Swagger Documentation**: Interactive API documentation available

## Nx Development Commands

```bash
# Build the Sessions API
nx build MeatGeek.Sessions.Api

# Build the Services library
nx build MeatGeek.Sessions.Services

# Run unit tests
nx test MeatGeek.Sessions.Api.Tests
nx test MeatGeek.Sessions.Services.Tests

# Run integration tests for WorkerApi
nx test MeatGeek.Sessions.WorkerApi.Tests

# Serve locally for development
nx serve MeatGeek.Sessions.Api

# Lint/format code
nx lint MeatGeek.Sessions.Api
```

## API Endpoints

### Production URLs
- **API Base**: https://meatgeek-sessions.azurewebsites.net/api/
- **Proxy**: https://meatgeek.azurewebsites.net/sessions
- **Swagger UI**: https://meatgeeksessionsapi.azurewebsites.net/api/swagger/ui

### Available Endpoints
- `POST /api/` - Create new session
- `GET /api/{traceid?}/{parentspanid?}` - Get all sessions
- `GET /api/{id}` - Get session by ID
- `GET /api/chart/{id}/{timeseries?}` - Get session chart data
- `PUT /api/{id}` - Update session
- `DELETE /api/{id}` - Delete session
- `POST /api/end/{id}` - End active session

## Azure Setup

### Required Azure Resources
- Azure Functions App (Consumption Plan)
- Azure Cosmos DB (for session storage)
- Azure Event Grid (for session events)
- Azure Application Insights (for monitoring)

### Environment Variables
Configure these in Azure Function App settings:
- `AzureCosmosDBConnectionString`
- `EventGridTopicEndpoint`
- `EventGridAccessKey`
- `APPINSIGHTS_INSTRUMENTATIONKEY`

## Local Development

1. Install Azure Functions Core Tools
2. Copy `local.settings.json.example` to `local.settings.json`
3. Configure connection strings and keys
4. Run `nx serve MeatGeek.Sessions.Api`

## Testing

The project includes comprehensive unit tests covering:
- API endpoint functionality
- Service layer logic
- Data repository operations
- Event publishing
- Model validation

Run tests with: `nx test MeatGeek.Sessions.Api.Tests`
