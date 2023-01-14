param location string = resourceGroup().location
param tenantId string = subscription().tenantId
param objectId string
// objectId is the objectId of the user who has admin permissions for the vault
// TODO: change the param name??
param kvName string = 'meatgeek'
var vaultURL = 'https://${kvName}${environment().suffixes.keyvaultDns}'

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
//////////////////////////////////////////////////////////////////////////////////
output stringOutput string = 'ONE'

resource databaseAccounts_meatgeek_name_resource 'Microsoft.DocumentDB/databaseAccounts@2022-08-15' = {
  name: cosmosDatabaseName
  location: location
  tags: {
    defaultExperience: 'Core (SQL)'
    'hidden-cosmos-mmspecial': ''
  }
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

//////////////////////////////////////////////////////////////////////////////////
output stringOutput2 string = 'TWO'
resource databaseAccounts_meatgeek_name_databaseAccounts_meatgeek_name 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2022-08-15' = {
  parent: databaseAccounts_meatgeek_name_resource
  name: cosmosDatabaseName
  properties: {
    resource: {
      id: cosmosDatabaseName
    }
  }
}

//////////////////////////////////////////////////////////////////////////////////
output stringOutput3 string = 'THREE'
resource databaseAccounts_meatgeek_name_00000000_0000_0000_0000_000000000001 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2022-08-15' = {
  parent: databaseAccounts_meatgeek_name_resource
  name: '00000000-0000-0000-0000-000000000001'
  properties: {
    roleName: 'Cosmos DB Built-in Data Reader'
    type: 'BuiltInRole'
    assignableScopes: [
      databaseAccounts_meatgeek_name_resource.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/executeQuery'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/readChangeFeed'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/read'
        ]
        notDataActions: []
      }
    ]
  }
}

//////////////////////////////////////////////////////////////////////////////////
output stringOutput4 string = 'FOUR'
resource databaseAccounts_meatgeek_name_00000000_0000_0000_0000_000000000002 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2022-08-15' = {
  parent: databaseAccounts_meatgeek_name_resource
  name: '00000000-0000-0000-0000-000000000002'
  properties: {
    roleName: 'Cosmos DB Built-in Data Contributor'
    type: 'BuiltInRole'
    assignableScopes: [
      databaseAccounts_meatgeek_name_resource.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/*'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/*'
        ]
        notDataActions: []
      }
    ]
  }
}

//////////////////////////////////////////////////////////////////////////////////
output stringOutput5 string = 'FIVE'
resource databaseAccounts_meatgeek_name_databaseAccounts_meatgeek_name_IoT 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-08-15' = {
  parent: databaseAccounts_meatgeek_name_databaseAccounts_meatgeek_name
  name: iotCollectionName
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

//////////////////////////////////////////////////////////////////////////////////
output stringOutput6 string = 'SiX'
resource databaseAccounts_meatgeek_name_databaseAccounts_meatgeek_name_sessions 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-08-15' = {
  parent: databaseAccounts_meatgeek_name_databaseAccounts_meatgeek_name
  name: sessionsCollectionName
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

//////////////////////////////////////////////////////////////////////////////////
output stringOutput7 string = 'SE7EN'
resource databaseAccounts_meatgeek_name_databaseAccounts_meatgeek_name_default 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/throughputSettings@2022-08-15' = {
  parent: databaseAccounts_meatgeek_name_databaseAccounts_meatgeek_name
  name: 'default'
  properties: {
    resource: {
      throughput: 100
      autoscaleSettings: {
        maxThroughput: 1000
      }
    }
  }
}
