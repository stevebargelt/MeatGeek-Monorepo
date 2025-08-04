targetScope = 'subscription'

@description('Location for the resources')
param location string = 'northcentralus'

@description('Object ID of the user/service principal for Key Vault access')
param objectId string

@description('Which environments to deploy')
@allowed([
  'prod-only'
  'prod-staging'
  'prod-staging-test'
  'all'
])
param environmentsTodeploy string = 'prod-only'

// Determine which environments to deploy
var environments = environmentsTodeploy == 'prod-only' ? ['prod'] : environmentsTodeploy == 'prod-staging' ? ['prod', 'staging'] : environmentsTodeploy == 'prod-staging-test' ? ['prod', 'staging', 'test'] : ['prod', 'staging', 'test']

// Deploy shared infrastructure first (only once, not per environment)
module sharedInfra '../shared/deploy/shared.bicep' = {
  name: 'shared-infrastructure'
  params: {
    location: location
    objectId: objectId
    environment: 'prod' // Shared resources are always 'prod'
  }
}

// Deploy environment-specific resources
module environmentDeployments '../shared/deploy/shared.bicep' = [for env in environments: if (env != 'prod') {
  name: 'environment-${env}'
  params: {
    location: location
    objectId: objectId
    environment: env
  }
  dependsOn: [
    sharedInfra
  ]
}]

// Deploy Sessions API for each environment
module sessionsApi '../sessions/deploy/microservice.bicep' = [for env in environments: {
  name: 'sessions-api-${env}'
  scope: resourceGroup(env == 'prod' ? 'MeatGeek-Sessions' : 'MeatGeek-Sessions-${env}')
  params: {
    location: location
    keyVaultName: sharedInfra.outputs.keyVaultName
    cosmosAccountName: sharedInfra.outputs.cosmosAccountName
    cosmosDbCollectionName: env == 'prod' ? 'meatgeek' : 'meatgeek-${env}'
    eventGridTopicEndpoint: sharedInfra.outputs.eventGridTopicEndpoint
    eventGridTopicKey: sharedInfra.outputs.eventGridTopicKey
    iotEventHubEndpoint: sharedInfra.outputs.iotEventHubEndpoint
    iotServiceConnection: sharedInfra.outputs.iotServiceConnection
    cosmosConnectionString: sharedInfra.outputs.cosmosConnectionString
    environment: env
  }
  dependsOn: [
    sharedInfra
  ]
}]

// Deploy Sessions Worker API for each environment
module sessionsWorkerApi '../sessions/deploy/microservice-worker.bicep' = [for env in environments: {
  name: 'sessions-worker-api-${env}'
  scope: resourceGroup(env == 'prod' ? 'MeatGeek-Sessions' : 'MeatGeek-Sessions-${env}')
  params: {
    location: location
    keyVaultName: sharedInfra.outputs.keyVaultName
    cosmosAccountName: sharedInfra.outputs.cosmosAccountName
    cosmosDbCollectionName: env == 'prod' ? 'meatgeek' : 'meatgeek-${env}'
    eventGridTopicEndpoint: sharedInfra.outputs.eventGridTopicEndpoint
    eventGridTopicKey: sharedInfra.outputs.eventGridTopicKey
    iotEventHubEndpoint: sharedInfra.outputs.iotEventHubEndpoint
    iotServiceConnection: sharedInfra.outputs.iotServiceConnection
    cosmosConnectionString: sharedInfra.outputs.cosmosConnectionString
    environment: env
  }
  dependsOn: [
    sharedInfra
  ]
}]

// Deploy Device API for each environment
module deviceApi '../device/deploy/microservice.bicep' = [for env in environments: {
  name: 'device-api-${env}'
  scope: resourceGroup(env == 'prod' ? 'MeatGeek-Device' : 'MeatGeek-Device-${env}')
  params: {
    location: location
    keyVaultName: sharedInfra.outputs.keyVaultName
    relayNamespaceName: env == 'prod' ? 'meatgeek-relay' : 'meatgeek-relay-${env}'
    hybridConnectionName: env == 'prod' ? 'meatgeek-hc' : 'meatgeek-hc-${env}'
    environment: env
  }
  dependsOn: [
    sharedInfra
  ]
}]

// Deploy IoT Functions for each environment
module iotFunctions '../iot/deploy/api.bicep' = [for env in environments: {
  name: 'iot-functions-${env}'
  scope: resourceGroup(env == 'prod' ? 'MeatGeek-IoT' : 'MeatGeek-IoT-${env}')
  params: {
    location: location
    keyVaultName: sharedInfra.outputs.keyVaultName
    cosmosAccountName: sharedInfra.outputs.cosmosAccountName
    cosmosDbCollectionName: env == 'prod' ? 'meatgeek' : 'meatgeek-${env}'
    iotHubName: sharedInfra.outputs.iotHubName
    environment: env
  }
  dependsOn: [
    sharedInfra
  ]
}]

// Deploy IoT Worker API for each environment
module iotWorkerApi '../iot/deploy/microservice-worker.bicep' = [for env in environments: {
  name: 'iot-worker-api-${env}'
  scope: resourceGroup(env == 'prod' ? 'MeatGeek-IoT' : 'MeatGeek-IoT-${env}')
  params: {
    location: location
    keyVaultName: sharedInfra.outputs.keyVaultName
    cosmosAccountName: sharedInfra.outputs.cosmosAccountName
    cosmosDbCollectionName: env == 'prod' ? 'meatgeek' : 'meatgeek-${env}'
    eventGridTopicEndpoint: sharedInfra.outputs.eventGridTopicEndpoint
    eventGridTopicKey: sharedInfra.outputs.eventGridTopicKey
    iotEventHubEndpoint: sharedInfra.outputs.iotEventHubEndpoint
    iotServiceConnection: sharedInfra.outputs.iotServiceConnection
    cosmosConnectionString: sharedInfra.outputs.cosmosConnectionString
    environment: env
  }
  dependsOn: [
    sharedInfra
  ]
}]

// Outputs
output deployedEnvironments array = environments
output keyVaultName string = sharedInfra.outputs.keyVaultName
output cosmosAccountName string = sharedInfra.outputs.cosmosAccountName
output containerRegistryName string = sharedInfra.outputs.containerRegistryName
output iotHubName string = sharedInfra.outputs.iotHubName