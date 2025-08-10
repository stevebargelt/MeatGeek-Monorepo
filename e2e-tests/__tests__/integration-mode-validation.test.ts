/**
 * Validation test for integration mode with Docker services
 */

import { ExistingTestRunner } from '../utils/existing-test-runner';
import { MockDeviceController } from '../utils/mock-device-controller';
import { getCurrentEnvironment } from '../config/test-environments';

describe('Integration Mode Validation', () => {
  let testRunner: ExistingTestRunner;
  let mockDevice: MockDeviceController;

  beforeAll(async () => {
    // Set integration mode
    process.env.E2E_TEST_MODE = 'integration';
    
    testRunner = new ExistingTestRunner();
    mockDevice = new MockDeviceController();
  }, 30000); // 30 second timeout for setup

  afterAll(async () => {
    await testRunner.stopExistingEnvironment();
  }, 15000); // 15 second timeout for cleanup

  describe('Environment Configuration', () => {
    it('should detect integration mode', () => {
      const environment = getCurrentEnvironment();
      expect(environment.mode).toBe('integration');
      expect(environment.services.mockDevice.enabled).toBe(true);
      expect(environment.services.azure.enabled).toBe(false); // Still using local Azure mocks
      expect(environment.services.docker.enabled).toBe(true);
    });

    it('should enable appropriate features for integration', () => {
      const environment = getCurrentEnvironment();
      expect(environment.features.realTelemetry).toBe(true);
      expect(environment.features.crossServiceValidation).toBe(true);
      expect(environment.features.longRunningWorkflows).toBe(true);
      expect(environment.features.performanceTesting).toBe(false);
    });
  });

  describe('Docker Environment Management', () => {
    it('should start Docker services successfully', async () => {
      const started = await testRunner.startExistingEnvironment();
      expect(started).toBe(true);
    }, 60000); // 60 seconds for Docker to start

    it('should connect to MockDevice service', async () => {
      const isHealthy = await mockDevice.isHealthy();
      expect(isHealthy).toBe(true);
    }, 10000);

    it('should get telemetry from MockDevice', async () => {
      const status = await mockDevice.getCurrentStatus();
      
      expect(status).not.toBeNull();
      expect(status).toHaveProperty('temps');
      expect(status).toHaveProperty('setPoint');
      expect(status).toHaveProperty('mode');
      expect(typeof status!.setPoint).toBe('number');
    }, 10000);
  });

  describe('Service Integration', () => {
    it('should start and stop cooking scenarios', async () => {
      // Start a cooking scenario
      const started = await mockDevice.startCookingScenario('ribs');
      expect(started).toBe(true);

      // Wait for scenario to initialize
      await new Promise(resolve => setTimeout(resolve, 2000));

      // Verify status shows cooking
      const status = await mockDevice.getCurrentStatus();
      expect(status?.mode).toBe('cooking');

      // Stop the scenario
      const stopped = await mockDevice.stopCooking();
      expect(stopped).toBe(true);

      // Verify status shows idle
      const finalStatus = await mockDevice.getCurrentStatus();
      expect(finalStatus?.mode).toBe('idle');
    }, 20000);

    it('should handle temperature changes', async () => {
      const result = await mockDevice.setTargetTemperature(275);
      expect(result).toBe(true);
    });
  });

  describe('Environment Cleanup', () => {
    it('should stop Docker environment cleanly', async () => {
      const stopped = await testRunner.stopExistingEnvironment();
      expect(stopped).toBe(true);
    }, 30000);
  });
});