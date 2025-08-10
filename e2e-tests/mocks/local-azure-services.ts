/**
 * Local mock implementations of Azure services for local-first testing
 */

import { EventEmitter } from 'events';

export interface MockCosmosRecord {
  id: string;
  [key: string]: any;
}

export interface MockEventGridEvent {
  id: string;
  eventType: string;
  subject: string;
  data: any;
  eventTime: string;
  dataVersion: string;
}

export class MockCosmosClient extends EventEmitter {
  private collections: Map<string, Map<string, MockCosmosRecord>> = new Map();

  constructor() {
    super();
  }

  // Database operations
  async createDatabase(databaseId: string): Promise<void> {
    // No-op for mock
  }

  async createContainer(databaseId: string, containerId: string): Promise<void> {
    const key = `${databaseId}/${containerId}`;
    if (!this.collections.has(key)) {
      this.collections.set(key, new Map());
    }
  }

  // Document operations
  async createItem<T extends MockCosmosRecord>(
    databaseId: string,
    containerId: string,
    item: T
  ): Promise<T> {
    const key = `${databaseId}/${containerId}`;
    const collection = this.collections.get(key) || new Map();
    
    if (!item.id) {
      item.id = `mock-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    }

    collection.set(item.id, { ...item });
    this.collections.set(key, collection);

    this.emit('itemCreated', { databaseId, containerId, item });
    return { ...item };
  }

  async readItem<T extends MockCosmosRecord>(
    databaseId: string,
    containerId: string,
    itemId: string
  ): Promise<T | null> {
    const key = `${databaseId}/${containerId}`;
    const collection = this.collections.get(key);
    const item = collection?.get(itemId);
    return item ? { ...item } as T : null;
  }

  async queryItems<T extends MockCosmosRecord>(
    databaseId: string,
    containerId: string,
    query: string,
    parameters?: Array<{ name: string; value: any }>
  ): Promise<T[]> {
    const key = `${databaseId}/${containerId}`;
    const collection = this.collections.get(key);
    
    if (!collection) return [];

    // Simple mock query - just return all items for now
    // In a real implementation, you'd parse the SQL query
    const allItems = Array.from(collection.values()) as T[];
    
    // Basic filtering for sessionId queries (common pattern)
    if (query.includes('c.sessionId') && parameters?.length) {
      const sessionIdParam = parameters.find(p => p.name === '@sessionId');
      if (sessionIdParam) {
        return allItems.filter(item => (item as any).sessionId === sessionIdParam.value);
      }
    }

    return allItems;
  }

  async updateItem<T extends MockCosmosRecord>(
    databaseId: string,
    containerId: string,
    itemId: string,
    item: Partial<T>
  ): Promise<T | null> {
    const key = `${databaseId}/${containerId}`;
    const collection = this.collections.get(key);
    const existingItem = collection?.get(itemId);

    if (!existingItem) return null;

    const updatedItem = { ...existingItem, ...item, id: itemId };
    collection!.set(itemId, updatedItem);

    this.emit('itemUpdated', { databaseId, containerId, item: updatedItem });
    return { ...updatedItem } as T;
  }

  async deleteItem(
    databaseId: string,
    containerId: string,
    itemId: string
  ): Promise<boolean> {
    const key = `${databaseId}/${containerId}`;
    const collection = this.collections.get(key);
    const deleted = collection?.delete(itemId) || false;

    if (deleted) {
      this.emit('itemDeleted', { databaseId, containerId, itemId });
    }

    return deleted;
  }

  // Utility methods for testing
  clear(): void {
    this.collections.clear();
    this.emit('cleared');
  }

  getCollectionSize(databaseId: string, containerId: string): number {
    const key = `${databaseId}/${containerId}`;
    return this.collections.get(key)?.size || 0;
  }

  getAllItems<T extends MockCosmosRecord>(databaseId: string, containerId: string): T[] {
    const key = `${databaseId}/${containerId}`;
    const collection = this.collections.get(key);
    return collection ? Array.from(collection.values()) as T[] : [];
  }
}

export class MockEventGridClient extends EventEmitter {
  private events: MockEventGridEvent[] = [];

  constructor() {
    super();
  }

  async publishEvent(topicEndpoint: string, event: Omit<MockEventGridEvent, 'id' | 'eventTime'>): Promise<string> {
    const fullEvent: MockEventGridEvent = {
      id: `mock-event-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      eventTime: new Date().toISOString(),
      ...event
    };

    this.events.push(fullEvent);
    this.emit('eventPublished', { topicEndpoint, event: fullEvent });

    // Simulate async processing
    setTimeout(() => {
      this.emit('eventProcessed', fullEvent);
    }, 10);

    return fullEvent.id;
  }

  async publishEvents(topicEndpoint: string, events: Array<Omit<MockEventGridEvent, 'id' | 'eventTime'>>): Promise<string[]> {
    const eventIds: string[] = [];
    
    for (const event of events) {
      const eventId = await this.publishEvent(topicEndpoint, event);
      eventIds.push(eventId);
    }

    return eventIds;
  }

  // Test utilities
  getEvents(eventType?: string): MockEventGridEvent[] {
    if (eventType) {
      return this.events.filter(e => e.eventType === eventType);
    }
    return [...this.events];
  }

  getEventCount(eventType?: string): number {
    return this.getEvents(eventType).length;
  }

  clear(): void {
    this.events = [];
    this.emit('cleared');
  }

