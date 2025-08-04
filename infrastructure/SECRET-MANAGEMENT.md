# MeatGeek Secret Management Strategy

This document outlines the automated approaches for managing secrets in Azure Key Vault after infrastructure deployment.

## Overview

The MeatGeek system requires various secrets for service-to-service communication. This guide provides multiple strategies for automating secret population.

## Secret Categories

### 1. **Auto-Generated Secrets**
These can be generated during deployment:
- Event Grid Topic Keys
- Storage Account Keys
- Temporary/Development passwords

### 2. **Resource-Derived Secrets**
Retrieved from Azure resources after creation:
- Cosmos DB Connection Strings (handled by Bicep)
- Storage Account Connection Strings
- Event Grid Topic Access Keys

### 3. **External Secrets**
Must be provided from external sources:
- IoT Hub Connection Strings
- Azure Relay Credentials
- Third-party API Keys

## Automation Strategies

### Strategy 1: Post-Deployment Script (Recommended)

```bash
# After infrastructure deployment
cd infrastructure/scripts
./populate-secrets.sh

# For specific environments
ENVIRONMENTS=prod,staging ./populate-secrets.sh
```

**Features:**
- Generates secure random values
- Retrieves actual values from deployed resources
- Skips existing secrets (idempotent)
- Provides clear logging

### Strategy 2: GitHub Actions Integration

Use the `sync-secrets-to-keyvault.yml` workflow to:
1. Sync GitHub Secrets to Key Vault
2. Generate missing secrets
3. Validate secret presence

```yaml
# Trigger manually from Actions tab
# Select environment and secret type
# Automated validation report
```

### Strategy 3: Bicep-Time Generation

Add to your main.bicep:

```bicep
module generateSecrets 'modules/generate-secrets.bicep' = {
  name: 'generate-secrets-${environment}'
  scope: resourceGroup(sharedRg.name)
  params: {
    keyVaultName: keyVaultName
    environment: environment
  }
}
```

## Required Secrets by Service

### Sessions API
```
SharedCosmosConnectionString[-env]
EventGridTopicKey[-env]
[storage-account-name]-ConnectionString
```

### Device API
```
SharedCosmosConnectionString[-env]
RelayNamespace[-env]
RelayConnectionName[-env]
RelayKeyName[-env]
RelayKey[-env]
```

### IoT Functions
```
SharedCosmosConnectionString[-env]
IoTServiceConnection
IoTEventHubEndpoint
IoTSharedAccessConnString
EventGridTopicKey[-env]
```

## Implementation Guide

### Step 1: Deploy Infrastructure
```bash
cd infrastructure
./deploy.sh
```

### Step 2: Populate Base Secrets
```bash
cd scripts
./populate-secrets.sh
```

### Step 3: Configure External Secrets

For production IoT Hub (example):
```bash
# Get IoT Hub connection string
az iot hub connection-string show \
  --hub-name meatgeek-iothub \
  --policy-name service

# Set in Key Vault
az keyvault secret set \
  --vault-name meatgeekkv \
  --name IoTServiceConnection \
  --value "<connection-string>"
```

### Step 4: Validate Secrets
```bash
# List all secrets
az keyvault secret list \
  --vault-name meatgeekkv \
  --query "[].{Name:name,Enabled:attributes.enabled}" \
  -o table

# Check specific secret
az keyvault secret show \
  --vault-name meatgeekkv \
  --name EventGridTopicKey-staging
```

## Security Best Practices

### 1. **Use Managed Identities**
Where possible, use managed identities instead of connection strings:
```bicep
// In your Function App configuration
identity: {
  type: 'SystemAssigned'
}
```

### 2. **Key Vault References**
Use Key Vault references in app settings:
```
@Microsoft.KeyVault(VaultName=meatgeekkv;SecretName=IoTServiceConnection)
```

### 3. **Secret Rotation**
Enable automatic rotation for supported services:
```bash
az keyvault key rotation-policy update \
  --vault-name meatgeekkv \
  --name mykey \
  --policy @rotation-policy.json
```

### 4. **Access Policies**
Limit access to only required services:
```bash
# Grant Function App access
az keyvault set-policy \
  --name meatgeekkv \
  --object-id <managed-identity-id> \
  --secret-permissions get list
```

## Troubleshooting

### Common Issues

**1. "Secret already exists" errors**
- This is expected behavior - script skips existing secrets
- To overwrite: delete the secret first or use `--force` flag

**2. "Access denied" to Key Vault**
- Ensure your Azure AD account has proper permissions
- Check Key Vault access policies

**3. "Resource not found" when retrieving values**
- Ensure infrastructure deployment completed successfully
- Check resource names match expected patterns

### Debug Commands

```bash
# Check Key Vault access
az keyvault show --name meatgeekkv

# Test secret creation
az keyvault secret set \
  --vault-name meatgeekkv \
  --name test-secret \
  --value test-value

# Check Function App identity
az functionapp identity show \
  --name meatgeeksessionsapi \
  --resource-group MeatGeek-Sessions
```

## Integration with CI/CD

### GitHub Actions Secrets

Required GitHub secrets for automation:
```
AZURE_CREDENTIALS
AZURE_SUBSCRIPTION
IOT_SERVICE_CONNECTION (if available)
IOT_EVENTHUB_ENDPOINT (if available)
```

### Deployment Pipeline

1. Infrastructure deployment creates Key Vault
2. Post-deployment job runs secret population
3. Application deployment uses Key Vault references
4. Runtime: Apps retrieve secrets from Key Vault

## Future Enhancements

1. **Azure Key Vault CSI Driver** for Kubernetes workloads
2. **HashiCorp Vault** integration for multi-cloud
3. **Certificate management** for SSL/TLS
4. **Secret scanning** in CI/CD pipeline
5. **Automated rotation** schedules

## Quick Reference

```bash
# Populate all secrets for staging
ENVIRONMENTS=staging ./populate-secrets.sh

# Sync from GitHub to Key Vault
# Use GitHub Actions workflow

# Validate all secrets exist
./validate-secrets.sh prod

# Generate secret template
cat ../templates/secrets-template.json
```