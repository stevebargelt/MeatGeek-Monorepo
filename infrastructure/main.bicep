targetScope = 'subscription'

@description('Location for all resources')
param location string = 'northcentralus'

@description('Azure AD Object ID for Key Vault access')
param objectId string

@description('Environments to deploy (prod is always included)')
@allowed([
  'prod-only'
  'prod-staging'
  'prod-staging-test'
  'all'
])
param environmentsTodeploy string = 'prod-only'

@description('Prefixes to be used by all resources')
param resourcePrefix string = 'meatgeek'

// Shared parameters
param cosmosAccountName string = resourcePrefix
param cosmosContainerName string = resourcePrefix
param cosmosPartition string = '/smokerId'

// Environment configuration
var environments = environmentsTodeploy == 'prod-only' ? ['prod'] : 
                  environmentsTodeploy == 'prod-staging' ? ['prod', 'staging'] :
                  environmentsTodeploy == 'prod-staging-test' ? ['prod', 'staging', 'test'] :
                  ['prod', 'staging', 'test']

// 1. Deploy shared infrastructure (creates resource groups and shared resources)
module sharedInfrastructure 'shared/deploy/shared.bicep' = [for environment in environments: {
  name: 'shared-infra-${environment}'
  params: {
    location: location
    objectId: objectId
    resourcePrefix: resourcePrefix
    environment: environment
    cosmosAccountName: cosmosAccountName
    cosmosDatabaseName: environment == 'prod' ? resourcePrefix : '${resourcePrefix}-${environment}'
    cosmosContainerName: cosmosContainerName
    cosmosPartition: cosmosPartition
    topics_meatgeek_name: environment == 'prod' ? '${resourcePrefix}-session' : '${resourcePrefix}-session-${environment}'
  }
}]

// 2. Deploy Sessions microservices for each environment
module sessionsInfrastructure 'sessions/deploy/microservice.bicep' = [for (environment, i) in environments: {
  name: 'sessions-infra-${environment}'
  scope: resourceGroup(sharedInfrastructure[i].outputs.sessionsResourceGroupName)
  dependsOn: [
    sharedInfrastructure[i]
  ]
  params: {
    location: location
    environment: environment
    resourcePrefix: resourcePrefix
    resourceProject: 'sessions'
    keyVaultName: sharedInfrastructure[i].outputs.keyVaultName
    keyVaultResourceGroup: sharedInfrastructure[i].outputs.sharedResourceGroupName
    cosmosAccountName: cosmosAccountName
    cosmosDbDatabaseName: environment == 'prod' ? resourcePrefix : '${resourcePrefix}-${environment}'
    cosmosDbCollectionName: cosmosContainerName
    cosmosConnectionString: '@Microsoft.KeyVault(VaultName=${sharedInfrastructure[i].outputs.keyVaultName};SecretName=SharedCosmosConnectionString${environment == 'prod' ? '' : '-${environment}'})'
    eventGridTopicEndpoint: 'https://${resourcePrefix}-session${environment == 'prod' ? '' : '-${environment}'}.${location}-1.eventgrid.azure.net/api/events'
    eventGridTopicKey: '@Microsoft.KeyVault(VaultName=${sharedInfrastructure[i].outputs.keyVaultName};SecretName=EventGridTopicKey${environment == 'prod' ? '' : '-${environment}'})'
    iotEventHubEndpoint: '@Microsoft.KeyVault(VaultName=${sharedInfrastructure[i].outputs.keyVaultName};SecretName=IoTEventHubEndpoint)'
    iotServiceConnection: '@Microsoft.KeyVault(VaultName=${sharedInfrastructure[i].outputs.keyVaultName};SecretName=IoTServiceConnection)'
  }
}]

// 3. Deploy Sessions Worker API for each environment
module sessionsWorkerInfrastructure 'sessions/deploy/microservice-worker.bicep' = [for (environment, i) in environments: {
  name: 'sessions-worker-infra-${environment}'
  scope: resourceGroup(sharedInfrastructure[i].outputs.sessionsResourceGroupName)
  dependsOn: [
    sharedInfrastructure[i]
  ]
  params: {
    location: location
    environment: environment
    resourcePrefix: resourcePrefix
    resourceProject: 'sessions-worker'
    keyVaultName: sharedInfrastructure[i].outputs.keyVaultName
    keyVaultResourceGroup: sharedInfrastructure[i].outputs.sharedResourceGroupName
    cosmosAccountName: cosmosAccountName
    cosmosDbDatabaseName: environment == 'prod' ? resourcePrefix : '${resourcePrefix}-${environment}'
    cosmosDbCollectionName: cosmosContainerName
    cosmosConnectionString: '@Microsoft.KeyVault(VaultName=${sharedInfrastructure[i].outputs.keyVaultName};SecretName=SharedCosmosConnectionString${environment == 'prod' ? '' : '-${environment}'})'
    eventGridTopicEndpoint: 'https://${resourcePrefix}-session${environment == 'prod' ? '' : '-${environment}'}.${location}-1.eventgrid.azure.net/api/events'
    eventGridTopicKey: '@Microsoft.KeyVault(VaultName=${sharedInfrastructure[i].outputs.keyVaultName};SecretName=EventGridTopicKey${environment == 'prod' ? '' : '-${environment}'})'
    iotEventHubEndpoint: '@Microsoft.KeyVault(VaultName=${sharedInfrastructure[i].outputs.keyVaultName};SecretName=IoTEventHubEndpoint)'
    iotServiceConnection: '@Microsoft.KeyVault(VaultName=${sharedInfrastructure[i].outputs.keyVaultName};SecretName=IoTServiceConnection)'
  }
}]