  // Wait for events (useful in tests)
  async waitForEvent(eventType: string, timeout: number = 5000): Promise<MockEventGridEvent> {
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => {
        reject(new Error(`Timeout waiting for event type: ${eventType}`));
      }, timeout);

      const handler = (event: MockEventGridEvent) => {
        if (event.eventType === eventType) {
          clearTimeout(timer);
          this.removeListener('eventProcessed', handler);
          resolve(event);
        }
      };

      this.on('eventProcessed', handler);

      // Check if event already exists
      const existingEvent = this.events.find(e => e.eventType === eventType);
      if (existingEvent) {
        clearTimeout(timer);
        this.removeListener('eventProcessed', handler);
        resolve(existingEvent);
      }
    });
  }
}

export class MockIoTHubClient extends EventEmitter {
  private devices: Map<string, any> = new Map();
  private telemetryMessages: Array<{ deviceId: string; message: any; timestamp: string }> = [];

  constructor() {
    super();
  }

  // Device management
  async createDevice(deviceId: string, deviceInfo: any = {}): Promise<void> {
    this.devices.set(deviceId, {
      deviceId,
      status: 'enabled',
      connectionState: 'disconnected',
      lastActivity: new Date().toISOString(),
      ...deviceInfo
    });

    this.emit('deviceCreated', { deviceId, deviceInfo });
  }

  async getDevice(deviceId: string): Promise<any | null> {
    return this.devices.get(deviceId) || null;
  }

  async deleteDevice(deviceId: string): Promise<boolean> {
    const deleted = this.devices.delete(deviceId);
    if (deleted) {
      this.emit('deviceDeleted', { deviceId });
    }
    return deleted;
  }

  // Telemetry simulation
  async sendTelemetry(deviceId: string, message: any): Promise<void> {
    const telemetryRecord = {
      deviceId,
      message: { ...message },
      timestamp: new Date().toISOString()
    };

    this.telemetryMessages.push(telemetryRecord);
    this.emit('telemetryReceived', telemetryRecord);

    // Update device last activity
    const device = this.devices.get(deviceId);
    if (device) {
      device.lastActivity = telemetryRecord.timestamp;
      device.connectionState = 'connected';
    }
  }

  // Direct methods (for device control)
  async invokeDirectMethod(deviceId: string, methodName: string, payload: any = {}): Promise<any> {
    const device = this.devices.get(deviceId);
    if (!device) {
      throw new Error(`Device not found: ${deviceId}`);
    }

    // Mock direct method responses
    const mockResponses: Record<string, any> = {
      'StartCookingSession': { status: 'success', sessionId: `session-${Date.now()}` },
      'StopCookingSession': { status: 'success' },
      'GetDeviceStatus': { 
        status: 'success', 
        data: { 
          deviceId, 
          temperature: 225, 
          batteryLevel: 85 
        } 
      },
      'UpdateTargetTemperature': { status: 'success', targetTemp: payload.targetTemp }
    };

    const response = mockResponses[methodName] || { status: 'success', message: 'Method executed' };
    
    this.emit('directMethodInvoked', { deviceId, methodName, payload, response });
    return response;
  }

  // Utility methods for testing
  getTelemetryMessages(deviceId?: string): Array<{ deviceId: string; message: any; timestamp: string }> {
    if (deviceId) {
      return this.telemetryMessages.filter(t => t.deviceId === deviceId);
    }
    return [...this.telemetryMessages];
  }

  getTelemetryCount(deviceId?: string): number {
    return this.getTelemetryMessages(deviceId).length;
  }

  getConnectedDevices(): string[] {
    const connectedDevices: string[] = [];
    this.devices.forEach((device, deviceId) => {
      if (device.connectionState === 'connected') {
        connectedDevices.push(deviceId);
      }
    });
    return connectedDevices;
  }

  clear(): void {
    this.devices.clear();
    this.telemetryMessages = [];
    this.emit('cleared');
  }
}

// Factory for creating all mock services
export class LocalAzureServiceFactory {
  private cosmosClient?: MockCosmosClient;
  private eventGridClient?: MockEventGridClient;
  private iotHubClient?: MockIoTHubClient;

  getCosmosClient(): MockCosmosClient {
    if (!this.cosmosClient) {
      this.cosmosClient = new MockCosmosClient();
    }
    return this.cosmosClient;
  }

  getEventGridClient(): MockEventGridClient {
    if (!this.eventGridClient) {
      this.eventGridClient = new MockEventGridClient();
    }
    return this.eventGridClient;
  }

  getIoTHubClient(): MockIoTHubClient {
    if (!this.iotHubClient) {
      this.iotHubClient = new MockIoTHubClient();
    }
    return this.iotHubClient;
  }

  clearAll(): void {
    this.cosmosClient?.clear();
    this.eventGridClient?.clear();
    this.iotHubClient?.clear();
  }

  async setupTestData(): Promise<void> {
    // Create default test databases/containers
    const cosmos = this.getCosmosClient();
    await cosmos.createDatabase('MeatGeek-Sessions');
    await cosmos.createContainer('MeatGeek-Sessions', 'Sessions');
    await cosmos.createContainer('MeatGeek-Sessions', 'Telemetry');
    
    // Create test IoT device
    const iotHub = this.getIoTHubClient();
    await iotHub.createDevice('mock-device-001', {
      deviceType: 'smoker',
      model: 'MeatGeek Pro',
      firmware: '1.0.0'
    });
  }
}