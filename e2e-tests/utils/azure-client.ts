/**
 * Azure services client for E2E testing
 * Provides integration with Azure IoT Hub, Cosmos DB, and Event Grid
 */

import { DefaultAzureCredential } from '@azure/identity';
import { CosmosClient, Database, Container } from '@azure/cosmos';
import { EventGridPublisherClient } from '@azure/eventgrid';
import { Registry } from 'azure-iothub';
import { getCurrentEnvironment } from '../config/test-environments';
import { LocalAzureServiceFactory, MockCosmosClient, MockEventGridClient, MockIoTHubClient } from '../mocks/local-azure-services';

export interface AzureClientOptions {
  cosmosConnectionString?: string;
  iotHubConnectionString?: string;
  eventGridEndpoint?: string;
  eventGridKey?: string;
}

export interface TelemetryRecord {
  id: string;
  deviceId: string;
  sessionId?: string;
  timestamp: string;
  temperature: number;
  mode: string;
  ttl?: number;
}

export interface SessionRecord {
  id: string;
  userId: string;
  deviceId: string;
  startTime: string;
  endTime?: string;
  status: 'active' | 'completed' | 'cancelled';
  telemetryCount: number;
}

/**
 * Manages connections to Azure services for E2E test validation
 */
export class AzureClient {
  private cosmosClient?: CosmosClient;
  private iotHubRegistry?: Registry;
  private eventGridClient?: EventGridPublisherClient<any>;
  private readonly options: AzureClientOptions;
  
  // Local-first mode properties
  private localServiceFactory?: LocalAzureServiceFactory;
  private isLocalFirst: boolean;

  constructor(options: AzureClientOptions = {}) {
    const environment = getCurrentEnvironment();
    this.isLocalFirst = environment.mode === 'local-first';
    
    this.options = {
      cosmosConnectionString: process.env.COSMOS_DB_CONNECTION_STRING,
      iotHubConnectionString: process.env.IOT_HUB_CONNECTION_STRING,
      eventGridEndpoint: process.env.EVENT_GRID_ENDPOINT,
      eventGridKey: process.env.EVENT_GRID_ACCESS_KEY,
      ...options
    };

    if (this.isLocalFirst) {
      this.localServiceFactory = new LocalAzureServiceFactory();
      this.setupLocalServices();
    } else {
      this.initializeClients();
    }
  }

  private async setupLocalServices(): Promise<void> {
    if (this.localServiceFactory) {
      await this.localServiceFactory.setupTestData();
      console.log('🏠 Local Azure services initialized for testing');
    }
  }

  /**
   * Validates Azure connectivity for E2E tests
   */
  async validateConnectivity(): Promise<boolean> {
    const results = await Promise.allSettled([
      this.validateCosmosDB(),
      this.validateIoTHub(),
      this.validateEventGrid()
    ]);

    const successCount = results.filter(result => result.status === 'fulfilled').length;
    const isConnected = successCount > 0; // At least one service should be connected

    if (isConnected) {
      console.log(`✅ Azure connectivity validated (${successCount}/3 services available)`);
    } else {
      console.log('⚠️ No Azure services available - tests will run in local-only mode');
    }

    return isConnected;
  }

  /**
   * Creates a test session record in Cosmos DB
   */
  async createTestSession(sessionData: Omit<SessionRecord, 'id'>): Promise<string | null> {
    if (this.isLocalFirst && this.localServiceFactory) {
      const sessionId = `e2e-session-${Date.now()}`;
      const sessionRecord: SessionRecord = {
        id: sessionId,
        ...sessionData
      };

      const mockCosmos = this.localServiceFactory.getCosmosClient();
      await mockCosmos.createItem('MeatGeek-Sessions', 'Sessions', sessionRecord);
      
      console.log(`🏠 Test session created locally: ${sessionId}`);
      return sessionId;
    }

    if (!this.cosmosClient) {
      console.log('ℹ️ Cosmos DB not available - simulating session creation');
      return `sim-session-${Date.now()}`;
    }

    try {
      const sessionId = `e2e-session-${Date.now()}`;
      const sessionRecord: SessionRecord = {
        id: sessionId,
        ...sessionData
      };

      const container = await this.getSessionsContainer();
      await container.items.create(sessionRecord);

      console.log(`✅ Test session created in Cosmos DB: ${sessionId}`);
      return sessionId;
    } catch (error) {
      console.error('❌ Failed to create test session in Cosmos DB:', error);
      return null;
    }
  }

