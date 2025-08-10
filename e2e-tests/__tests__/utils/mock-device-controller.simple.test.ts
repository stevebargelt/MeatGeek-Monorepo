/**
 * Simplified unit tests for MockDeviceController (core functionality only)
 */

import { MockDeviceController } from '../../utils/mock-device-controller';
import { SmokerStatus, createTestSmokerStatus } from '../../fixtures/bbq-types';
import axios from 'axios';

// Mock axios
jest.mock('axios');
const mockedAxios = axios as jest.Mocked<typeof axios>;

describe('MockDeviceController - Core Functionality', () => {
  let controller: MockDeviceController;
  let mockAxiosInstance: jest.Mocked<any>;

  beforeEach(() => {
    jest.clearAllMocks();
    
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
    controller = new MockDeviceController();
  });

  describe('Initialization', () => {
    it('should create axios instance with correct configuration', () => {
      expect(mockedAxios.create).toHaveBeenCalledWith({
        baseURL: 'http://localhost:3000',
        timeout: 10000,
        headers: {
          'Content-Type': 'application/json',
          'User-Agent': 'MeatGeek-E2E-Tests/1.0.0'
        }
      });
    });

    it('should setup retry interceptor', () => {
      expect(mockAxiosInstance.interceptors.response.use).toHaveBeenCalledTimes(1);
    });
  });

  describe('Health Check', () => {
    it('should return true when MockDevice is healthy', async () => {
      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: { status: 'healthy', timestamp: new Date().toISOString() }
      });

      const result = await controller.isHealthy();

      expect(result).toBe(true);
      expect(mockAxiosInstance.get).toHaveBeenCalledWith('/health');
    });

    it('should return false when MockDevice returns unhealthy status', async () => {
      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: { status: 'unhealthy', timestamp: new Date().toISOString() }
      });

      const result = await controller.isHealthy();
      expect(result).toBe(false);
    });

    it('should return false on API errors', async () => {
      mockAxiosInstance.get.mockRejectedValue(new Error('Connection failed'));

      const result = await controller.isHealthy();
      expect(result).toBe(false);
    });
  });

  describe('Get Current Status', () => {
    it('should return smoker status when API call succeeds', async () => {
      const testStatus = createTestSmokerStatus({
        temps: { grillTemp: 225, probe1Temp: 165 },
        mode: 'cooking'
      });

      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: { result: testStatus }
      });

      const result = await controller.getCurrentStatus();

      expect(result).toEqual(testStatus);
      expect(mockAxiosInstance.get).toHaveBeenCalledWith('/api/robots/MeatGeekBot/commands/get_status');
    });

    it('should return null when API call fails', async () => {
      mockAxiosInstance.get.mockRejectedValue(new Error('API Error'));

      const result = await controller.getCurrentStatus();
      expect(result).toBeNull();
    });

    it('should return null when response has no result', async () => {
      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: { message: 'success' } // No result field
      });

      const result = await controller.getCurrentStatus();
      expect(result).toBeNull();
    });
  });

  describe('Start Cooking Scenario', () => {
    it('should start valid scenarios successfully', async () => {
      mockAxiosInstance.post.mockResolvedValue({
        status: 200,
        data: { status: 'started', targetTemp: 225, scenario: 'brisket' }
      });

      const result = await controller.startCookingScenario('brisket');

      expect(result).toBe(true);
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/api/simulation/start?scenario=brisket');
    });

    it('should handle all valid scenario types', async () => {
      const scenarios = ['brisket', 'porkshoulder', 'ribs', 'chicken', 'default'];
      
      for (const scenario of scenarios) {
        mockAxiosInstance.post.mockResolvedValue({
          status: 200,
          data: { status: 'started', targetTemp: 225 }
        });

        const result = await controller.startCookingScenario(scenario);
        expect(result).toBe(true);
      }
    });

    it('should convert scenario names to lowercase', async () => {
      mockAxiosInstance.post.mockResolvedValue({
        status: 200,
        data: { status: 'started', targetTemp: 225 }
      });

      await controller.startCookingScenario('BRISKET');

      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/api/simulation/start?scenario=brisket');
    });

    it('should reject invalid scenario names', async () => {
      const result = await controller.startCookingScenario('invalid-scenario');

      expect(result).toBe(false);
      expect(mockAxiosInstance.post).not.toHaveBeenCalled();
    });

    it('should return false when API call fails', async () => {
      mockAxiosInstance.post.mockRejectedValue(new Error('Network error'));

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

    it('should return false when stop fails', async () => {
      mockAxiosInstance.post.mockRejectedValue(new Error('Stop failed'));

      const result = await controller.stopCooking();
      expect(result).toBe(false);
    });
  });

  describe('Set Target Temperature', () => {
    it('should set valid temperatures successfully', async () => {
      mockAxiosInstance.post.mockResolvedValue({
        status: 200,
        data: { status: 'temperature set' }
      });

      const result = await controller.setTargetTemperature(225);

      expect(result).toBe(true);
      expect(mockAxiosInstance.post).toHaveBeenCalledWith('/api/simulation/settemp?temperature=225');
    });

    it('should reject temperatures below 100°F', async () => {
      const result = await controller.setTargetTemperature(50);

      expect(result).toBe(false);
      expect(mockAxiosInstance.post).not.toHaveBeenCalled();
    });

    it('should reject temperatures above 500°F', async () => {
      const result = await controller.setTargetTemperature(600);

      expect(result).toBe(false);
      expect(mockAxiosInstance.post).not.toHaveBeenCalled();
    });

    it('should accept temperatures in valid range', async () => {
      const validTemps = [100, 225, 350, 500];
      
      mockAxiosInstance.post.mockResolvedValue({
        status: 200,
        data: { status: 'temperature set' }
      });

      for (const temp of validTemps) {
        const result = await controller.setTargetTemperature(temp);
        expect(result).toBe(true);
      }

      expect(mockAxiosInstance.post).toHaveBeenCalledTimes(4);
    });
  });

  describe('Basic Condition Checking', () => {
    it('should resolve immediately when condition is already met', async () => {
      const testStatus = createTestSmokerStatus({ mode: 'cooking' });
      
      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: { result: testStatus }
      });

      const condition = (status: SmokerStatus) => status.mode === 'cooking';
      
      // Use a very short timeout to avoid hanging
      const result = await controller.waitForCondition(condition, 100, 50);

      expect(result).toBe(true);
    });

    it('should handle API errors during condition checking', async () => {
      mockAxiosInstance.get.mockRejectedValue(new Error('API Error'));

      const condition = (status: SmokerStatus) => status.mode === 'cooking';
      
      const result = await controller.waitForCondition(condition, 100, 50);

      expect(result).toBe(false);
    });
  });

  describe('Temperature Monitoring', () => {
    it('should detect when cooking temperature is achieved', async () => {
      const targetStatus = createTestSmokerStatus({
        temps: { grillTemp: 223, probe1Temp: 150 }, // Within 15° tolerance of 225
        mode: 'cooking'
      });
      
      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: { result: targetStatus }
      });

      const result = await controller.waitForCookingTemperature(225, 15);
      expect(result).toBe(true);
    });

    it('should detect when mode is reached', async () => {
      const targetStatus = createTestSmokerStatus({ mode: 'cooking' });
      
      mockAxiosInstance.get.mockResolvedValue({
        status: 200,
        data: { result: targetStatus }
      });

      const result = await controller.waitForMode('cooking', 100);
      expect(result).toBe(true);
    });
  });
});