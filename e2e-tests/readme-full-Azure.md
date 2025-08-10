🔑 Required Azure Connection Strings

  1. IoT Hub Connections

  # For E2E Tests
  export TEST_IOT_HUB_CONNECTION_STRING="HostName=<your-iothub>.azure-devices.net;SharedA
  ccessKeyName=<keyname>;SharedAccessKey=<key>"

  # For IoT Edge (from .env file)  
  export IOTHUB_CONNECTION_STRING="HostName=<your-iothub>.azure-devices.net;SharedAccessK
  eyName=<keyname>;SharedAccessKey=<key>"
  export DEVICE_CONNECTION_STRING="HostName=<your-iothub>.azure-devices.net;DeviceId=<dev
  ice-id>;SharedAccessKey=<device-key>"

  2. Cosmos DB Connection

  export TEST_COSMOS_DB_CONNECTION_STRING="AccountEndpoint=https://<account>.documents.az
  ure.com:443/;AccountKey=<key>;"

  3. Event Grid Connection

  export TEST_EVENT_GRID_CONNECTION_STRING="Endpoint=https://<topic>.eventgrid.azure.net/
  ;AccessKey=<key>"
  # OR separated values:
  export EVENT_GRID_ENDPOINT="https://<topic>.<region>.eventgrid.azure.net/"
  export EVENT_GRID_ACCESS_KEY="<access-key>"

  🏗️ Azure Resources Needed

  1. Azure IoT Hub

  - Purpose: Real device communication, telemetry ingestion
  - Configuration:
    - Device registration for test devices
    - Shared access policies for service and device access
    - Message routing configured

  2. Azure Cosmos DB

  - Purpose: Session data storage, telemetry persistence
  - Configuration:
    - Database: MeatGeek-Sessions
    - Containers: Sessions, Telemetry
    - Partition keys configured properly

  3. Azure Event Grid

  - Purpose: Cross-service messaging, event routing
  - Configuration:
    - Custom topic for MeatGeek events
    - Event subscriptions for service integration
    - Access keys for publishing

  4. Optional: Key Vault

  export AZURE_KEY_VAULT_URL="https://<vault-name>.vault.azure.net/"
  # For storing connection strings securely

  📋 Full-Azure Mode Setup Checklist

  Step 1: Create Azure Resources

  # Resource Group
  az group create --name MeatGeek-E2E-Test --location eastus

  # IoT Hub
  az iot hub create --name meatgeek-e2e-iothub --resource-group MeatGeek-E2E-Test --sku
  S1

  # Cosmos DB
  az cosmosdb create --name meatgeek-e2e-cosmos --resource-group MeatGeek-E2E-Test

  # Event Grid Topic
  az eventgrid topic create --name meatgeek-e2e-events --resource-group MeatGeek-E2E-Test

  Step 2: Get Connection Strings

  # IoT Hub connection string
  az iot hub connection-string show --hub-name meatgeek-e2e-iothub

  # Cosmos DB connection string  
  az cosmosdb keys list --name meatgeek-e2e-cosmos --resource-group MeatGeek-E2E-Test

  # Event Grid access key
  az eventgrid topic key list --name meatgeek-e2e-events --resource-group
  MeatGeek-E2E-Test

  Step 3: Configure Environment

  Create .env.azure file:
  # Azure IoT Hub
  TEST_IOT_HUB_CONNECTION_STRING="HostName=meatgeek-e2e-iothub.azure-devices.net;SharedAc
  cessKeyName=iothubowner;SharedAccessKey=..."
  DEVICE_CONNECTION_STRING="HostName=meatgeek-e2e-iothub.azure-devices.net;DeviceId=e2e-t
  est-device;SharedAccessKey=..."

  # Azure Cosmos DB
  TEST_COSMOS_DB_CONNECTION_STRING="AccountEndpoint=https://meatgeek-e2e-cosmos.documents
  .azure.com:443/;AccountKey=...;"

  # Azure Event Grid
  TEST_EVENT_GRID_CONNECTION_STRING="Endpoint=https://meatgeek-e2e-events.eastus-1.eventg
  rid.azure.net/;AccessKey=..."

  # Test Configuration
  E2E_TEST_MODE=full-azure

  Step 4: Initialize Azure Resources

  # Create test device in IoT Hub
  az iot hub device-identity create --hub-name meatgeek-e2e-iothub --device-id
  e2e-test-device

  # Create Cosmos DB database and containers
  az cosmosdb sql database create --account-name meatgeek-e2e-cosmos --name
  MeatGeek-Sessions
  az cosmosdb sql container create --account-name meatgeek-e2e-cosmos --database-name
  MeatGeek-Sessions --name Sessions --partition-key-path "/id"
  az cosmosdb sql container create --account-name meatgeek-e2e-cosmos --database-name
  MeatGeek-Sessions --name Telemetry --partition-key-path "/deviceId"

  🚀 Running Full-Azure Mode

  # Load environment variables
  source .env.azure

  # Run E2E tests with real Azure services
  E2E_TEST_MODE=full-azure npm test

  # Run specific test suites
  E2E_TEST_MODE=full-azure npm test -- --testPathPattern=workflows
  E2E_TEST_MODE=full-azure npm test -- --testPathPattern=cross-service

  💰 Cost Considerations

  - IoT Hub S1: ~$50/month (includes 400K messages)
  - Cosmos DB: ~$25/month (minimal usage)
  - Event Grid: ~$1/month (first 100K operations free)
  - Total Estimated: ~$75/month for dedicated E2E testing resources

  🔒 Security Best Practices

  1. Use separate Azure subscription for E2E testing
  2. Implement resource lifecycle management - auto-cleanup after tests
  3. Use managed identity when running in Azure DevOps
  4. Store secrets in Key Vault or GitHub Secrets
  5. Implement cost alerts to prevent runaway costs