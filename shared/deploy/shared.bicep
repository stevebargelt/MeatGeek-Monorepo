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

// Outputs - Get values from existing shared resources
var existingKeyVault = environment == 'prod' ? sharedResources : null
var kvName = 'meatgeekkv'
var cosmosName = 'meatgeek'
var acrName = 'acrmeatgeek${uniqueString(sharedRg.id)}'
var iotName = 'meatgeekiothub'

output keyVaultName string = kvName
output cosmosAccountName string = cosmosName
output containerRegistryName string = environment == 'prod' && existingKeyVault != null ? sharedResources.outputs.containerRegistryName : acrName
output eventGridTopicEndpoint string = environment == 'prod' && existingKeyVault != null ? sharedResources.outputs.eventGridTopicEndpoint : 'https://placeholder.eventgrid.azure.net'
output eventGridTopicKey string = environment == 'prod' && existingKeyVault != null ? sharedResources.outputs.eventGridTopicKey : 'placeholder-key'
output iotHubName string = iotName
output iotEventHubEndpoint string = environment == 'prod' && existingKeyVault != null ? sharedResources.outputs.iotEventHubEndpoint : 'sb://placeholder.servicebus.windows.net'
output iotServiceConnection string = environment == 'prod' && existingKeyVault != null ? sharedResources.outputs.iotServiceConnection : 'HostName=placeholder.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=placeholder'
output cosmosConnectionString string = environment == 'prod' && existingKeyVault != null ? sharedResources.outputs.cosmosConnectionString : 'AccountEndpoint=https://placeholder.documents.azure.com:443/;AccountKey=placeholder;'

