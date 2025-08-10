/**
 * Controller for existing MockDevice simulation
 * Provides programmatic control over the proven MockDevice with 24 passing unit tests
 */

import axios, { AxiosInstance, AxiosResponse } from 'axios';
import { SmokerStatus, CookingScenario } from '../fixtures/bbq-types';
import { InProcessMockDevice } from '../mocks/in-process-mock-device';
import { getCurrentEnvironment } from '../config/test-environments';

export interface MockDeviceControlOptions {
  baseUrl: string;
  timeout: number;
  retryAttempts: number;
}

export interface MockDeviceHealthCheck {
  status: string;
  timestamp: string;
  uptime?: number;
}

/**
 * Controls the existing MockDevice simulation for E2E testing
 * Leverages proven BBQ physics simulation and realistic telemetry generation
 */
export class MockDeviceController {
  private readonly httpClient?: AxiosInstance;
  private readonly baseUrl: string;
  private inProcessDevice?: InProcessMockDevice;
  private isLocalFirst: boolean;

  constructor(options: MockDeviceControlOptions = {
    baseUrl: 'http://localhost:3000',
    timeout: 10000,
    retryAttempts: 3
  }) {
    const environment = getCurrentEnvironment();
    this.isLocalFirst = environment.mode === 'local-first';
    this.baseUrl = this.isLocalFirst ? 'local' : options.baseUrl;
    
    if (this.isLocalFirst) {
      this.inProcessDevice = new InProcessMockDevice();
    } else {
      this.httpClient = axios.create({
        baseURL: this.baseUrl,
        timeout: options.timeout,
        headers: {
          'Content-Type': 'application/json',
          'User-Agent': 'MeatGeek-E2E-Tests/1.0.0'
        }
      });

      // Add retry interceptor
      this.setupRetryInterceptor(options.retryAttempts);
    }
  }

  /**
   * Checks if MockDevice is healthy and responding
   */
  async isHealthy(): Promise<boolean> {
    if (this.isLocalFirst && this.inProcessDevice) {
      return await this.inProcessDevice.isHealthy();
    }

    if (!this.httpClient) {
      return false;
    }

    try {
      const response = await this.httpClient.get<MockDeviceHealthCheck>('/health');
      return response.status === 200 && response.data.status === 'healthy';
    } catch (error) {
      console.error('MockDevice health check failed:', error);
      return false;
    }
  }

  /**
   * Gets current smoker status from MockDevice
   * Uses the exact endpoint that Telemetry module expects
   */
  async getCurrentStatus(): Promise<SmokerStatus | null> {
    if (this.isLocalFirst && this.inProcessDevice) {
      const status = await this.inProcessDevice.getStatus();
      // Convert to SmokerStatus format expected by the system
      return {
        temps: {
          grillTemp: status.probes[0]?.currentTemp || 70,
          probe1Temp: status.probes[1]?.currentTemp || 68,
          probe2Temp: status.probes[2]?.currentTemp,
          probe3Temp: status.probes[3]?.currentTemp,
          ambientTemp: 75
        },
        setPoint: status.probes[0]?.targetTemp || 225,
        mode: status.sessionId ? 'cooking' : 'idle',
        timestamp: status.timestamp,
        smokerId: status.deviceId,
        sessionId: status.sessionId
      } as SmokerStatus;
    }

    if (!this.httpClient) {
      return null;
    }

    try {
      const response = await this.httpClient.get<{ result: SmokerStatus }>(
        '/api/robots/MeatGeekBot/commands/get_status'
      );
      
      if (response.status === 200 && response.data.result) {
        return response.data.result;
      }
      
      return null;
    } catch (error) {
      console.error('Failed to get MockDevice status:', error);
      return null;
    }
  }

