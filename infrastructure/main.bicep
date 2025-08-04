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
var environments = environmentsTodeploy == 'prod-only' ? ['prod'] : environmentsTodeploy == 'prod-staging' ? ['prod', 'staging'] : environmentsTodeploy == 'prod-staging-test' ? ['prod', 'staging', 'test'] : ['prod', 'staging', 'test']

// 1. Deploy shared infrastructure (creates resource groups and shared resources)
// This deploys once and creates all necessary databases for all environments
module sharedInfrastructure '../shared/deploy/shared.bicep' = {
  name: 'shared-infrastructure'
  params: {
    location: location
    objectId: objectId
    resourcePrefix: resourcePrefix
    cosmosAccountName: cosmosAccountName
    cosmosContainerName: cosmosContainerName
    cosmosPartition: cosmosPartition
    environments: environments  // Pass all environments to create databases
  }
}

// 2. Deploy Sessions microservices for each environment
module sessionsInfrastructure '../sessions/deploy/microservice.bicep' = [for (environment, i) in environments: {
  name: 'sessions-infra-${environment}'
  scope: resourceGroup(environment == 'prod' ? 'MeatGeek-Sessions' : 'MeatGeek-Sessions-${environment}')
  dependsOn: [
    sharedInfrastructure
  ]
  params: {
    location: location
    environment: environment
    resourcePrefix: resourcePrefix
    resourceProject: 'sessions'
    keyVaultName: 'meatgeekkv'
    keyVaultResourceGroup: 'MeatGeek-Shared'
    cosmosAccountName: cosmosAccountName
    cosmosDbDatabaseName: environment == 'prod' ? resourcePrefix : '${resourcePrefix}-${environment}'
    cosmosDbCollectionName: cosmosContainerName
    cosmosConnectionString: '@Microsoft.KeyVault(VaultName=meatgeekkv;SecretName=SharedCosmosConnectionString)'
    eventGridTopicEndpoint: 'https://${resourcePrefix}-session${environment == 'prod' ? '' : '-${environment}'}.${location}-1.eventgrid.azure.net/api/events'
    eventGridTopicKey: '@Microsoft.KeyVault(VaultName=meatgeekkv;SecretName=EventGridTopicKey${environment == 'prod' ? '' : '-${environment}'})'
    iotEventHubEndpoint: '@Microsoft.KeyVault(VaultName=meatgeekkv;SecretName=IoTEventHubEndpoint)'
    iotServiceConnection: '@Microsoft.KeyVault(VaultName=meatgeekkv;SecretName=IoTServiceConnection)'
  }
}]

// 3. Deploy Sessions Worker API for each environment
module sessionsWorkerInfrastructure '../sessions/deploy/microservice-worker.bicep' = [for (environment, i) in environments: {
  name: 'sessions-worker-infra-${environment}'
  scope: resourceGroup(environment == 'prod' ? 'MeatGeek-Sessions' : 'MeatGeek-Sessions-${environment}')
  dependsOn: [
    sharedInfrastructure
  ]
  params: {
    location: location
    environment: environment
    resourcePrefix: resourcePrefix
    resourceProject: 'sessions-worker'
    keyVaultName: 'meatgeekkv'
    keyVaultResourceGroup: 'MeatGeek-Shared'
    cosmosAccountName: cosmosAccountName
    cosmosDbDatabaseName: environment == 'prod' ? resourcePrefix : '${resourcePrefix}-${environment}'
    cosmosDbCollectionName: cosmosContainerName
    cosmosConnectionString: '@Microsoft.KeyVault(VaultName=meatgeekkv;SecretName=SharedCosmosConnectionString)'
    eventGridTopicEndpoint: 'https://${resourcePrefix}-session${environment == 'prod' ? '' : '-${environment}'}.${location}-1.eventgrid.azure.net/api/events'
    eventGridTopicKey: '@Microsoft.KeyVault(VaultName=meatgeekkv;SecretName=EventGridTopicKey${environment == 'prod' ? '' : '-${environment}'})'
    iotEventHubEndpoint: '@Microsoft.KeyVault(VaultName=meatgeekkv;SecretName=IoTEventHubEndpoint)'
    iotServiceConnection: '@Microsoft.KeyVault(VaultName=meatgeekkv;SecretName=IoTServiceConnection)'
  }
}]

