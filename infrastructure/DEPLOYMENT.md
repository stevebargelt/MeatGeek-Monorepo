# MeatGeek Infrastructure Deployment Guide

This guide provides instructions for deploying the complete MeatGeek infrastructure from scratch, including multi-environment support.

## Architecture Overview

The MeatGeek platform uses a multi-environment architecture with shared and environment-specific resources:

### Shared Resources (Single instance across all environments)
- **Azure Key Vault**: Stores secrets and connection strings
- **Azure Container Registry**: Hosts container images
- **Azure Cosmos DB**: Single account with environment-specific databases
- **Azure Event Grid**: Event routing and messaging
- **Azure IoT Hub**: Device connectivity and telemetry

### Environment-Specific Resources
- **Resource Groups**: Separate groups for each environment (prod, staging, test)
- **Function Apps**: Isolated compute for each service and environment
- **Storage Accounts**: Environment-specific storage
- **Application Insights**: Separate monitoring per environment

## Prerequisites

1. **Azure CLI** (version 2.40.0 or later)
   ```bash
   az --version
   ```

2. **Azure Subscription** with sufficient permissions to create resources

3. **Logged in to Azure CLI**
   ```bash
   az login
   az account set --subscription <your-subscription-id>
   ```

## Deployment Steps

### 1. Quick Deployment

For a production-only deployment:
```bash
cd infrastructure
./deploy.sh
```

### 2. Multi-Environment Deployment

Deploy production and staging environments:
```bash
./deploy.sh --environments prod-staging
```

Deploy all environments (prod, staging, test):
```bash
./deploy.sh --environments all
```

### 3. Custom Deployment Options

```bash
./deploy.sh \
  --location westus2 \
  --environments prod-staging-test \
  --object-id <your-object-id>
```

**Parameters:**
- `--location`: Azure region (default: northcentralus)
- `--environments`: Which environments to deploy
  - `prod-only`: Production only (default)
  - `prod-staging`: Production and staging
  - `prod-staging-test`: Production, staging, and test
  - `all`: All environments
- `--object-id`: Azure AD object ID for Key Vault access (default: current user)

## Post-Deployment Configuration

### 1. Populate Secrets

After deployment, populate the Key Vault with required secrets:

```bash
./scripts/populate-secrets.sh
```

This script will:
- Generate secure passwords for services
- Retrieve connection strings from deployed resources
- Store all secrets in Key Vault with environment prefixes

### 2. Deploy Application Code

Use GitHub Actions to deploy the application code:

```bash
# Trigger deployment for all services
gh workflow run deploy-all.yml

# Or deploy individual services
gh workflow run sessions-api-ci.yml
gh workflow run device-api-ci.yml
gh workflow run iot-functions-ci.yml
```

### 3. Configure IoT Devices

1. Get the IoT Hub connection string:
   ```bash
   az iot hub connection-string show --hub-name <iot-hub-name>
   ```

2. Register your IoT devices:
   ```bash
   az iot hub device-identity create --hub-name <iot-hub-name> --device-id <device-id>
   ```

3. Configure devices with their connection strings

## Environment-Specific Deployments

### Production Only
```bash
./deploy.sh --environments prod-only
```
Creates:
- MeatGeek-Shared (shared resources)
- MeatGeek-Sessions
- MeatGeek-Device
- MeatGeek-IoT

### Production + Staging
```bash
./deploy.sh --environments prod-staging
```
Additionally creates:
- MeatGeek-Sessions-staging
- MeatGeek-Device-staging
- MeatGeek-IoT-staging

### All Environments
```bash
./deploy.sh --environments all
```
Additionally creates:
- MeatGeek-Sessions-test
- MeatGeek-Device-test
- MeatGeek-IoT-test

## Resource Naming Convention

Resources follow this naming pattern:
- Shared resources: `meatgeek<resource-type>`
- Environment-specific: `meatgeek<resource-type>-<environment>`

Examples:
- Key Vault: `meatgeekkv` (shared)
- Cosmos DB: `meatgeek` (shared)
- Function App: `meatgeeksessionsapi` (prod) or `meatgeeksessionsapi-staging` (staging)

## Monitoring Deployment

Monitor deployment progress:
```bash
# Watch deployment status
az deployment sub show --name <deployment-name> --query properties.provisioningState

# Get deployment operations
az deployment sub operation list --name <deployment-name> --output table
```

## Troubleshooting

### Common Issues

1. **Insufficient Permissions**
   ```
   Error: The client does not have authorization
   ```
   Solution: Ensure your account has Owner or Contributor role on the subscription

2. **Resource Already Exists**
   ```
   Error: A resource with the same name already exists
   ```
   Solution: Either delete existing resources or use a different resource prefix

3. **Region Availability**
   ```
   Error: The subscription is not registered for the resource type
   ```
   Solution: Choose a different region or register the resource provider

### Clean Up Resources

To remove all deployed resources:
```bash
# Delete resource groups (this will delete all resources within them)
az group delete --name MeatGeek-Shared --yes
az group delete --name MeatGeek-Sessions --yes
az group delete --name MeatGeek-Device --yes
az group delete --name MeatGeek-IoT --yes

# For staging/test environments
az group delete --name MeatGeek-Sessions-staging --yes
az group delete --name MeatGeek-Device-staging --yes
az group delete --name MeatGeek-IoT-staging --yes
```

## Cost Optimization

The deployment uses several cost-optimization strategies:
- Shared Cosmos DB account with environment-specific databases
- Single Container Registry for all environments
- Consumption-based Function Apps (pay per execution)
- Free tier options where available (Cosmos DB free tier)

## Security Considerations

- All secrets are stored in Azure Key Vault
- Function Apps use Managed Service Identity (MSI) for authentication
- Network isolation can be added post-deployment
- All services use HTTPS/TLS encryption

## Next Steps

After successful deployment:
1. Set up CI/CD pipelines for automated deployments
2. Configure monitoring and alerts in Application Insights
3. Implement backup strategies for Cosmos DB
4. Set up Azure Policy for compliance
5. Configure cost alerts and budgets