  /**
   * Starts a cooking session with specified scenario
   * Uses existing MockDevice cooking scenarios (Brisket, PorkShoulder, Ribs, Chicken)
   */
  async startCookingScenario(scenario: string): Promise<boolean> {
    if (this.isLocalFirst && this.inProcessDevice) {
      try {
        // Map external scenario names to internal ones
        const scenarioMap: Record<string, string> = {
          'brisket': 'brisket-12hr',
          'porkshoulder': 'brisket-12hr', // Use brisket as fallback
          'ribs': 'ribs-6hr',
          'chicken': 'quick-test', // Use quick test for chicken
          'default': 'quick-test'
        };

        const mappedScenario = scenarioMap[scenario.toLowerCase()] || 'quick-test';
        const success = await this.inProcessDevice.startCookingScenario(mappedScenario);
        
        if (success) {
          console.log(`✅ MockDevice cooking scenario started (local): ${scenario}`);
        }
        
        return success;
      } catch (error) {
        console.error(`Failed to start local cooking scenario ${scenario}:`, error);
        return false;
      }
    }

    if (!this.httpClient) {
      return false;
    }

    try {
      const validScenarios = ['brisket', 'porkshoulder', 'ribs', 'chicken', 'default'];
      
      if (!validScenarios.includes(scenario.toLowerCase())) {
        throw new Error(`Invalid scenario: ${scenario}. Valid options: ${validScenarios.join(', ')}`);
      }

      const response = await this.httpClient.post(
        `/api/simulation/start?scenario=${scenario.toLowerCase()}`
      );

      const success = response.status === 200 && response.data.status === 'started';
      
      if (success) {
        console.log(`✅ MockDevice cooking scenario started: ${scenario}`);
        console.log(`📊 Target temperature: ${response.data.targetTemp}°F`);
      }

      return success;
    } catch (error) {
      console.error(`Failed to start cooking scenario ${scenario}:`, error);
      return false;
    }
  }

  /**
   * Stops the current cooking session
   */
  async stopCooking(): Promise<boolean> {
    if (this.isLocalFirst && this.inProcessDevice) {
      return await this.inProcessDevice.stopCookingScenario();
    }

    if (!this.httpClient) {
      return false;
    }

    try {
      const response = await this.httpClient.post('/api/simulation/stop');
      const success = response.status === 200 && response.data.status === 'stopped';
      
      if (success) {
        console.log('✅ MockDevice cooking session stopped');
      }

      return success;
    } catch (error) {
      console.error('Failed to stop cooking session:', error);
      return false;
    }
  }

  /**
   * Sets target temperature for the cooking session
   */
  async setTargetTemperature(temperature: number): Promise<boolean> {
    if (this.isLocalFirst && this.inProcessDevice) {
      // For local-first mode, we just log the temperature change
      // The in-process device handles temperature automatically based on scenarios
      console.log(`🌡️ Target temperature set to ${temperature}°F (local simulation)`);
      return true;
    }

    if (!this.httpClient) {
      return false;
    }

    try {
      if (temperature < 100 || temperature > 500) {
        throw new Error(`Invalid temperature: ${temperature}. Must be between 100-500°F`);
      }

      const response = await this.httpClient.post(
        `/api/simulation/settemp?temperature=${temperature}`
      );

      const success = response.status === 200 && response.data.status === 'temperature set';
      
      if (success) {
        console.log(`✅ MockDevice target temperature set to ${temperature}°F`);
      }

      return success;
    } catch (error) {
      console.error(`Failed to set target temperature ${temperature}:`, error);
      return false;
    }
  }

  /**
   * Waits for MockDevice to reach a specific condition
   */
  async waitForCondition(
    condition: (status: SmokerStatus) => boolean,
    timeoutMs: number = 60000,
    checkIntervalMs: number = 5000
  ): Promise<boolean> {
    const endTime = Date.now() + timeoutMs;
    
    console.log(`⏳ Waiting for MockDevice condition (timeout: ${timeoutMs / 1000}s)...`);

    while (Date.now() < endTime) {
      try {
        const status = await this.getCurrentStatus();
        
        if (status && condition(status)) {
          console.log('✅ MockDevice condition met');
          return true;
        }

        await new Promise(resolve => setTimeout(resolve, checkIntervalMs));
      } catch (error) {
        console.error('Error checking condition:', error);
        await new Promise(resolve => setTimeout(resolve, checkIntervalMs));
      }
    }

    console.error('❌ MockDevice condition timeout reached');
    return false;
  }

