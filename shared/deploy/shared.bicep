targetScope = 'subscription'

param location string = 'northcentralus'
param tenantId string = subscription().tenantId
param objectId string
@description('Prefixes to be used by all resources deployed by this template')
param resourcePrefix string = 'meatgeek'
@description('Environment name')
param environment string = 'prod'

// Resource group names
var sharedRgName = 'MeatGeek-Shared'  // Shared across all environments
var sessionsRgName = environment == 'prod' ? 'MeatGeek-Sessions' : 'MeatGeek-Sessions-${environment}'
var deviceRgName = environment == 'prod' ? 'MeatGeek-Device' : 'MeatGeek-Device-${environment}'
var iotRgName = environment == 'prod' ? 'MeatGeek-IoT' : 'MeatGeek-IoT-${environment}'

// Create resource groups
resource sharedRg 'Microsoft.Resources/resourceGroups@2021-04-01' = if (environment == 'prod') {
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

// Deploy shared resources module (only for prod environment)
module sharedResources 'shared-resources.bicep' = if (environment == 'prod') {
  name: 'shared-resources'
  scope: sharedRg
  params: {
    location: location
    tenantId: tenantId
    objectId: objectId
    resourcePrefix: resourcePrefix
  }
}

// Outputs
output keyVaultName string = environment == 'prod' ? sharedResources.outputs.keyVaultName : 'meatgeekkv'
output cosmosAccountName string = environment == 'prod' ? sharedResources.outputs.cosmosAccountName : 'meatgeek'
output containerRegistryName string = environment == 'prod' ? sharedResources.outputs.containerRegistryName : sharedResources.outputs.containerRegistryName
output eventGridTopicEndpoint string = environment == 'prod' ? sharedResources.outputs.eventGridTopicEndpoint : sharedResources.outputs.eventGridTopicEndpoint
output eventGridTopicKey string = environment == 'prod' ? sharedResources.outputs.eventGridTopicKey : sharedResources.outputs.eventGridTopicKey
output iotHubName string = environment == 'prod' ? sharedResources.outputs.iotHubName : sharedResources.outputs.iotHubName
output iotEventHubEndpoint string = environment == 'prod' ? sharedResources.outputs.iotEventHubEndpoint : sharedResources.outputs.iotEventHubEndpoint
output iotServiceConnection string = environment == 'prod' ? sharedResources.outputs.iotServiceConnection : sharedResources.outputs.iotServiceConnection
output cosmosConnectionString string = environment == 'prod' ? sharedResources.outputs.cosmosConnectionString : sharedResources.outputs.cosmosConnectionString

