param location string = resourceGroup().location
param tenantId string = subscription().tenantId
param objectId string
// objectId is the objectId of the user who has admin permissions for the vault
// TODO: change the param name??
param kvName string = 'meatgeek'
var vaultURL = 'https://${kvName}${environment().suffixes.keyvaultDns}'

param cosmosAccountName string = 'meatgeek'
param cosmosDatabaseName string = 'meatgeek'
param sessionsCollectionName string = 'sessions'
param iotCollectionName string = 'IoT'
param sessionsPartition string = '/smokerId'
param iotPartition string = '/smokerId'


@description('The SKU of the vault to be created.')
@allowed([
  'standard'
  'premium'
])
param skuName string = 'standard'

resource vaults_meatgeekkv_name_resource 'Microsoft.KeyVault/vaults@2022-07-01' = {
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
    enableRbacAuthorization: false
    vaultUri: vaultURL
    provisioningState: 'Succeeded'
    publicNetworkAccess: 'Enabled'
  }
}

resource databaseAccounts_meatgeek_name_resource 'Microsoft.DocumentDB/databaseAccounts@2022-08-15' = {
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
  name: '${cosmosAccountName}/${cosmosDatabaseName}'
  properties: {
    resource: {
      id: cosmosDatabaseName
    }
  }
}


resource databaseAccounts_meatgeek_name_databaseAccounts_meatgeek_name_IoT 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-08-15' = {
  name: '${iotCollectionName}/${cosmosDatabaseName}'
  properties: {
    resource: {
      id: iotCollectionName
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
          iotPartition
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

resource databaseAccounts_meatgeek_name_databaseAccounts_meatgeek_name_sessions 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-08-15' = {
  name: '${sessionsCollectionName}/${cosmosDatabaseName}'
  properties: {
    resource: {
      id: sessionsCollectionName
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
          sessionsPartition
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
