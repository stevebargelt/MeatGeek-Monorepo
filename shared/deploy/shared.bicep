targetScope = 'subscription'

param location string = 'northcentralus'
param tenantId string = subscription().tenantId
param objectId string
@description('Prefixes to be used by all resources deployed by this template')
param resourcePrefix string = 'meatgeek'
@description('Environment name (prod, staging, test)')
@allowed([
  'prod'
  'staging'
  'test'
])
param environment string = 'prod'

// Environment-specific naming
var envSuffix = environment == 'prod' ? '' : '-${environment}'

// Resource Group Names
var sharedRgName = 'MeatGeek-Shared'  // Shared across all environments
var sessionsRgName = environment == 'prod' ? 'MeatGeek-Sessions' : 'MeatGeek-Sessions-${environment}'
var deviceRgName = environment == 'prod' ? 'MeatGeek-Device' : 'MeatGeek-Device-${environment}'
var iotRgName = environment == 'prod' ? 'MeatGeek-IoT' : 'MeatGeek-IoT-${environment}'

// Single Key Vault for all environments
param kvName string = '${resourcePrefix}kv'
var vaultURL = 'https://${kvName}${az.environment().suffixes.keyvaultDns}'

// Cosmos DB - single account, environment-specific databases
param cosmosAccountName string = resourcePrefix
param cosmosDatabaseName string
param cosmosContainerName string = resourcePrefix
param cosmosPartition string = '/smokerId'
param topics_meatgeek_name string // Event topics are environment-specific

// Shared Resource Group - Create once for all environments
resource sharedRg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: sharedRgName
  location: location
}

resource sessionsRg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: sessionsRgName
  location: location
}

resource deviceRg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: deviceRgName
  location: location
}

resource iotRg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: iotRgName
  location: location
}

// IoT Worker API reference
var iotWorkerApiName = environment == 'prod' ? 'meatgeekiot-workerapi' : 'meatgeekiot-${environment}-workerapi'
var meatgeekiot_workerapi_externalid = '/subscriptions/${subscription().subscriptionId}/resourceGroups/${iotRgName}/providers/Microsoft.Web/sites/${iotWorkerApiName}'
var sessionCreatedId = '${meatgeekiot_workerapi_externalid}/functions/SessionCreated'

@description('The SKU of the vault to be created.')
@allowed([
  'standard'
  'premium'
])
param skuName string = 'standard'

// Deploy shared resources within the shared resource group
// Environment-specific resources (databases, topics) are deployed each time
// Truly shared resources (KV, Cosmos account, ACR) are created if they don't exist
module sharedResources 'shared-resources.bicep' = {
  name: 'shared-resources-${environment}'
  scope: sharedRg
  params: {
    location: location
    tenantId: tenantId
    objectId: objectId
    kvName: kvName
    skuName: skuName
    cosmosAccountName: cosmosAccountName
    cosmosDatabaseName: cosmosDatabaseName
    cosmosContainerName: cosmosContainerName
    cosmosPartition: cosmosPartition
    topics_meatgeek_name: topics_meatgeek_name
    meatgeekiot_workerapi_externalid: meatgeekiot_workerapi_externalid
    acrName: acrName
    acrSku: acrSku
    environment: environment
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
output sessionsResourceGroupName string = sessionsRg.name
output deviceResourceGroupName string = deviceRg.name
output iotResourceGroupName string = iotRg.name