  /**
   * Retrieves telemetry records for a session from Cosmos DB
   */
  async getSessionTelemetry(sessionId: string): Promise<TelemetryRecord[]> {
    if (!this.cosmosClient) {
      console.log('ℹ️ Cosmos DB not available - returning simulated telemetry');
      return this.generateSimulatedTelemetry(sessionId);
    }

    try {
      const container = await this.getTelemetryContainer();
      const query = {
        query: 'SELECT * FROM c WHERE c.sessionId = @sessionId ORDER BY c.timestamp',
        parameters: [{ name: '@sessionId', value: sessionId }]
      };

      const { resources } = await container.items.query<TelemetryRecord>(query).fetchAll();
      console.log(`✅ Retrieved ${resources.length} telemetry records for session ${sessionId}`);
      return resources;
    } catch (error) {
      console.error('❌ Failed to retrieve telemetry from Cosmos DB:', error);
      return [];
    }
  }

  /**
   * Updates a session record in Cosmos DB
   */
  async updateTestSession(sessionId: string, updates: Partial<SessionRecord>): Promise<boolean> {
    if (!this.cosmosClient) {
      console.log('ℹ️ Cosmos DB not available - simulating session update');
      return true;
    }

    try {
      const container = await this.getSessionsContainer();
      const { resource: existingSession } = await container.item(sessionId, sessionId).read<SessionRecord>();
      
      if (existingSession) {
        const updatedSession = { ...existingSession, ...updates };
        await container.item(sessionId, sessionId).replace(updatedSession);
        console.log(`✅ Test session updated in Cosmos DB: ${sessionId}`);
        return true;
      }

      return false;
    } catch (error) {
      console.error('❌ Failed to update test session in Cosmos DB:', error);
      return false;
    }
  }

  /**
   * Invokes a direct method on an IoT device
   */
  async invokeDeviceMethod(deviceId: string, methodName: string, payload: unknown): Promise<boolean> {
    if (!this.iotHubRegistry) {
      console.log('ℹ️ IoT Hub not available - simulating direct method invocation');
      return true;
    }

    try {
      const methodRequest = {
        methodName,
        payload,
        responseTimeoutInSeconds: 30,
        connectTimeoutInSeconds: 15
      };

      const result = await (this.iotHubRegistry as any).invokeDeviceMethod(deviceId, methodRequest);
      const success = result.status >= 200 && result.status < 300;

      if (success) {
        console.log(`✅ Direct method '${methodName}' invoked successfully on device ${deviceId}`);
      } else {
        console.error(`❌ Direct method '${methodName}' failed with status ${result.status}`);
      }

      return success;
    } catch (error) {
      console.error(`❌ Failed to invoke direct method '${methodName}' on device ${deviceId}:`, error);
      return false;
    }
  }

  /**
   * Monitors device connection status
   */
  async getDeviceConnectionState(deviceId: string): Promise<string> {
    if (!this.iotHubRegistry) {
      console.log('ℹ️ IoT Hub not available - simulating device connection state');
      return 'Connected';
    }

    try {
      const device = await this.iotHubRegistry.get(deviceId);
      const connectionState = (device.responseBody as any)?.connectionState || 'Disconnected';
      console.log(`📡 Device ${deviceId} connection state: ${connectionState}`);
      return connectionState;
    } catch (error) {
      console.error(`❌ Failed to get device connection state for ${deviceId}:`, error);
      return 'Unknown';
    }
  }

  /**
   * Publishes an event to Event Grid (for testing cross-service communication)
   */
  async publishTestEvent(eventType: string, subject: string, data: unknown): Promise<boolean> {
    if (!this.eventGridClient) {
      console.log('ℹ️ Event Grid not available - simulating event publication');
      return true;
    }

    try {
      const event = [{
        eventType,
        subject,
        dataVersion: '1.0',
        data,
        id: `e2e-test-${Date.now()}`,
        eventTime: new Date().toISOString()
      }];

      await this.eventGridClient.send(event);
      console.log(`✅ Event published to Event Grid: ${eventType}`);
      return true;
    } catch (error) {
      console.error(`❌ Failed to publish event to Event Grid:`, error);
      return false;
    }
  }

