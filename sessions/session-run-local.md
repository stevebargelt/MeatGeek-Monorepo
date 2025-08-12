# Sessions API - Local Development Setup Plan

## Overview
This document outlines the steps to run the MeatGeek Sessions API locally, initially connecting to the production CosmosDB instance to simplify setup since there are no production users yet.

## Prerequisites

### Required Tools
1. **Azure Functions Core Tools v4**
   ```bash
   brew tap azure/functions
   brew install azure-functions-core-tools@4
   # or via npm:
   npm install -g azure-functions-core-tools@4
   ```

2. **.NET 8.0 SDK**
   ```bash
   brew install --cask dotnet-sdk
   # Verify installation:
   dotnet --version
   ```

3. **Azure Storage Emulator Alternative** (Azurite)
   ```bash
   npm install -g azurite
   ```

### Azure Resources Access
- Access to production CosmosDB instance
- Event Grid Topic endpoint and key (optional for local - can be mocked)
- Application Insights key (optional for local)

## Phase 1: Basic Local Setup (Connect to Production CosmosDB)

### Step 1: Create local.settings.json
Create `sessions/src/MeatGeek.Sessions.Api/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "FUNCTIONS_EXTENSION_VERSION": "~4",
    
    // CosmosDB Configuration (Production)
    "CosmosDBConnection": "<PRODUCTION_COSMOSDB_CONNECTION_STRING>",
    "CosmosDbDatabaseName": "meatgeek",
    "CosmosDbCollectionName": "meatgeek",
    
    // Event Grid Configuration (Optional for local)
    "EventGridTopicEndpoint": "https://dummy-endpoint.eventgrid.azure.net/api/events",
    "EventGridTopicKey": "dummy-key-for-local-development",
    
    // Application Insights (Optional for local)
    "APPINSIGHTS_INSTRUMENTATIONKEY": "",
    
    // Logging
    "AzureFunctionsJobHost__logging__console__isEnabled": "true",
    "AzureFunctionsJobHost__logging__logLevel__default": "Information",
    "AzureFunctionsJobHost__logging__logLevel__MeatGeek.Sessions": "Debug"
  },
  "Host": {
    "LocalHttpPort": 7071,
    "CORS": "*",
    "CORSCredentials": false
  }
}
```

### Step 2: Create local.settings.json for WorkerApi
Create `sessions/src/MeatGeek.Sessions.WorkerApi/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "FUNCTIONS_EXTENSION_VERSION": "~4",
    
    // CosmosDB Configuration (Production)
    "CosmosDBConnection": "<PRODUCTION_COSMOSDB_CONNECTION_STRING>",
    "CosmosDbDatabaseName": "meatgeek",
    "CosmosDbCollectionName": "meatgeek",
    
    // Event Grid Configuration
    "EventGridTopicEndpoint": "https://dummy-endpoint.eventgrid.azure.net/api/events",
    "EventGridTopicKey": "dummy-key-for-local-development",
    
    // Application Insights (Optional for local)
    "APPINSIGHTS_INSTRUMENTATIONKEY": "",
    
    // Logging
    "AzureFunctionsJobHost__logging__console__isEnabled": "true",
    "AzureFunctionsJobHost__logging__logLevel__default": "Information"
  },
  "Host": {
    "LocalHttpPort": 7072,
    "CORS": "*",
    "CORSCredentials": false
  }
}
```

### Step 3: Start Azurite (Storage Emulator)
```bash
# Start Azurite in a separate terminal
azurite --silent --location ./azurite-data --debug ./azurite-debug.log
```

### Step 4: Run the Functions Apps

#### Terminal 1: Run Sessions API
```bash
cd sessions/src/MeatGeek.Sessions.Api
func start --verbose
```

#### Terminal 2: Run Sessions WorkerApi (if needed)
```bash
cd sessions/src/MeatGeek.Sessions.WorkerApi
func start --port 7072 --verbose
```

## Getting Production Connection Strings

### Option 1: Azure Portal
1. Navigate to Azure Portal
2. Go to the CosmosDB account (likely named `meatgeek`)
3. Click on "Keys" in the left menu
4. Copy the "Primary Connection String"

### Option 2: Azure CLI
```bash
# Login to Azure
az login

# List CosmosDB accounts
az cosmosdb list --query "[].{name:name, resourceGroup:resourceGroup}" -o table

# Get connection string
az cosmosdb keys list --name meatgeek --resource-group <RESOURCE_GROUP> --type connection-strings
```

## API Endpoints for Testing

Once running locally, the Sessions API will be available at:

- **GET** `http://localhost:7071/api/smoker/{smokerId}/sessions` - Get all sessions
- **POST** `http://localhost:7071/api/smoker/{smokerId}/sessions` - Create session
- **GET** `http://localhost:7071/api/smoker/{smokerId}/sessions/{sessionId}` - Get session by ID
- **PUT** `http://localhost:7071/api/smoker/{smokerId}/sessions/{sessionId}` - Update session
- **DELETE** `http://localhost:7071/api/smoker/{smokerId}/sessions/{sessionId}` - Delete session
- **POST** `http://localhost:7071/api/smoker/{smokerId}/sessions/{sessionId}/end` - End session
- **GET** `http://localhost:7071/api/smoker/{smokerId}/sessions/{sessionId}/chart` - Get session chart
- **GET** `http://localhost:7071/api/smoker/{smokerId}/sessions/statuses` - Get session statuses