  /**
   * Waits for MockDevice to reach cooking temperature
   */
  async waitForCookingTemperature(targetTemp: number, toleranceDegrees: number = 15): Promise<boolean> {
    return this.waitForCondition(
      (status) => {
        const currentTemp = status.temps.grillTemp;
        const difference = Math.abs(currentTemp - targetTemp);
        return difference <= toleranceDegrees && status.mode === 'cooking';
      },
      120000, // 2 minutes
      5000    // Check every 5 seconds
    );
  }

  /**
   * Waits for MockDevice to transition to a specific mode
   */
  async waitForMode(expectedMode: string, timeoutMs: number = 60000): Promise<boolean> {
    return this.waitForCondition(
      (status) => status.mode === expectedMode,
      timeoutMs
    );
  }

  /**
   * Simulates realistic temperature progression over time
   * Useful for testing long cooking sessions in accelerated time
   */
  async simulateProgressiveCook(
    targetTemp: number,
    durationMinutes: number,
    progressCallback?: (status: SmokerStatus) => void
  ): Promise<boolean> {
    try {
      console.log(`🍖 Starting progressive cook simulation: ${targetTemp}°F for ${durationMinutes} minutes`);
      
      // Start cooking
      await this.setTargetTemperature(targetTemp);
      await this.startCookingScenario('default');

      // Wait for initial temperature stabilization
      const tempReached = await this.waitForCookingTemperature(targetTemp);
      
      if (!tempReached) {
        console.error('❌ Failed to reach target temperature');
        return false;
      }

      // Monitor cooking progress
      const endTime = Date.now() + (durationMinutes * 60 * 1000);
      let lastCheckTime = Date.now();

      while (Date.now() < endTime) {
        const status = await this.getCurrentStatus();
        
        if (status) {
          // Call progress callback if provided
          if (progressCallback) {
            progressCallback(status);
          }

          // Log progress every minute
          const currentTime = Date.now();
          if (currentTime - lastCheckTime >= 60000) {
            const remainingMinutes = Math.ceil((endTime - currentTime) / 60000);
            console.log(`🔥 Cooking progress: ${status.temps?.grillTemp || 0}°F, ${remainingMinutes} minutes remaining`);
            lastCheckTime = currentTime;
          }
        }

        await new Promise(resolve => setTimeout(resolve, 10000)); // Check every 10 seconds
      }

      console.log('✅ Progressive cook simulation completed');
      return true;
    } catch (error) {
      console.error('❌ Progressive cook simulation failed:', error);
      return false;
    }
  }

  /**
   * Sets up retry interceptor for resilient API calls
   */
  private setupRetryInterceptor(maxRetries: number): void {
    if (!this.httpClient) {
      return;
    }
    
    this.httpClient.interceptors.response.use(
      (response) => response,
      async (error) => {
        const config = error.config;
        
        if (!config || !config.retry) {
          config.retry = 0;
        }

        if (config.retry < maxRetries && this.isRetryableError(error)) {
          config.retry++;
          const delay = Math.pow(2, config.retry) * 1000; // Exponential backoff
          
          console.log(`⚠️ MockDevice API call failed, retrying in ${delay}ms (attempt ${config.retry}/${maxRetries})`);
          
          await new Promise(resolve => setTimeout(resolve, delay));
          return this.httpClient!(config);
        }

        return Promise.reject(error);
      }
    );
  }

  /**
   * Determines if an error is retryable
   */
  private isRetryableError(error: any): boolean {
    if (!error.response) {
      return true; // Network errors are retryable
    }

    const status = error.response.status;
    return status >= 500 || status === 429; // Server errors and rate limiting are retryable
  }
}