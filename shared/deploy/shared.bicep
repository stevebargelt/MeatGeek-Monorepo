targetScope = 'subscription'

param location string = 'westus2'
param tenantId string = subscription().tenantId
param objectId string
@description('Prefixes to be used by all resources deployed by this template')
param resourcePrefix string = 'meatgeek'
@description('Environments to create databases for')
param environments array = ['prod']

// This module creates shared resources and databases for all environments

// Resource Group Names
var sharedRgName = 'MeatGeek-Shared'  // Shared across all environments

// Single Key Vault for all environments - append unique string for global uniqueness
param kvName string = '${resourcePrefix}kv${substring(uniqueString(subscription().subscriptionId), 0, 5)}'
var vaultURL = 'https://${kvName}${az.environment().suffixes.keyvaultDns}'

// Cosmos DB - single account, environment-specific databases
param cosmosAccountName string = resourcePrefix
param cosmosContainerName string = resourcePrefix
param cosmosPartition string = '/smokerId'

// Shared Resource Group - Create once for all environments
resource sharedRg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: sharedRgName
  location: location
}

// Create resource groups for each environment
resource sessionsRgs 'Microsoft.Resources/resourceGroups@2021-04-01' = [for env in environments: {
  name: env == 'prod' ? 'MeatGeek-Sessions' : 'MeatGeek-Sessions-${env}'
  location: location
}]

resource deviceRgs 'Microsoft.Resources/resourceGroups@2021-04-01' = [for env in environments: {
  name: env == 'prod' ? 'MeatGeek-Device' : 'MeatGeek-Device-${env}'
  location: location
}]

resource iotRgs 'Microsoft.Resources/resourceGroups@2021-04-01' = [for env in environments: {
  name: env == 'prod' ? 'MeatGeek-IoT' : 'MeatGeek-IoT-${env}'
  location: location
}]

// Removed IoT Worker API reference - this is environment-specific and should be handled in deployment

@description('The SKU of the vault to be created.')
@allowed([
  'standard'
  'premium'
])
param skuName string = 'standard'

// Deploy shared resources within the shared resource group
// This creates shared resources once and databases for all environments
module sharedResources 'shared-resources.bicep' = {
  name: 'shared-resources'
  scope: sharedRg
  params: {
    location: location
    tenantId: tenantId
    objectId: objectId
    kvName: kvName
    skuName: skuName
    cosmosAccountName: cosmosAccountName
    cosmosContainerName: cosmosContainerName
    cosmosPartition: cosmosPartition
    acrName: acrName
    acrSku: acrSku
    environments: environments
  }
}

// ACR parameters (defined here since ACR is shared across all environments)
@minLength(5)
@maxLength(50)
@description('Provide a globally unique name of your Azure Container Registry')
param acrName string = 'acr${resourcePrefix}${uniqueString(subscription().subscriptionId)}'
@description('Provide a tier of your Azure Container Registry.')
param acrSku string = 'Basic'

// Outputs from the shared resources module
output loginServer string = sharedResources.outputs.loginServer
output keyVaultName string = sharedResources.outputs.keyVaultName
output cosmosAccountName string = sharedResources.outputs.cosmosAccountName
output sharedResourceGroupName string = sharedRg.name
