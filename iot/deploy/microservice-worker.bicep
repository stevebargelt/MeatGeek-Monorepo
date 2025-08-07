@description('Subscription ID')
param subscriptionId string = subscription().subscriptionId
@minLength(3)
@maxLength(8)
@description('Prefixes to be used by all resources deployed by this template')
param resourcePrefix string = 'meatgeek'
@description('Project Name to be used by all resources deployed by this template (sessions, shared, device, iot)')
param resourceProject string = 'iot-worker'
@description('Location for the resrouces. Defaults to the location of the Resource Group')
param location string= resourceGroup().location
@description('Environment name (prod, staging, test)')
@allowed([
  'prod'
  'staging'
  'test'
])
param environment string = 'prod'
@description('Name of the Cosmos DB to use')
param cosmosAccountName string = 'meatgeek'
@description('Name of the Cosmos DB database to use')
param cosmosDbDatabaseName string = environment == 'prod' ? 'meatgeek' : 'meatgeek-${environment}'
@description('Name of the Cosmos DB collection to use')
param cosmosDbCollectionName string = 'meatgeek'
@description('ID of a existing keyvault that will be used to store and retrieve keys in this deployment')
param keyVaultName string = 'meatgeekkv'
@description('Shared Key Vault Resource Group')
param keyVaultResourceGroup string = 'MeatGeek-Shared'
// param deploymentDate string = utcNow()
param eventGridTopicEndpoint string 
param eventGridTopicKey string
param iotEventHubEndpoint string
param iotServiceConnection string 
param cosmosConnectionString string

// Environment-specific resource naming
var envSuffix = environment == 'prod' ? '' : '-${environment}'
var functionsAppServicePlanName = '${resourcePrefix}${envSuffix}-${resourceProject}-app-service-plan'
var functionsApiAppName = '${resourcePrefix}${replace(resourceProject, '-', '')}${envSuffix}api'
var appInsightsName = '${resourcePrefix}${envSuffix}-${resourceProject}-appinsights'
var logAnalyticsName = '${resourcePrefix}${envSuffix}-${resourceProject}-loganalytics'

var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storage.name};EndpointSuffix=${az.environment().suffixes.storage};AccountKey=${storage.listKeys().keys[0].value}'
var resourceSuffix = substring(uniqueString(resourceGroup().id),0,5)
var storageAccountName =  toLower(format('st{0}', replace('${resourceProject}${envSuffix}${resourceSuffix}', '-', '')))

var storageBlobDataContributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
var keyVaultSecretsUserRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')

// var appTags = {
//   AppID: '${appName}-${appInternalServiceName}'
//   AppName: '${appName}-${appInternalServiceName}'
// }

resource storage 'Microsoft.Storage/storageAccounts@2019-06-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'    
    encryption: {
      keySource: 'Microsoft.Storage'
      services: {
        file: {
          keyType: 'Account'
          enabled: true
        }
        blob: {
          keyType: 'Account'
          enabled: true
        }
        queue: {
          enabled: true
        }
        table: {
          enabled: true
        }
      }
    }
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2019-06-01' = {
  parent: storage
  name: 'default'
  properties: {
    cors: {
      corsRules: []
    }
    deleteRetentionPolicy: {
      enabled: false
    }
  }
  resource content 'containers' = {
    name: 'content'
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: {
      dailyQuotaGb: 1
    }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: {
    'hidden-link:${resourceId('Microsoft.Web/sites', appInsightsName)}': 'Resource'
  }
  kind: 'web'
  properties: {
    Application_Type: 'web'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2019-09-01' existing = {     
  name: keyVaultName
  scope: resourceGroup(subscriptionId, keyVaultResourceGroup)     
} 

module setStorageAccountSecret 'setSecret.bicep' = {
  scope: resourceGroup(subscriptionId, keyVaultResourceGroup)
  name: '${resourcePrefix}-${resourceProject}${envSuffix}-storeageaccountsecret'
  params: {
    keyVaultName: keyVault.name
    secretName: '${storage.name}-${resourcePrefix}-${resourceProject}${envSuffix}-ConnectionString'
    secretValue: storageConnectionString
  }
}

resource functionsAppServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: functionsAppServicePlanName
  location: location
  sku: {
    name: 'Y1'
  }
  kind: 'functionapp'
  properties: {

  }
}

resource functionsApiApp 'Microsoft.Web/sites@2024-04-01' = {
  name: functionsApiAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
     autoGeneratedDomainNameLabelScope: 'TenantReuse'
    httpsOnly: true
    enabled: true
    serverFarmId: functionsAppServicePlan.id
    reserved: false
    siteConfig: {
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      netFrameworkVersion: 'v6.0'
      appSettings: [

      ]
    }
  }

}

resource functionsApiAppName_appsettings 'Microsoft.Web/sites/config@2016-08-01' = {
  parent: functionsApiApp
  name: 'appsettings'
  properties: {
    CosmosDBConnection: cosmosConnectionString
    DatabaseName: cosmosDbDatabaseName
    CollectionName: cosmosDbCollectionName
    ContentStorageAccount: storage.name
    ContentContainer: blobService::content.name
    FUNCTIONS_EXTENSION_VERSION: '~4'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet'
    AzureWebJobsStorage: storageConnectionString
    WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: storageConnectionString
    WEBSITE_CONTENTSHARE: '${functionsApiAppName}102269'
    APPINSIGHTS_INSTRUMENTATIONKEY: appInsights.properties.InstrumentationKey
    EventGridTopicEndpoint: eventGridTopicEndpoint
    EventGridTopicKey: eventGridTopicKey
    IOT_EVENTHUB_ENDPOINT: iotEventHubEndpoint
    IOT_SERVICE_CONNECTION: iotServiceConnection

  }
}

resource storageFunctionAppPermissions 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(storage.id, functionsApiApp.name, storageBlobDataContributorRole)
  scope: storage
  properties: {
    principalId: functionsApiApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: storageBlobDataContributorRole
  }
}

module kvFunctionAppPermissions 'setVaultPermissions.bicep' = {
  name: 'KeyVaultPermissions'
  params: {
    keyVaultId: keyVault.id
    functionsApiAppName: functionsApiAppName
    principalId: functionsApiApp.identity.principalId
    keyVaultUserRole: keyVaultSecretsUserRole  
  }
}


output apiAppName string = functionsApiAppName
