/**
 * Wrapper for existing IoT Edge integration tests
 * Extends proven iot-edge/integration-tests/test-runner.sh with E2E capabilities
 */

import { exec, spawn } from 'child_process';
import { promisify } from 'util';
import path from 'path';
import fs from 'fs/promises';
import { getCurrentEnvironment } from '../config/test-environments';

const execAsync = promisify(exec);

export interface IoTEdgeTestResult {
  success: boolean;
  duration: number;
  mockDeviceHealthy: boolean;
  telemetryFlowValidated: boolean;
  containersCommunicating: boolean;
  errorMessages: string[];
  logFiles: string[];
}

export interface IoTEdgeTestOptions {
  timeout: number;
  enableLogging: boolean;
  validateAzureConnectivity: boolean;
  customEnvironment?: Record<string, string>;
}

/**
 * Executes the existing proven IoT Edge integration test suite
 * Provides foundation for E2E tests to build upon
 */
export class ExistingTestRunner {
  private readonly iotEdgeTestsPath: string;
  private readonly testRunnerScript: string;
  private readonly dockerComposeFile: string;

  constructor() {
    this.iotEdgeTestsPath = path.join(__dirname, '../../iot-edge/integration-tests');
    this.testRunnerScript = path.join(this.iotEdgeTestsPath, 'test-runner.sh');
    this.dockerComposeFile = path.join(this.iotEdgeTestsPath, 'docker-compose.integration.yml');
  }

  /**
   * Validates that existing IoT Edge integration tests are available
   */
  async validateTestsAvailable(): Promise<boolean> {
    try {
      await fs.access(this.testRunnerScript);
      await fs.access(this.dockerComposeFile);
      
      // Check if test runner script is executable
      const stats = await fs.stat(this.testRunnerScript);
      const isExecutable = (stats.mode & parseInt('111', 8)) !== 0;
      
      return isExecutable;
    } catch (error) {
      console.error('Existing IoT Edge tests validation failed:', error);
      return false;
    }
  }

  /**
   * Runs the existing IoT Edge integration test suite
   * Returns detailed results for E2E test orchestration
   */
  async runIntegrationTests(options: IoTEdgeTestOptions): Promise<IoTEdgeTestResult> {
    const startTime = Date.now();
    const result: IoTEdgeTestResult = {
      success: false,
      duration: 0,
      mockDeviceHealthy: false,
      telemetryFlowValidated: false,
      containersCommunicating: false,
      errorMessages: [],
      logFiles: []
    };

    try {
      console.log('🚀 Running existing IoT Edge integration tests...');
      
      // Prepare environment
      const testEnv = {
        ...process.env,
        ...options.customEnvironment
      };

      // Execute the proven test runner
      const { stdout, stderr } = await execAsync(
        this.testRunnerScript,
        {
          cwd: this.iotEdgeTestsPath,
          env: testEnv,
          timeout: options.timeout
        }
      );

      // Parse test results from existing test runner output
      result.success = stdout.includes('SUCCESS') || !stderr.includes('ERROR');
      result.mockDeviceHealthy = stdout.includes('mock-device') && stdout.includes('healthy');
      result.telemetryFlowValidated = stdout.includes('telemetry') && stdout.includes('validated');
      result.containersCommunicating = stdout.includes('containers') && stdout.includes('communicating');

      if (stderr) {
        result.errorMessages.push(stderr);
      }

      console.log('✅ Existing IoT Edge integration tests completed successfully');
      
    } catch (error) {
      result.success = false;
      const errorMessage = error instanceof Error ? error.message : 'Unknown test execution error';
      result.errorMessages.push(errorMessage);
      console.error('❌ Existing IoT Edge integration tests failed:', errorMessage);
    }

    result.duration = Date.now() - startTime;
    return result;
  }

  /**
   * Starts the existing Docker Compose environment for E2E tests to use
   */
  async startExistingEnvironment(): Promise<boolean> {
    try {
      console.log('🐳 Starting Docker Compose environment...');
      
      // Check if we should use the local compose file for E2E tests
      const environment = getCurrentEnvironment();
      const composeFile = environment.mode === 'full-azure' ? 
        this.dockerComposeFile : 
        path.join(__dirname, '../config/docker-compose.minimal.yml');

      console.log(`📋 Using compose file: ${path.basename(composeFile)}`);

      // Install npm dependencies for local services if using local compose
      if (environment.mode !== 'local-first') {
        console.log('📦 Installing dependencies for local services...');
        await this.installLocalServiceDependencies();
      }

      const { stdout, stderr } = await execAsync(
        `docker-compose -f ${composeFile} up -d`,
        { cwd: path.dirname(composeFile) }
      );

      if (stderr && stderr.includes('Error') && !stderr.includes('Creating') && !stderr.includes('Starting')) {
        console.error('❌ Docker Compose failed:', stderr);
        return false;
      }

      console.log('✅ Docker Compose environment started');
      
      // Wait for services to be healthy
      await this.waitForServicesReady(environment.mode);
      
      return true;
    } catch (error) {
      console.error('❌ Failed to start Docker Compose environment:', error);
      return false;
    }
  }