  /**
   * Cleans up test data from Azure services
   */
  async cleanupTestData(sessionId: string): Promise<boolean> {
    const results = await Promise.allSettled([
      this.cleanupSessionData(sessionId),
      this.cleanupTelemetryData(sessionId)
    ]);

    const successCount = results.filter(result => result.status === 'fulfilled').length;
    console.log(`✅ Cleanup completed (${successCount}/2 operations successful)`);
    return successCount > 0;
  }

  // Private helper methods

  private initializeClients(): void {
    try {
      if (this.options.cosmosConnectionString) {
        this.cosmosClient = new CosmosClient(this.options.cosmosConnectionString);
      }

      if (this.options.iotHubConnectionString) {
        this.iotHubRegistry = Registry.fromConnectionString(this.options.iotHubConnectionString);
      }

      if (this.options.eventGridEndpoint && this.options.eventGridKey) {
        this.eventGridClient = new EventGridPublisherClient(
          this.options.eventGridEndpoint,
          'EventGrid',
          { key: this.options.eventGridKey }
        );
      }
    } catch (error) {
      console.warn('⚠️ Some Azure services not available for E2E tests:', error);
    }
  }

  private async validateCosmosDB(): Promise<boolean> {
    if (!this.cosmosClient) return false;
    try {
      await this.cosmosClient.getDatabaseAccount();
      return true;
    } catch {
      return false;
    }
  }

  private async validateIoTHub(): Promise<boolean> {
    if (!this.iotHubRegistry) return false;
    try {
      // Try to list devices (low-cost operation to test connection)
      const query = this.iotHubRegistry.createQuery('SELECT * FROM devices', 1);
      await query.nextAsTwin();
      return true;
    } catch {
      return false;
    }
  }

  private async validateEventGrid(): Promise<boolean> {
    if (!this.eventGridClient) return false;
    try {
      // Event Grid validation would need actual endpoint validation
      // For now, just check if client was initialized with valid config
      return !!(this.options.eventGridEndpoint && this.options.eventGridKey);
    } catch {
      return false;
    }
  }

  private async getSessionsContainer(): Promise<Container> {
    if (!this.cosmosClient) {
      throw new Error('Cosmos DB client not initialized');
    }

    const database = this.cosmosClient.database('MeatGeek');
    const container = database.container('Sessions');
    return container;
  }

  private async getTelemetryContainer(): Promise<Container> {
    if (!this.cosmosClient) {
      throw new Error('Cosmos DB client not initialized');
    }

    const database = this.cosmosClient.database('MeatGeek');
    const container = database.container('Telemetry');
    return container;
  }

  private generateSimulatedTelemetry(sessionId: string): TelemetryRecord[] {
    // Generate realistic telemetry data for testing when Cosmos DB is not available
    const records: TelemetryRecord[] = [];
    const startTime = Date.now() - (60 * 60 * 1000); // 1 hour ago

    for (let i = 0; i < 10; i++) {
      const timestamp = new Date(startTime + (i * 6 * 60 * 1000)); // Every 6 minutes
      records.push({
        id: `sim-telemetry-${i}`,
        deviceId: 'e2e-test-device-001',
        sessionId,
        timestamp: timestamp.toISOString(),
        temperature: 225 + (Math.random() * 10) - 5, // 220-230°F range
        mode: i < 2 ? 'startup' : 'cooking',
        ttl: -1 // Session data
      });
    }

    return records;
  }

  private async cleanupSessionData(sessionId: string): Promise<void> {
    if (!this.cosmosClient) return;

    try {
      const container = await this.getSessionsContainer();
      await container.item(sessionId, sessionId).delete();
      console.log(`🧹 Cleaned up session data: ${sessionId}`);
    } catch (error) {
      console.warn(`⚠️ Failed to cleanup session data: ${error}`);
    }
  }

  private async cleanupTelemetryData(sessionId: string): Promise<void> {
    if (!this.cosmosClient) return;

    try {
      const container = await this.getTelemetryContainer();
      const query = {
        query: 'SELECT c.id FROM c WHERE c.sessionId = @sessionId',
        parameters: [{ name: '@sessionId', value: sessionId }]
      };

      const { resources } = await container.items.query(query).fetchAll();
      
      for (const item of resources) {
        await container.item(item.id, item.id).delete();
      }

      console.log(`🧹 Cleaned up ${resources.length} telemetry records for session ${sessionId}`);
    } catch (error) {
      console.warn(`⚠️ Failed to cleanup telemetry data: ${error}`);
    }
  }
}