param location string = resourceGroup().location
param tenantId string = subscription().tenantId
param objectId string
@description('Prefixes to be used by all resources deployed by this template')
param resourcePrefix string = 'meatgeek'

param kvName string = '${resourcePrefix}kv'
var vaultURL = 'https://${kvName}${environment().suffixes.keyvaultDns}'

param cosmosAccountName string = resourcePrefix
param cosmosDatabaseName string = resourcePrefix
param cosmosContainerName string = resourcePrefix
param cosmosPartition string = '/smokerId'
param topics_meatgeek_name string = '${resourcePrefix}-session'

// TODO: Parameterize the following... or pass from elsewhere? 
param meatgeekiot_workerapi_externalid string = '/subscriptions/c7e800cb-0ee6-4175-9605-a6b97c6f419f/resourceGroups/MeatGeek-IoT/providers/Microsoft.Web/sites/meatgeekiot-workerapi'
var sessionCreatedId = '${meatgeekiot_workerapi_externalid}/functions/SessionCreated'

@description('The SKU of the vault to be created.')
@allowed([
  'standard'
  'premium'
])
param skuName string = 'standard'

resource meatgeek_keyvault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: kvName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: skuName
    }
    tenantId: tenantId
    accessPolicies: [
      {
        tenantId: tenantId
        objectId: objectId
        permissions: {
          keys: [
            'Get'
            'List'
            'Update'
            'Create'
            'Import'
            'Delete'
            'Recover'
            'Backup'
            'Restore'
            'GetRotationPolicy'
            'SetRotationPolicy'
            'Rotate'
          ]
          secrets: [
            'Get'
            'List'
            'Set'
            'Delete'
            'Recover'
            'Backup'
            'Restore'
          ]
          certificates: [
            'Get'
            'List'
            'Update'
            'Create'
            'Import'
            'Delete'
            'Recover'
            'Backup'
            'Restore'
            'ManageContacts'
            'ManageIssuers'
            'GetIssuers'
            'ListIssuers'
            'SetIssuers'
            'DeleteIssuers'
          ]
        }
      }
    ]
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enableRbacAuthorization: true
    vaultUri: vaultURL
    provisioningState: 'Succeeded'
    publicNetworkAccess: 'Enabled'
  }
}

resource secret 'Microsoft.KeyVault/vaults/secrets@2021-11-01-preview' = {
  parent: meatgeek_keyvault
  name: 'myPassword'
  properties: {
    value: 'correct-horse-battery-staple'
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

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2022-05-15' = {
  parent: databaseAccount
  name: cosmosDatabaseName
  properties: {
    resource: {
      id: cosmosDatabaseName
    }
  }
}

resource databaseAccounts_meatgeek_name_databaseAccounts_meatgeek_name_IoT 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-08-15' = {
  parent: database
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

}

resource setCosmosConnectionString 'Microsoft.KeyVault/vaults/secrets@2021-11-01-preview' = {
  parent: meatgeek_keyvault
  name: 'SharedCosmosConnectionString'
  properties: {
    value: databaseAccount.listConnectionStrings().connectionStrings[0].connectionString
  }
}

resource meatgeek_eventgrid_session_topic 'Microsoft.EventGrid/topics@2022-06-15' = {
  name: topics_meatgeek_name
  location: location
  identity: {
    type: 'None'
  }
  properties: {
    inputSchema: 'EventGridSchema'
    publicNetworkAccess: 'Enabled'
    dataResidencyBoundary: 'WithinGeopair'
  }
}

resource topics_meatgeek_session_name_meatgeekeventviewer 'Microsoft.EventGrid/topics/eventSubscriptions@2022-06-15' = {
  parent: meatgeek_eventgrid_session_topic
  name: 'meatgeekeventviewer'
  properties: {
    destination: {
      properties: {
        maxEventsPerBatch: 1
        preferredBatchSizeInKilobytes: 64
      }
      endpointType: 'WebHook'
    }
    filter: {
      enableAdvancedFilteringOnArrays: true
    }
    labels: []
    eventDeliverySchema: 'EventGridSchema'
    retryPolicy: {
      maxDeliveryAttempts: 30
      eventTimeToLiveInMinutes: 1440
    }
  }
}

resource topics_meatgeek_session_name_SessionCreated 'Microsoft.EventGrid/topics/eventSubscriptions@2022-06-15' = {
  parent: meatgeek_eventgrid_session_topic
  name: 'SessionCreated'
  properties: {
    destination: {
      properties: {
        resourceId: sessionCreatedId
        maxEventsPerBatch: 1
        preferredBatchSizeInKilobytes: 64
      }
      endpointType: 'AzureFunction'
    }
    filter: {
      includedEventTypes: [
        'SessionCreated'
        'session'
      ]
      enableAdvancedFilteringOnArrays: true
    }
    labels: []
    eventDeliverySchema: 'EventGridSchema'
    retryPolicy: {
      maxDeliveryAttempts: 30
      eventTimeToLiveInMinutes: 1440
    }
  }
}

resource topics_meatgeek_session_name_SessionEnded 'Microsoft.EventGrid/topics/eventSubscriptions@2022-06-15' = {
  parent: meatgeek_eventgrid_session_topic
  name: 'SessionEnded'
  properties: {
    destination: {
      properties: {
        resourceId: '${meatgeekiot_workerapi_externalid}/functions/SessionEnded'
        maxEventsPerBatch: 1
        preferredBatchSizeInKilobytes: 64
      }
      endpointType: 'AzureFunction'
    }
    filter: {
      includedEventTypes: [
        'SessionEnded'
      ]
      enableAdvancedFilteringOnArrays: true
    }
    labels: []
    eventDeliverySchema: 'EventGridSchema'
    retryPolicy: {
      maxDeliveryAttempts: 30
      eventTimeToLiveInMinutes: 1440
    }
  }
}

resource topics_meatgeek_session_name_SessionUpdated 'Microsoft.EventGrid/topics/eventSubscriptions@2022-06-15' = {
  parent: meatgeek_eventgrid_session_topic
  name: 'SessionUpdated'
  properties: {
    destination: {
      properties: {
        resourceId: '${meatgeekiot_workerapi_externalid}/functions/SessionUpdated'
        maxEventsPerBatch: 1
        preferredBatchSizeInKilobytes: 64
      }
      endpointType: 'AzureFunction'
    }
    filter: {
      includedEventTypes: [
        'SessionCreated'
      ]
      enableAdvancedFilteringOnArrays: true
    }
    labels: []
    eventDeliverySchema: 'EventGridSchema'
    retryPolicy: {
      maxDeliveryAttempts: 30
      eventTimeToLiveInMinutes: 1440
    }
  }
}

@minLength(5)
@maxLength(50)
@description('Provide a globally unique name of your Azure Container Registry')
param acrName string = 'acr${resourcePrefix}${uniqueString(resourceGroup().id)}'
@description('Provide a tier of your Azure Container Registry.')
param acrSku string = 'Basic'

resource acrResource 'Microsoft.ContainerRegistry/registries@2021-06-01-preview' = {
  name: acrName
  location: location
  sku: {
    name: acrSku
  }
  properties: {
    adminUserEnabled: true
  }
},


@description('Output the login server property for later use')
output loginServer string = acrResource.properties.loginServer
