/**
 * Unit tests for MockDeviceController
 */

import { MockDeviceController } from '../../utils/mock-device-controller';
import { SmokerStatus, createTestSmokerStatus } from '../../fixtures/bbq-types';
import axios from 'axios';

// Mock axios
jest.mock('axios');
const mockedAxios = axios as jest.Mocked<typeof axios>;

describe('MockDeviceController', () => {
  let controller: MockDeviceController;
  let mockAxiosInstance: jest.Mocked<any>;

  beforeEach(() => {
    // Reset all mocks
    jest.clearAllMocks();
    
    // Mock axios.create to return our mock instance
    mockAxiosInstance = {
      get: jest.fn(),
      post: jest.fn(),
      interceptors: {
        response: {
          use: jest.fn()
        }
      }
    };
    
    mockedAxios.create.mockReturnValue(mockAxiosInstance);
    
    controller = new MockDeviceController({
      baseUrl: 'http://localhost:3000',
      timeout: 5000,
      retryAttempts: 2
    });
  });

  describe('Constructor and Configuration', () => {
    it('should initialize with default options', () => {
      const defaultController = new MockDeviceController();
      
      expect(mockedAxios.create).toHaveBeenCalledWith({
        baseURL: 'http://localhost:3000',
        timeout: 10000,
        headers: {
          'Content-Type': 'application/json',
          'User-Agent': 'MeatGeek-E2E-Tests/1.0.0'
        }
      });
    });

    it('should initialize with custom options', () => {
      const customController = new MockDeviceController({
        baseUrl: 'http://custom-url:4000',
        timeout: 15000,
        retryAttempts: 5
      });

      expect(mockedAxios.create).toHaveBeenCalledWith({
        baseURL: 'http://custom-url:4000',
        timeout: 15000,
        headers: {
          'Content-Type': 'application/json',
          'User-Agent': 'MeatGeek-E2E-Tests/1.0.0'
        }
      });
    });

    it('should set up retry interceptor', () => {
      expect(mockAxiosInstance.interceptors.response.use).toHaveBeenCalled();
    });
  });

  describe('Health Check', () => {
    it('should return true when MockDevice is healthy', async () => {
      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: {
          status: 'healthy',
          timestamp: new Date().toISOString(),
          uptime: 3600
        }
      });

      const isHealthy = await controller.isHealthy();

      expect(isHealthy).toBe(true);
      expect(mockAxiosInstance.get).toHaveBeenCalledWith('/health');
    });

    it('should return false when MockDevice is unhealthy', async () => {
      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: {
          status: 'unhealthy',
          timestamp: new Date().toISOString()
        }
      });

      const isHealthy = await controller.isHealthy();

      expect(isHealthy).toBe(false);
    });

    it('should return false when HTTP request fails', async () => {
      mockAxiosInstance.get.mockRejectedValue(new Error('Network error'));

      const isHealthy = await controller.isHealthy();

      expect(isHealthy).toBe(false);
    });

    it('should return false for non-200 status codes', async () => {
      mockAxiosInstance.get.mockResolvedValue({
        status: 503,
        data: { status: 'healthy' }
      });

      const isHealthy = await controller.isHealthy();

      expect(isHealthy).toBe(false);
    });
  });

  describe('Get Current Status', () => {
    it('should return smoker status successfully', async () => {
      const expectedStatus = createTestSmokerStatus({
        temps: { grillTemp: 225, probe1Temp: 165 },
        mode: 'cooking',
        setPoint: 225
      });

      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: {
          result: expectedStatus
        }
      });

      const status = await controller.getCurrentStatus();

      expect(status).toEqual(expectedStatus);
      expect(mockAxiosInstance.get).toHaveBeenCalledWith('/api/robots/MeatGeekBot/commands/get_status');
    });

    it('should return null when request fails', async () => {
      mockAxiosInstance.get.mockRejectedValue(new Error('API error'));

      const status = await controller.getCurrentStatus();

      expect(status).toBeNull();
    });

    it('should return null when response has no result', async () => {
      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: {}
      });

      const status = await controller.getCurrentStatus();

      expect(status).toBeNull();
    });

    it('should return null for non-200 status codes', async () => {
      mockAxiosInstance.get.mockResolvedValue({
        status: 404,
        data: { result: createTestSmokerStatus() }
      });

      const status = await controller.getCurrentStatus();

      expect(status).toBeNull();
    });
  });

  describe('Start Cooking Scenario', () => {
    it('should start valid cooking scenarios successfully', async () => {
      const scenarios = ['brisket', 'porkshoulder', 'ribs', 'chicken', 'default'];
      
      for (const scenario of scenarios) {
        mockAxiosInstance.post.mockResolvedValue({
          status: 200,
          data: {
            status: 'started',
            targetTemp: 225,
            scenario
          }
        });

        const result = await controller.startCookingScenario(scenario);

        expect(result).toBe(true);
        expect(mockAxiosInstance.post).toHaveBeenCalledWith(`/api/simulation/start?scenario=${scenario}`);
      }
    });

    it('should handle case-insensitive scenario names', async () => {
      mockAxiosInstance.post.mockResolvedValue({
        status: 200,
        data: { status: 'started', targetTemp: 225 }
      });

      const result = await controller.startCookingScenario('BRISKET');

      expect(result).toBe(true);
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/api/simulation/start?scenario=brisket');
    });

    it('should reject invalid scenarios', async () => {
      const result = await controller.startCookingScenario('invalid-scenario');

      expect(result).toBe(false);
      expect(mockAxiosInstance.post).not.toHaveBeenCalled();
    });

    it('should return false when API call fails', async () => {
      mockAxiosInstance.post.mockRejectedValue(new Error('Network error'));

      const result = await controller.startCookingScenario('brisket');

      expect(result).toBe(false);
    });

    it('should return false for non-200 status codes', async () => {
      mockAxiosInstance.post.mockResolvedValue({
        status: 500,
        data: { status: 'started' }
      });

      const result = await controller.startCookingScenario('brisket');

      expect(result).toBe(false);
    });

    it('should return false when response status is not "started"', async () => {
      mockAxiosInstance.post.mockResolvedValue({
        status: 200,
        data: { status: 'failed' }
      });

      const result = await controller.startCookingScenario('brisket');

      expect(result).toBe(false);
    });
  });

  describe('Stop Cooking', () => {
    it('should stop cooking successfully', async () => {
      mockAxiosInstance.post.mockResolvedValue({
        status: 200,
        data: { status: 'stopped' }
      });

      const result = await controller.stopCooking();

      expect(result).toBe(true);
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/api/simulation/stop');
    });

    it('should return false when API call fails', async () => {
      mockAxiosInstance.post.mockRejectedValue(new Error('Network error'));

      const result = await controller.stopCooking();

      expect(result).toBe(false);
    });

    it('should return false for non-200 status codes', async () => {
      mockAxiosInstance.post.mockResolvedValue({
        status: 404,
        data: { status: 'stopped' }
      });

      const result = await controller.stopCooking();

      expect(result).toBe(false);
    });
  });

  describe('Set Target Temperature', () => {
    it('should set valid temperatures successfully', async () => {
      const validTemps = [150, 225, 350, 450];
      
      for (const temp of validTemps) {
        mockAxiosInstance.post.mockResolvedValue({
          status: 200,
          data: { status: 'temperature set' }
        });

        const result = await controller.setTargetTemperature(temp);

        expect(result).toBe(true);
        expect(mockAxiosInstance.post).toHaveBeenCalledWith(`/api/simulation/settemp?temperature=${temp}`);
      }
    });

    it('should reject temperatures that are too low', async () => {
      const result = await controller.setTargetTemperature(50);

      expect(result).toBe(false);
      expect(mockAxiosInstance.post).not.toHaveBeenCalled();
    });

    it('should reject temperatures that are too high', async () => {
      const result = await controller.setTargetTemperature(600);

      expect(result).toBe(false);
      expect(mockAxiosInstance.post).not.toHaveBeenCalled();
    });

    it('should return false when API call fails', async () => {
      mockAxiosInstance.post.mockRejectedValue(new Error('Network error'));

      const result = await controller.setTargetTemperature(225);

      expect(result).toBe(false);
    });
  });

  describe('Wait For Condition', () => {
    it('should resolve when condition is met immediately', async () => {
      const testStatus = createTestSmokerStatus({ mode: 'cooking' });
      
      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: { result: testStatus }
      });

      const condition = (status: SmokerStatus) => status.mode === 'cooking';
      const result = await controller.waitForCondition(condition, 5000, 1000);

      expect(result).toBe(true);
    });

    it('should resolve when condition is met after retries', async () => {
      const startupStatus = createTestSmokerStatus({ mode: 'startup' });
      const cookingStatus = createTestSmokerStatus({ mode: 'cooking' });
      
      mockAxiosInstance.get
        .mockResolvedValueOnce({ status: 200, data: { result: startupStatus } })
        .mockResolvedValueOnce({ status: 200, data: { result: startupStatus } })
        .mockResolvedValueOnce({ status: 200, data: { result: cookingStatus } });

      const condition = (status: SmokerStatus) => status.mode === 'cooking';
      const result = await controller.waitForCondition(condition, 10000, 100);

      expect(result).toBe(true);
    });

    it('should timeout when condition is never met', async () => {
      const testStatus = createTestSmokerStatus({ mode: 'startup' });
      
      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: { result: testStatus }
      });

      const condition = (status: SmokerStatus) => status.mode === 'cooking';
      const result = await controller.waitForCondition(condition, 500, 100);

      expect(result).toBe(false);
    }, 10000);

    it('should handle API errors gracefully and continue trying', async () => {
      const testStatus = createTestSmokerStatus({ mode: 'cooking' });
      
      mockAxiosInstance.get
        .mockRejectedValueOnce(new Error('Network error'))
        .mockResolvedValueOnce({ status: 200, data: { result: testStatus } });

      const condition = (status: SmokerStatus) => status.mode === 'cooking';
      const result = await controller.waitForCondition(condition, 5000, 100);

      expect(result).toBe(true);
    });
  });

  describe('Wait For Cooking Temperature', () => {
    it('should wait for target temperature with tolerance', async () => {
      const targetStatus = createTestSmokerStatus({
        temps: { grillTemp: 227, probe1Temp: 150 },
        mode: 'cooking'
      });
      
      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: { result: targetStatus }
      });

      const result = await controller.waitForCookingTemperature(225, 15);

      expect(result).toBe(true);
    });

    it('should fail if temperature is outside tolerance', async () => {
      const offTargetStatus = createTestSmokerStatus({
        temps: { grillTemp: 200, probe1Temp: 150 },
        mode: 'cooking'
      });
      
      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: { result: offTargetStatus }
      });

      // Use short timeout for testing
      const result = await controller.waitForCondition(
        (status) => {
          const currentTemp = status.temps.grillTemp;
          const difference = Math.abs(currentTemp - 225);
          return difference <= 5 && status.mode === 'cooking';
        },
        1000, // 1 second timeout for testing
        100   // Check every 100ms
      );

      expect(result).toBe(false);
    });
  });

  describe('Wait For Mode', () => {
    it('should wait for specific cooking mode', async () => {
      const targetStatus = createTestSmokerStatus({ mode: 'cooking' });
      
      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: { result: targetStatus }
      });

      const result = await controller.waitForMode('cooking', 5000);

      expect(result).toBe(true);
    });

    it('should timeout if mode is never reached', async () => {
      const wrongModeStatus = createTestSmokerStatus({ mode: 'startup' });
      
      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: { result: wrongModeStatus }
      });

      const result = await controller.waitForMode('cooking', 500);

      expect(result).toBe(false);
    }, 10000);
  });

  describe('Simulate Progressive Cook', () => {
    beforeEach(() => {
      // Mock timers for these tests to speed them up
      jest.useFakeTimers();
    });

    afterEach(() => {
      jest.useRealTimers();
    });

    it('should execute basic cooking simulation workflow', async () => {
      // Mock API calls for setting temperature and starting scenario
      mockAxiosInstance.post
        .mockResolvedValueOnce({ status: 200, data: { status: 'temperature set' } })
        .mockResolvedValueOnce({ status: 200, data: { status: 'started' } });

      // Mock achieving target temperature quickly
      const cookingStatus = createTestSmokerStatus({ 
        temps: { grillTemp: 225, probe1Temp: 160 },
        mode: 'cooking' 
      });

      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: { result: cookingStatus }
      });

      // Start the simulation but don't await it yet
      const simulationPromise = controller.simulateProgressiveCook(225, 0.01); // Very short duration

      // Fast-forward through the timers
      jest.advanceTimersByTime(1000); // Fast-forward 1 second
      
      const result = await simulationPromise;

      expect(result).toBe(true);
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/api/simulation/settemp?temperature=225');
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/api/simulation/start?scenario=default');
    });

    it('should fail if target temperature is not reached', async () => {
      // Mock setTargetTemperature to succeed
      mockAxiosInstance.post.mockResolvedValue({ status: 200, data: { status: 'temperature set' } });
      
      // Mock temperature that never reaches target
      const lowTempStatus = createTestSmokerStatus({ 
        temps: { grillTemp: 180, probe1Temp: 150 },
        mode: 'startup' 
      });

      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: { result: lowTempStatus }
      });

      // Start simulation
      const simulationPromise = controller.simulateProgressiveCook(225, 0.01);

      // Fast-forward past the wait timeout
      jest.advanceTimersByTime(125000); // Fast-forward past 2 minute timeout
      
      const result = await simulationPromise;

      expect(result).toBe(false);
    });
  });
});