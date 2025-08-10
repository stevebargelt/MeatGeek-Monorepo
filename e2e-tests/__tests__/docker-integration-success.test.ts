/**
 * Focused integration test validating Docker services work correctly
 */

import { MockDeviceController } from '../utils/mock-device-controller';
import { getCurrentEnvironment } from '../config/test-environments';

describe('Docker Integration Success Validation', () => {
  let mockDevice: MockDeviceController;

  beforeAll(() => {
    // Ensure we're in integration mode
    process.env.E2E_TEST_MODE = 'integration';
    mockDevice = new MockDeviceController();
  });

  describe('Environment Configuration', () => {
    it('should detect integration mode correctly', () => {
      const environment = getCurrentEnvironment();
      expect(environment.mode).toBe('integration');
      expect(environment.services.mockDevice.enabled).toBe(true);
      expect(environment.services.docker.enabled).toBe(true);
    });
  });

  describe('Docker Services Connection', () => {
    it('should connect to MockDevice successfully', async () => {
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
    }, 5000);

    it('should receive realistic telemetry data', async () => {
      const status = await mockDevice.getCurrentStatus();
      
      expect(status?.temps.grillTemp).toBeGreaterThan(60); // Room temp or higher
      expect(status?.temps.grillTemp).toBeLessThan(800); // Reasonable max
      expect(status?.temps.probe1Temp).toBeGreaterThan(60);
      expect(status?.temps.probe1Temp).toBeLessThan(800);
      expect(status?.setPoint).toBeGreaterThan(100);
      expect(status?.setPoint).toBeLessThan(600);
    }, 5000);
  });

  describe('MockDevice API Operations', () => {
    it('should handle temperature changes', async () => {
      const result = await mockDevice.setTargetTemperature(250);
      expect(result).toBe(true);
    }, 5000);

    it('should start cooking scenarios', async () => {
      const result = await mockDevice.startCookingScenario('ribs');
      expect(result).toBe(true);

      // Wait a moment for state change
      await new Promise(resolve => setTimeout(resolve, 1000));

      const status = await mockDevice.getCurrentStatus();
      expect(status?.mode).toMatch(/startup|cooking|idle/); // Any valid cooking state
    }, 10000);

    it('should stop cooking scenarios', async () => {
      const result = await mockDevice.stopCooking();
      expect(result).toBe(true);
    }, 5000);
  });

  describe('Integration Mode Features', () => {
    it('should enable correct features for integration mode', () => {
      const environment = getCurrentEnvironment();
      expect(environment.features.realTelemetry).toBe(true);
      expect(environment.features.crossServiceValidation).toBe(true);
      expect(environment.features.longRunningWorkflows).toBe(true);
    });

    it('should use external MockDevice URL', () => {
      const environment = getCurrentEnvironment();
      expect(environment.services.mockDevice.baseUrl).toBe('http://localhost:8080');
    });
  });

  describe('Cross-Service Validation', () => {
    it('should validate telemetry data format matches expectations', async () => {
      const status = await mockDevice.getCurrentStatus();
      
      // Validate structure matches what other services expect
      expect(status).toHaveProperty('temps');
      expect(status?.temps).toHaveProperty('grillTemp');
      expect(status?.temps).toHaveProperty('probe1Temp');
      expect(status).toHaveProperty('setPoint');
      expect(status).toHaveProperty('mode');
      expect(status).toHaveProperty('smokerId');
      
      // Validate data types
      expect(typeof status?.temps.grillTemp).toBe('number');
      expect(typeof status?.temps.probe1Temp).toBe('number');
      expect(typeof status?.setPoint).toBe('number');
      expect(typeof status?.mode).toBe('string');
    }, 5000);

    it('should maintain session context across calls', async () => {
      // Start a cooking scenario
      await mockDevice.startCookingScenario('brisket');
      
      // Get status multiple times
      const status1 = await mockDevice.getCurrentStatus();
      await new Promise(resolve => setTimeout(resolve, 100));
      const status2 = await mockDevice.getCurrentStatus();
      
      // Should maintain session context
      expect(status1?.smokerId).toBe(status2?.smokerId);
      expect(status1?.setPoint).toBe(status2?.setPoint);
      
      // Cleanup
      await mockDevice.stopCooking();
    }, 10000);
  });
});