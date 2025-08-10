/**
 * Quick validation test for local-first mode functionality
 */

import { MockDeviceController } from '../utils/mock-device-controller';
import { AzureClient } from '../utils/azure-client';
import { getCurrentEnvironment } from '../config/test-environments';

describe('Local-First Mode Validation', () => {
  let mockDevice: MockDeviceController;
  let azureClient: AzureClient;

  beforeEach(() => {
    // Ensure we're in local-first mode
    process.env.E2E_TEST_MODE = 'local-first';
    
    mockDevice = new MockDeviceController();
    azureClient = new AzureClient();
  });

  afterEach(async () => {
    await mockDevice.stopCooking();
  });

  describe('Environment Configuration', () => {
    it('should detect local-first mode', () => {
      const environment = getCurrentEnvironment();
      expect(environment.mode).toBe('local-first');
      expect(environment.services.mockDevice.enabled).toBe(true);
      expect(environment.services.azure.enabled).toBe(false);
      expect(environment.services.docker.enabled).toBe(false);
    });

    it('should enable appropriate features for local-first', () => {
      const environment = getCurrentEnvironment();
      expect(environment.features.realTelemetry).toBe(false);
      expect(environment.features.crossServiceValidation).toBe(false);
      expect(environment.features.longRunningWorkflows).toBe(true);
      expect(environment.features.performanceTesting).toBe(false);
    });
  });

  describe('MockDevice Local Operation', () => {
    it('should report healthy status locally', async () => {
      const isHealthy = await mockDevice.isHealthy();
      expect(isHealthy).toBe(true);
    });

    it('should get current status in correct format', async () => {
      const status = await mockDevice.getCurrentStatus();
      
      expect(status).not.toBeNull();
      expect(status).toHaveProperty('temps');
      expect(status).toHaveProperty('setPoint');
      expect(status).toHaveProperty('mode');
      expect(status!.temps).toHaveProperty('grillTemp');
      expect(status!.temps).toHaveProperty('probe1Temp');
      expect(typeof status!.setPoint).toBe('number');
    });

    it('should start cooking scenarios locally', async () => {
      const result = await mockDevice.startCookingScenario('brisket');
      expect(result).toBe(true);

      // Wait a moment for simulation to initialize
      await new Promise(resolve => setTimeout(resolve, 100));

      const status = await mockDevice.getCurrentStatus();
      expect(status?.mode).toBe('cooking');
      expect(status?.sessionId).toBeTruthy();
    });

    it('should handle temperature changes locally', async () => {
      const result = await mockDevice.setTargetTemperature(250);
      expect(result).toBe(true);
    });

    it('should stop cooking sessions locally', async () => {
      await mockDevice.startCookingScenario('quick-test');
      const stopResult = await mockDevice.stopCooking();
      expect(stopResult).toBe(true);
    });
  });

  describe('Azure Services Mock Operation', () => {
    it('should create test sessions locally', async () => {
      const sessionId = await azureClient.createTestSession({
        userId: 'test-user',
        deviceId: 'mock-device-001',
        startTime: new Date().toISOString(),
        status: 'active',
        telemetryCount: 0
      });

      expect(sessionId).toBeTruthy();
      expect(sessionId).toMatch(/^e2e-session-\d+$/);
    });

    it('should validate connectivity in local mode', async () => {
      const isConnected = await azureClient.validateConnectivity();
      // Should return true for local mode (mocked services are always "available")
      expect(isConnected).toBe(true);
    });
  });

  describe('Integration Test', () => {
    it('should complete a full local workflow', async () => {
      // Create a session
      const sessionId = await azureClient.createTestSession({
        userId: 'e2e-user',
        deviceId: 'mock-device-001',
        startTime: new Date().toISOString(),
        status: 'active',
        telemetryCount: 0
      });
      expect(sessionId).toBeTruthy();

      // Start cooking
      const cookingStarted = await mockDevice.startCookingScenario('quick-test');
      expect(cookingStarted).toBe(true);

      // Get initial status
      const initialStatus = await mockDevice.getCurrentStatus();
      expect(initialStatus?.mode).toBe('cooking');

      // Wait for a few simulation ticks
      await new Promise(resolve => setTimeout(resolve, 1000));

      // Get updated status
      const updatedStatus = await mockDevice.getCurrentStatus();
      expect(updatedStatus?.mode).toBe('cooking');
      
      // Temperatures might have changed slightly
      expect(updatedStatus?.temps.grillTemp).toBeGreaterThan(0);

      // Stop cooking
      const cookingStopped = await mockDevice.stopCooking();
      expect(cookingStopped).toBe(true);

      // Verify stopped
      const finalStatus = await mockDevice.getCurrentStatus();
      expect(finalStatus?.mode).toBe('idle');
      expect(finalStatus?.sessionId).toBeUndefined();
    }, 10000); // 10 second timeout for this integration test
  });
});