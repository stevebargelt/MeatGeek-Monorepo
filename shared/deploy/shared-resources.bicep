param location string
param tenantId string
param objectId string
param kvName string
param skuName string
param cosmosAccountName string
param cosmosContainerName string
param cosmosPartition string
param acrName string
param acrSku string
param environments array = ['prod']

// Shared resources naming (no environment suffix)
var vaultURL = 'https://${kvName}${az.environment().suffixes.keyvaultDns}'

resource meatgeek_keyvault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: kvName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: skuName
    }
    accessPolicies: []
    tenantId: tenantId
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enableRbacAuthorization: true
    vaultUri: vaultURL
  }
}

resource databaseAccount 'Microsoft.DocumentDB/databaseAccounts@2022-08-15' = {
  name: toLower(cosmosAccountName)
  location: location
  kind: 'GlobalDocumentDB'
  identity: {
    type: 'None'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
    enableAutomaticFailover: false
    enableMultipleWriteLocations: false
    isVirtualNetworkFilterEnabled: false
    virtualNetworkRules: []
    disableKeyBasedMetadataWriteAccess: false
    enableFreeTier: true
    enableAnalyticalStorage: false
    analyticalStorageConfiguration: {
      schemaType: 'WellDefined'
    }
    createMode: 'Default'
    databaseAccountOfferType: 'Standard'
    defaultIdentity: 'FirstPartyIdentity'
    networkAclBypass: 'None'
    disableLocalAuth: false
    enablePartitionMerge: false
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
      maxIntervalInSeconds: 5
      maxStalenessPrefix: 100
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    cors: []
    capabilities: []
    ipRules: []
    backupPolicy: {
      type: 'Continuous'
    }
    networkAclBypassResourceIds: []
    capacity: {
      totalThroughputLimit: 1000
    }
  }
}

// Create a database for each environment
resource databases 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2022-05-15' = [for env in environments: {
  parent: databaseAccount
  name: env == 'prod' ? cosmosAccountName : '${cosmosAccountName}-${env}'
  properties: {
    resource: {
      id: env == 'prod' ? cosmosAccountName : '${cosmosAccountName}-${env}'
    }
  }
}]

// Create containers in each database
resource containers 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-08-15' = [for (env, i) in environments: {
  parent: databases[i]
  name: cosmosContainerName
  properties: {
    resource: {
      id: cosmosContainerName
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
      }
      partitionKey: {
        paths: [
          cosmosPartition
        ]
        kind: 'Hash'
      }
      uniqueKeyPolicy: {
        uniqueKeys: []
      }
      conflictResolutionPolicy: {
        mode: 'LastWriterWins'
        conflictResolutionPath: '/_ts'
      }
    }
  }
}]

// Store connection string once - apps will use environment-specific databases
resource setCosmosConnectionString 'Microsoft.KeyVault/vaults/secrets@2021-11-01-preview' = {
  parent: meatgeek_keyvault
  name: 'SharedCosmosConnectionString'
  properties: {
    value: databaseAccount.listConnectionStrings().connectionStrings[0].connectionString
  }
}

// Create Event Grid topics for each environment
resource eventGridTopics 'Microsoft.EventGrid/topics@2022-06-15' = [for env in environments: {
  name: env == 'prod' ? '${cosmosAccountName}-session' : '${cosmosAccountName}-session-${env}'
  location: location
  identity: {
    type: 'None'
  }
  properties: {
    inputSchema: 'EventGridSchema'
    publicNetworkAccess: 'Enabled'
    dataResidencyBoundary: 'WithinGeopair'
  }
}]

// Store Event Grid topic keys in Key Vault for each environment
resource setEventGridTopicKeys 'Microsoft.KeyVault/vaults/secrets@2021-11-01-preview' = [for (env, i) in environments: {
  parent: meatgeek_keyvault
  name: env == 'prod' ? 'EventGridTopicKey' : 'EventGridTopicKey-${env}'
  properties: {
    value: eventGridTopics[i].listKeys().key1
  }
}]

// Note: Event subscriptions will be created later when the IoT Worker API is deployed
// as they need the function app resource IDs which are not available during shared resource deployment

resource acrResource 'Microsoft.ContainerRegistry/registries@2021-06-01-preview' = {
  name: acrName
  location: location
  sku: {
    name: acrSku
  }
  properties: {
    adminUserEnabled: true
  }
}

output loginServer string = acrResource.properties.loginServer
output keyVaultName string = meatgeek_keyvault.name
output cosmosAccountName string = databaseAccount.name
output eventGridTopicNames array = [for (env, i) in environments: eventGridTopics[i].name]
output eventGridTopicEndpoints array = [for (env, i) in environments: eventGridTopics[i].properties.endpoint]