# MeatGeek Complete System Deployment Guide

This guide explains how to deploy the entire MeatGeek system from scratch using the orchestration Bicep template.

## Overview

The MeatGeek system consists of multiple microservices deployed across different environments. This deployment approach creates a complete, multi-environment infrastructure in a single operation.

## Architecture

### Shared Resources (Single Instance)
- **Resource Group**: `MeatGeek-Shared`
- **Key Vault**: `meatgeekkv` (stores all secrets with environment prefixes)
- **Cosmos DB Account**: `meatgeek` (with environment-specific databases)
- **Container Registry**: `acrmeatgeek...` (shared across all environments)
- **Event Grid Topics**: Environment-specific topics

### Environment-Specific Resources
Each environment gets its own:
- **Sessions Resource Group**: `MeatGeek-Sessions[-environment]`
- **Device Resource Group**: `MeatGeek-Device[-environment]`
- **IoT Resource Group**: `MeatGeek-IoT[-environment]`
- **Function Apps**: Environment-specific instances
- **Cosmos Databases**: Isolated data per environment

## Deployment Options

### Option 1: GitHub Actions (Recommended)

1. **Navigate to Actions**: Go to the "Deploy Complete MeatGeek System" workflow
2. **Click "Run workflow"**
3. **Configure parameters**:
   - **Environments**: Choose from `prod-only`, `prod-staging`, `prod-staging-test`, or `all`
   - **Location**: Azure region (default: `northcentralus`)
   - **Confirmation**: Type `DEPLOY` to confirm
4. **Run the workflow**

### Option 2: Local Deployment Script

```bash
# Set required environment variables
export AZURE_SUBSCRIPTION_ID="your-subscription-id"
export AZURE_OBJECT_ID="your-azure-ad-object-id" 
export ENVIRONMENTS="prod-staging"  # Optional
export AZURE_LOCATION="northcentralus"  # Optional

# Run deployment
./deploy.sh
```

### Option 3: Direct Azure CLI

```bash
# Login to Azure
az login
az account set --subscription "your-subscription-id"

# Deploy complete system
az deployment sub create \
  --location northcentralus \
  --template-file main.bicep \
  --parameters \
    location=northcentralus \
    objectId=your-azure-ad-object-id \
    environmentsTodeploy=prod-staging \
  --name meatgeek-$(date +%Y%m%d-%H%M%S)
```

## Environment Options

| Option | Environments Created | Use Case |
|--------|---------------------|----------|
| `prod-only` | Production only | Minimal deployment |
| `prod-staging` | Production + Staging | Standard deployment |
| `prod-staging-test` | Production + Staging + Test | Full development lifecycle |
| `all` | All environments | Complete setup |

## Prerequisites

### Azure Setup
1. **Azure Subscription** with appropriate permissions
2. **Azure AD Object ID** for Key Vault access
3. **Resource Provider Registration** for all used services

### Local Setup (for script deployment)
1. **Azure CLI** installed and configured
2. **Bicep CLI** (optional, but recommended)
3. **Git** for cloning the repository

### GitHub Secrets (for GitHub Actions)
```
AZURE_CREDENTIALS          # Service principal credentials
AZURE_SUBSCRIPTION         # Subscription ID
STEVE_OBJECT_USER_ID       # Azure AD Object ID
```

## Post-Deployment Steps

### 1. Configure Key Vault Secrets

The system expects these secrets in Key Vault:

#### Shared Secrets
- `SharedCosmosConnectionString[-environment]`
- `EventGridTopicKey[-environment]`
- `IoTEventHubEndpoint`
- `IoTServiceConnection`
- `IoTSharedAccessConnString`

#### Device-Specific Secrets
- `RelayConnectionName[-environment]`
- `RelayKey[-environment]`
- `RelayKeyName[-environment]`
- `RelayNamespace[-environment]`

### 2. Deploy Application Code

After infrastructure is ready, deploy application code:

```bash
# For production
git push origin main

# For staging  
git push origin develop
```

### 3. Validate Deployment

```bash
# Check resource groups
az group list --query "[?starts_with(name, 'MeatGeek')]" -o table

# Check function apps
az functionapp list --query "[?contains(name, 'meatgeek')]" -o table

# Check Key Vault
az keyvault secret list --vault-name meatgeekkv -o table
```

## Troubleshooting

### Common Issues

1. **Permission Errors**
   - Ensure service principal has `Contributor` role
   - Verify Object ID is correct for Key Vault access

2. **Resource Naming Conflicts**
   - Bicep uses `uniqueString()` for globally unique names
   - Check if resources already exist with same names

3. **Quota Limits**
   - Some regions have limits on Function Apps or Cosmos DB
   - Try different region or request quota increase

### Validation Commands

```bash
# Check deployment status
az deployment sub show --name your-deployment-name

# List all MeatGeek resources
az resource list --query "[?contains(name, 'meatgeek')]" -o table

# Check function app status
az functionapp show --name meatgeeksessionsapi --resource-group MeatGeek-Sessions
```

## Cleanup

To remove all resources:

```bash
# Delete all MeatGeek resource groups
az group list --query "[?starts_with(name, 'MeatGeek')].name" -o tsv | \
  xargs -I {} az group delete --name {} --yes --no-wait
```

⚠️ **Warning**: This will delete ALL MeatGeek resources including data!

## Cost Considerations

### Shared Resources (Cost-Optimized)
- **Cosmos DB**: Free tier (first 1000 RU/s and 25GB)
- **Key Vault**: ~$0.03/10,000 operations
- **Container Registry**: Basic tier ~$5/month
- **Event Grid**: First 100,000 operations free

### Per-Environment Resources
- **Function Apps**: Consumption plan (pay per execution)
- **Application Insights**: First 5GB/month free
- **Storage Accounts**: ~$0.05/GB/month

**Estimated Monthly Cost**:
- Production only: ~$10-20/month
- Production + Staging: ~$15-30/month
- All environments: ~$25-50/month

## Security Best Practices

1. **Use managed identities** for service-to-service authentication
2. **Enable Key Vault soft delete** (already configured)
3. **Network security groups** for Function Apps (future enhancement)
4. **Private endpoints** for production workloads (future enhancement)
5. **Regular secret rotation** using Key Vault

## Monitoring and Observability

The deployment includes:
- **Application Insights** for each environment
- **Log Analytics Workspaces** for centralized logging
- **Azure Monitor** for metrics and alerts

Configure additional monitoring:
```bash
# Enable diagnostic settings
az monitor diagnostic-settings create \
  --resource /subscriptions/.../resourceGroups/MeatGeek-Shared/providers/Microsoft.KeyVault/vaults/meatgeekkv \
  --name KeyVaultDiagnostics \
  --logs '[{"category":"AuditEvent","enabled":true}]'
```

## Support

For issues or questions:
1. Check the [troubleshooting section](#troubleshooting)
2. Review Azure portal for deployment errors
3. Check GitHub Actions logs for CI/CD issues
4. Open an issue in the repository