// 4. Deploy Device microservices for each environment
module deviceInfrastructure '../device/deploy/microservice.bicep' = [for (environment, i) in environments: {
  name: 'device-infra-${environment}'
  scope: resourceGroup(environment == 'prod' ? 'MeatGeek-Device' : 'MeatGeek-Device-${environment}')
  dependsOn: [
    sharedInfrastructure
  ]
  params: {
    location: location
    environment: environment
    resourcePrefix: resourcePrefix
    resourceProject: 'device'
    keyVaultName: 'meatgeekkv'
    keyVaultResourceGroup: 'MeatGeek-Shared'
  }
}]

// 5. Deploy IoT microservices for each environment
module iotInfrastructure '../iot/deploy/api.bicep' = [for (environment, i) in environments: {
  name: 'iot-infra-${environment}'
  scope: resourceGroup(environment == 'prod' ? 'MeatGeek-IoT' : 'MeatGeek-IoT-${environment}')
  dependsOn: [
    sharedInfrastructure
  ]
  params: {
    location: location
    environment: environment
    resourcePrefix: resourcePrefix
    resourceProject: 'iot-api'
    keyVaultName: 'meatgeekkv'
    keyVaultResourceGroup: 'MeatGeek-Shared'
    cosmosAccountName: cosmosAccountName
    cosmosDbDatabaseName: environment == 'prod' ? resourcePrefix : '${resourcePrefix}-${environment}'
    cosmosDbCollectionName: cosmosContainerName
    cosmosConnectionString: '@Microsoft.KeyVault(VaultName=meatgeekkv;SecretName=SharedCosmosConnectionString)'
    eventGridTopicEndpoint: 'https://${resourcePrefix}-session${environment == 'prod' ? '' : '-${environment}'}.${location}-1.eventgrid.azure.net/api/events'
    eventGridTopicKey: '@Microsoft.KeyVault(VaultName=meatgeekkv;SecretName=EventGridTopicKey${environment == 'prod' ? '' : '-${environment}'})'
    iotEventHubEndpoint: '@Microsoft.KeyVault(VaultName=meatgeekkv;SecretName=IoTEventHubEndpoint)'
    iotServiceConnection: '@Microsoft.KeyVault(VaultName=meatgeekkv;SecretName=IoTServiceConnection)'
    iotSharedAccessConnString: '@Microsoft.KeyVault(VaultName=meatgeekkv;SecretName=IoTSharedAccessConnString)'
  }
}]

// 6. Deploy IoT Worker API for each environment
module iotWorkerInfrastructure '../iot/deploy/microservice-worker.bicep' = [for (environment, i) in environments: {
  name: 'iot-worker-infra-${environment}'
  scope: resourceGroup(environment == 'prod' ? 'MeatGeek-IoT' : 'MeatGeek-IoT-${environment}')
  dependsOn: [
    sharedInfrastructure
  ]
  params: {
    location: location
    environment: environment
    resourcePrefix: resourcePrefix
    resourceProject: 'iot-worker'
    keyVaultName: 'meatgeekkv'
    keyVaultResourceGroup: 'MeatGeek-Shared'
    cosmosAccountName: cosmosAccountName
    cosmosDbDatabaseName: environment == 'prod' ? resourcePrefix : '${resourcePrefix}-${environment}'
    cosmosDbCollectionName: cosmosContainerName
    cosmosConnectionString: '@Microsoft.KeyVault(VaultName=meatgeekkv;SecretName=SharedCosmosConnectionString)'
    eventGridTopicEndpoint: 'https://${resourcePrefix}-session${environment == 'prod' ? '' : '-${environment}'}.${location}-1.eventgrid.azure.net/api/events'
    eventGridTopicKey: '@Microsoft.KeyVault(VaultName=meatgeekkv;SecretName=EventGridTopicKey${environment == 'prod' ? '' : '-${environment}'})'
    iotEventHubEndpoint: '@Microsoft.KeyVault(VaultName=meatgeekkv;SecretName=IoTEventHubEndpoint)'
    iotServiceConnection: '@Microsoft.KeyVault(VaultName=meatgeekkv;SecretName=IoTServiceConnection)'
  }
}]

// Outputs for verification
output deployedEnvironments array = environments
output sharedResourceGroup string = 'MeatGeek-Shared'
output keyVaultName string = 'meatgeekkv'
output cosmosAccountNameUsed string = cosmosAccountName

// Output the environments that were deployed
output deployedEnvironmentsList array = environments