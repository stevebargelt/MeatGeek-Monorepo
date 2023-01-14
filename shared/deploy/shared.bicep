param location string = resourceGroup().location
param tenantId string = subscription().tenantId
// param cosmosDbAccountName string = 'meatgeek'
param kvName string = 'meatgeek'
@description('The SKU of the vault to be created.')
@allowed([
  'standard'
  'premium'
])
param skuName string = 'standard'

resource vault 'Microsoft.KeyVault/vaults@2021-11-01-preview' = {
  name: kvName
  location: location
  properties: {
    accessPolicies:[]
    enableRbacAuthorization: false
    enableSoftDelete: false
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    tenantId: tenantId
    sku: {
      name: skuName
      family: 'A'
    }
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2021-01-15' = {
//   name: cosmosDbAccountName
//   location: location
//   kind: 'GlobalDocumentDB'
//   properties: {
//     publicNetworkAccess: 'Enabled'
//     enableAutomaticFailover: false
//     enableMultipleWriteLocations: false
//     isVirtualNetworkFilterEnabled: false
//     virtualNetworkRules: []
//     disableKeyBasedMetadataWriteAccess: false
//     enableFreeTier: true
//     enableAnalyticalStorage: false
//     databaseAccountOfferType: 'Standard'
//     consistencyPolicy: {
//       defaultConsistencyLevel: 'Session'
//       maxIntervalInSeconds: 5
//       maxStalenessPrefix: 100
//     }
//     locations: [
//       {
//         locationName: location
//         failoverPriority: 0
//         isZoneRedundant: false
//       }
//     ]
//     cors: []
//     capabilities: []
//     ipRules: []
//     backupPolicy: {
//       type: 'Periodic'
//       periodicModeProperties: {
//         backupIntervalInMinutes: 240
//         backupRetentionIntervalInHours: 8
//       }
//     }
//   }
// }

// resource kvName_SHARED_CosmosConnectionString 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
//   parent: kv
//   name: 'SHARED-CosmosConnectionString'
//   properties: {
//     contentType: 'text/plain'
//     value: listConnectionStrings(cosmosDbAccount.id, '2019-12-12').connectionStrings[0].connectionString
//   }
// }