## Testing with cURL

```bash
# Get all sessions for a smoker
curl http://localhost:7071/api/smoker/test-smoker-001/sessions

# Create a new session
curl -X POST http://localhost:7071/api/smoker/test-smoker-001/sessions \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Test BBQ Session",
    "description": "Testing local development",
    "targetPitTemp": 225,
    "targetFoodTemp": 195
  }'
```

## Troubleshooting

### Common Issues

1. **Port Already in Use**
   - Change port in local.settings.json `Host.LocalHttpPort`
   - Or kill the process using the port: `lsof -i :7071` then `kill -9 <PID>`

2. **CosmosDB Connection Failed**
   - Verify connection string is correct
   - Check firewall rules on CosmosDB allow your IP
   - Ensure CosmosDB database and container exist

3. **Azurite Not Starting**
   - Ensure port 10000, 10001, 10002 are available
   - Clear Azurite data: `rm -rf ./azurite-data`

4. **Function App Not Starting**
   - Check .NET SDK version: `dotnet --version`
   - Clear bin and obj folders: `rm -rf bin obj`
   - Restore packages: `dotnet restore`

## Phase 2: Event Grid Integration (Optional)

For local Event Grid testing, you have two options:

### Option 1: Mock Event Grid
Keep the dummy values in local.settings.json. The EventGridPublisherService will attempt to publish but fail gracefully.

### Option 2: Use ngrok with Production Event Grid
1. Install ngrok: `brew install ngrok`
2. Start ngrok: `ngrok http 7072`
3. Configure Event Grid subscription to point to ngrok URL
4. Update local.settings.json with real Event Grid credentials

## Phase 3: Local CosmosDB Emulator (Future)

### Windows/Linux with Docker
```bash
docker run -p 8081:8081 -p 10251:10251 -p 10252:10252 -p 10253:10253 -p 10254:10254 \
  -m 3g --cpus=2.0 \
  --name azure-cosmos-emulator \
  -e AZURE_COSMOS_EMULATOR_PARTITION_COUNT=10 \
  -e AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true \
  -it mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator
```

### macOS Alternative
Since CosmosDB Emulator doesn't run natively on macOS:
1. Use Azure Cosmos DB free tier
2. Create a dev-specific database
3. Use connection string in local.settings.json

## Security Considerations

1. **Never commit local.settings.json to git**
   - Already in .gitignore
   - Use local.settings.json.example as template

2. **Use Azure Key Vault references for sensitive data**
   ```json
   "CosmosDBConnection": "@Microsoft.KeyVault(SecretUri=https://meatgeekkv.vault.azure.net/secrets/CosmosDBConnection/)"
   ```

3. **Rotate keys regularly**
   - Use secondary keys for local development
   - Rotate when developers leave the team

## Next Steps

1. ✅ Create local.settings.json with production CosmosDB
2. ✅ Test basic CRUD operations
3. 🔄 Set up local CosmosDB emulator (when needed)
4. 🔄 Configure Event Grid for local testing (when needed)
5. 🔄 Set up debugging in VS Code (optional)

## VS Code Launch Configuration

Add to `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Attach to .NET Functions",
      "type": "coreclr",
      "request": "attach",
      "processId": "${command:azureFunctions.pickProcess}"
    }
  ]
}
```

## Useful Scripts

Create `sessions/run-local.sh`:

```bash
#!/bin/bash

# Start Azurite in background
echo "Starting Azurite..."
azurite --silent --location ./azurite-data &
AZURITE_PID=$!

# Wait for Azurite to start
sleep 3

# Start Sessions API
echo "Starting Sessions API..."
cd src/MeatGeek.Sessions.Api
func start --verbose &
API_PID=$!

# Start Worker API
echo "Starting Worker API..."
cd ../MeatGeek.Sessions.WorkerApi
func start --port 7072 --verbose &
WORKER_PID=$!

# Function to cleanup on exit
cleanup() {
    echo "Stopping services..."
    kill $AZURITE_PID $API_PID $WORKER_PID 2>/dev/null
    exit
}

# Set trap to cleanup on script exit
trap cleanup INT TERM

# Wait for user to press Ctrl+C
echo "Services running. Press Ctrl+C to stop."
wait
```

Make it executable: `chmod +x sessions/run-local.sh`

## Monitoring Local Execution

1. **Function Logs**: Check terminal output
2. **Application Insights**: If configured, view in Azure Portal
3. **CosmosDB Data Explorer**: View documents in Azure Portal
4. **Postman/Insomnia**: Import API collection for testing

## Summary

This plan provides a straightforward path to run the Sessions API locally while connecting to production CosmosDB. The phased approach allows for immediate local development (Phase 1) with options for more sophisticated setups (Phases 2-3) as needed.

Key benefits:
- ✅ Quick setup with minimal configuration
- ✅ No need for local CosmosDB emulator initially
- ✅ Can test with real data structure
- ✅ Event Grid can be mocked for local testing
- ✅ Easy to debug and develop new features

Next immediate action: Get the production CosmosDB connection string and create local.settings.json files.