  private async installLocalServiceDependencies(): Promise<void> {
    const serviceDirs = [
      '../config/local-workload',
      '../config/message-collector'
    ];

    for (const serviceDir of serviceDirs) {
      const fullPath = path.join(__dirname, serviceDir);
      try {
        await execAsync('npm install', { cwd: fullPath });
        console.log(`✅ Dependencies installed for ${serviceDir}`);
      } catch (error) {
        console.warn(`⚠️ Could not install dependencies for ${serviceDir}:`, (error as any).message);
      }
    }
  }

  private async waitForServicesReady(mode: string): Promise<void> {
    if (mode === 'local-first') {
      return; // No services to wait for in local-first mode
    }

    console.log('⏳ Waiting for services to be ready...');
    
    const services = mode === 'full-azure' ? 
      ['http://localhost:3000/health'] : // Original integration MockDevice
      ['http://localhost:8080/health'];   // Our local MockDevice

    for (const serviceUrl of services) {
      let retries = 20;
      while (retries > 0) {
        try {
          // Using a simple curl check instead of HTTP client to avoid dependencies
          await execAsync(`curl -f -s ${serviceUrl}`, { timeout: 2000 });
          console.log(`✅ Service ready: ${serviceUrl}`);
          break;
        } catch {
          retries--;
          if (retries === 0) {
            console.warn(`⚠️ Service not ready after timeout: ${serviceUrl}`);
          } else if (retries % 5 === 0) {
            console.log(`⏳ Waiting for service: ${serviceUrl} (${retries} retries left)`);
            await new Promise(resolve => setTimeout(resolve, 1000));
          }
        }
      }
    }
  }

  /**
   * Stops the existing Docker Compose environment
   */
  async stopExistingEnvironment(): Promise<boolean> {
    try {
      console.log('🛑 Stopping existing Docker Compose environment...');
      
      await execAsync(
        `docker-compose -f ${this.dockerComposeFile} down`,
        { cwd: this.iotEdgeTestsPath }
      );

      console.log('✅ Existing Docker Compose environment stopped');
      return true;
    } catch (error) {
      console.error('❌ Failed to stop existing Docker Compose environment:', error);
      return false;
    }
  }

  /**
   * Monitors the existing test environment health
   */
  async monitorEnvironmentHealth(durationMs: number): Promise<boolean> {
    const endTime = Date.now() + durationMs;
    const checkInterval = 5000; // 5 seconds

    console.log(`🔍 Monitoring existing environment health for ${durationMs / 1000} seconds...`);

    while (Date.now() < endTime) {
      try {
        const { stdout } = await execAsync(
          `docker-compose -f ${this.dockerComposeFile} ps`,
          { cwd: this.iotEdgeTestsPath }
        );

        // Check if all services are running
        const isHealthy = stdout.includes('Up') && !stdout.includes('Exit');
        
        if (isHealthy) {
          console.log('✅ Environment health check passed');
          return true;
        }

        console.log('⏳ Environment not ready yet, continuing to monitor...');
        await new Promise(resolve => setTimeout(resolve, checkInterval));
        
      } catch (error) {
        console.error('⚠️ Health check failed:', error);
        await new Promise(resolve => setTimeout(resolve, checkInterval));
      }
    }

    console.error('❌ Environment health monitoring timed out');
    return false;
  }

  /**
   * Gets logs from the existing environment for debugging
   */
  async getEnvironmentLogs(): Promise<Record<string, string>> {
    const logs: Record<string, string> = {};

    try {
      const services = ['mock-device', 'telemetry-module'];
      
      for (const service of services) {
        try {
          const { stdout } = await execAsync(
            `docker-compose -f ${this.dockerComposeFile} logs ${service}`,
            { cwd: this.iotEdgeTestsPath }
          );
          logs[service] = stdout;
        } catch (error) {
          logs[service] = `Failed to get logs: ${error}`;
        }
      }
    } catch (error) {
      console.error('Failed to retrieve environment logs:', error);
    }

    return logs;
  }
}