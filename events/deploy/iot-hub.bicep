@secure()
param IotHubs_MeatGeek_connectionString string

@secure()
param IotHubs_MeatGeek_containerName string
param IotHubs_MeatGeek_name string = 'MeatGeek'
@description('Location for the resrouces. Defaults to the location of the Resource Group')
param location string= resourceGroup().location

resource IotHubs_MeatGeek_name_resource 'Microsoft.Devices/IotHubs@2021-07-01' = {
  name: IotHubs_MeatGeek_name
  location: location
  sku: {
    name: 'S1'
    capacity: 1
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    ipFilterRules: []
    eventHubEndpoints: {
      events: {
        retentionTimeInDays: 1
        partitionCount: 4
      }
    }
    routing: {
      endpoints: {
        serviceBusQueues: []
        serviceBusTopics: [
          {
            endpointUri: 'sb://meatgeek-sessions.servicebus.windows.net'
            entityPath: 'sessiontelemetry'
            authenticationType: 'identityBased'
            name: 'sessiontelemetry'
            id: '23312712-af59-4d79-97f6-c5bc692d2e87'
            subscriptionId: '2394ff5d-4d73-4134-a00f-2385754aeeb5'
            resourceGroup: '${IotHubs_MeatGeek_name}-Shared'
          }
        ]
        eventHubs: []
        storageContainers: []
      }
      routes: [
        {
          name: 'sessiontelemetry'
          source: 'DeviceMessages'
          condition: 'IS_STRING($body.sessionId)'
          endpointNames: [
            'sessiontelemetry'
          ]
          isEnabled: true
        }
      ]
      fallbackRoute: {
        name: '$fallback'
        source: 'DeviceMessages'
        condition: 'true'
        endpointNames: [
          'events'
        ]
        isEnabled: true
      }
    }
    storageEndpoints: {
      '$default': {
        sasTtlAsIso8601: 'PT1H'
        connectionString: IotHubs_MeatGeek_connectionString
        containerName: IotHubs_MeatGeek_containerName
      }
    }
    messagingEndpoints: {
      fileNotifications: {
        lockDurationAsIso8601: 'PT1M'
        ttlAsIso8601: 'PT1H'
        maxDeliveryCount: 10
      }
    }
    enableFileUploadNotifications: false
    cloudToDevice: {
      maxDeliveryCount: 10
      defaultTtlAsIso8601: 'PT1H'
      feedback: {
        lockDurationAsIso8601: 'PT1M'
        ttlAsIso8601: 'PT1H'
        maxDeliveryCount: 10
      }
    }
    features: 'None'
  }
}