// 4. Deploy Device microservices for each environment
module deviceInfrastructure 'device/deploy/microservice.bicep' = [for (environment, i) in environments: {
  name: 'device-infra-${environment}'
  scope: resourceGroup(sharedInfrastructure[i].outputs.deviceResourceGroupName)
  dependsOn: [
    sharedInfrastructure[i]
  ]
  params: {
    location: location
    environment: environment
    resourcePrefix: resourcePrefix
    resourceProject: 'device'
    keyVaultName: sharedInfrastructure[i].outputs.keyVaultName
    keyVaultResourceGroup: sharedInfrastructure[i].outputs.sharedResourceGroupName
  }
}]

// 5. Deploy IoT microservices for each environment
module iotInfrastructure 'iot/deploy/api.bicep' = [for (environment, i) in environments: {
  name: 'iot-infra-${environment}'
  scope: resourceGroup(sharedInfrastructure[i].outputs.iotResourceGroupName)
  dependsOn: [
    sharedInfrastructure[i]
  ]
  params: {
    location: location
    environment: environment
    resourcePrefix: resourcePrefix
    resourceProject: 'iot-api'
    keyVaultName: sharedInfrastructure[i].outputs.keyVaultName
    keyVaultResourceGroup: sharedInfrastructure[i].outputs.sharedResourceGroupName
    cosmosAccountName: cosmosAccountName
    cosmosDbDatabaseName: environment == 'prod' ? resourcePrefix : '${resourcePrefix}-${environment}'
    cosmosDbCollectionName: cosmosContainerName
    cosmosConnectionString: '@Microsoft.KeyVault(VaultName=${sharedInfrastructure[i].outputs.keyVaultName};SecretName=SharedCosmosConnectionString${environment == 'prod' ? '' : '-${environment}'})'
    eventGridTopicEndpoint: 'https://${resourcePrefix}-session${environment == 'prod' ? '' : '-${environment}'}.${location}-1.eventgrid.azure.net/api/events'
    eventGridTopicKey: '@Microsoft.KeyVault(VaultName=${sharedInfrastructure[i].outputs.keyVaultName};SecretName=EventGridTopicKey${environment == 'prod' ? '' : '-${environment}'})'
    iotEventHubEndpoint: '@Microsoft.KeyVault(VaultName=${sharedInfrastructure[i].outputs.keyVaultName};SecretName=IoTEventHubEndpoint)'
    iotServiceConnection: '@Microsoft.KeyVault(VaultName=${sharedInfrastructure[i].outputs.keyVaultName};SecretName=IoTServiceConnection)'
    iotSharedAccessConnString: '@Microsoft.KeyVault(VaultName=${sharedInfrastructure[i].outputs.keyVaultName};SecretName=IoTSharedAccessConnString)'
  }
}]

// 6. Deploy IoT Worker API for each environment
module iotWorkerInfrastructure 'iot/deploy/microservice-worker.bicep' = [for (environment, i) in environments: {
  name: 'iot-worker-infra-${environment}'
  scope: resourceGroup(sharedInfrastructure[i].outputs.iotResourceGroupName)
  dependsOn: [
    sharedInfrastructure[i]
  ]
  params: {
    location: location
    environment: environment
    resourcePrefix: resourcePrefix
    resourceProject: 'iot-worker'
    keyVaultName: sharedInfrastructure[i].outputs.keyVaultName
    keyVaultResourceGroup: sharedInfrastructure[i].outputs.sharedResourceGroupName
    cosmosAccountName: cosmosAccountName
    cosmosDbDatabaseName: environment == 'prod' ? resourcePrefix : '${resourcePrefix}-${environment}'
    cosmosDbCollectionName: cosmosContainerName
    cosmosConnectionString: '@Microsoft.KeyVault(VaultName=${sharedInfrastructure[i].outputs.keyVaultName};SecretName=SharedCosmosConnectionString${environment == 'prod' ? '' : '-${environment}'})'
    eventGridTopicEndpoint: 'https://${resourcePrefix}-session${environment == 'prod' ? '' : '-${environment}'}.${location}-1.eventgrid.azure.net/api/events'
    eventGridTopicKey: '@Microsoft.KeyVault(VaultName=${sharedInfrastructure[i].outputs.keyVaultName};SecretName=EventGridTopicKey${environment == 'prod' ? '' : '-${environment}'})'
    iotEventHubEndpoint: '@Microsoft.KeyVault(VaultName=${sharedInfrastructure[i].outputs.keyVaultName};SecretName=IoTEventHubEndpoint)'
    iotServiceConnection: '@Microsoft.KeyVault(VaultName=${sharedInfrastructure[i].outputs.keyVaultName};SecretName=IoTServiceConnection)'
  }
}]

// Outputs for verification
output deployedEnvironments array = environments
output sharedResourceGroup string = sharedInfrastructure[0].outputs.sharedResourceGroupName
output keyVaultName string = sharedInfrastructure[0].outputs.keyVaultName
output cosmosAccountName string = sharedInfrastructure[0].outputs.cosmosAccountName
output containerRegistryLoginServer string = sharedInfrastructure[0].outputs.loginServer

output environmentResourceGroups object = {
  sessions: [for (environment, i) in environments: {
    environment: environment
    resourceGroup: sharedInfrastructure[i].outputs.sessionsResourceGroupName
  }]
  device: [for (environment, i) in environments: {
    environment: environment
    resourceGroup: sharedInfrastructure[i].outputs.deviceResourceGroupName
  }]
  iot: [for (environment, i) in environments: {
    environment: environment
    resourceGroup: sharedInfrastructure[i].outputs.iotResourceGroupName
  }]
}