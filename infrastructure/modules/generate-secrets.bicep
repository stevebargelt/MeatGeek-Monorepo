@description('Key Vault name')
param keyVaultName string

@description('Environment name')
param environment string

@description('Generate new GUID for secret generation')
param guidSeed string = newGuid()

// Environment suffix
var envSuffix = environment == 'prod' ? '' : '-${environment}'

// Reference existing Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: keyVaultName
}

// Generate deterministic but unique values
var secretBase = uniqueString(subscription().subscriptionId, resourceGroup().id, guidSeed)

// Event Grid Topic Key (generated)
resource eventGridTopicKeySecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'EventGridTopicKey${envSuffix}'
  properties: {
    value: base64(concat('EventGrid-', secretBase, '-', environment))
  }
}

// Relay Key (generated)
resource relayKeySecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: 'RelayKey${envSuffix}'
  properties: {
    value: base64(concat('Relay-', secretBase, '-', environment))
  }
}

// IoT Hub Keys (shared, only for prod)
resource iotServiceKeySecret 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = if (environment == 'prod') {
  parent: keyVault
  name: 'IoTServiceKey'
  properties: {
    value: base64(concat('IoTService-', secretBase))
  }
}

// Placeholder secrets that need actual values
resource placeholderSecrets 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = [for secret in [
  'IoTServiceConnection'
  'IoTEventHubEndpoint'
  'IoTSharedAccessConnString'
]: {
  parent: keyVault
  name: '${secret}${envSuffix}-PLACEHOLDER'
  properties: {
    value: 'REPLACE_ME_WITH_ACTUAL_VALUE'
    contentType: 'Placeholder - Requires manual configuration'
    tags: {
      environment: environment
      status: 'placeholder'
    }
  }
}]

output secretsGenerated array = [
  'EventGridTopicKey${envSuffix}'
  'RelayKey${envSuffix}'
]