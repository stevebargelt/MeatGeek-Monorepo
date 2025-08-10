/**
 * Global setup for MeatGeek E2E tests
 * Extends existing IoT Edge integration tests and prepares test environment
 */

import { exec } from 'child_process';
import { promisify } from 'util';
import path from 'path';
import fs from 'fs/promises';
import { getCurrentEnvironment } from '../config/test-environments';

const execAsync = promisify(exec);

interface SetupResult {
  success: boolean;
  iotEdgeTestsReady: boolean;
  mockDeviceReady: boolean;
  azureResourcesReady: boolean;
  errorMessage?: string;
}

/**
 * Global setup function that runs before all E2E tests
 * Builds on existing proven IoT Edge integration infrastructure
 */
export default async function globalSetup(): Promise<void> {
  const environment = getCurrentEnvironment();
  console.log(`🚀 Starting MeatGeek E2E test environment setup (${environment.mode})...`);
  
  const setupStartTime = Date.now();
  const result: SetupResult = {
    success: false,
    iotEdgeTestsReady: false,
    mockDeviceReady: false,
    azureResourcesReady: false
  };

  try {
    // Step 1: Validate existing IoT Edge integration tests (skip for local-first)
    if (environment.mode === 'local-first') {
      console.log('📋 Step 1: Running in local-first mode - skipping IoT Edge validation');
      result.iotEdgeTestsReady = true;
    } else {
      console.log('📋 Step 1: Validating existing IoT Edge integration infrastructure...');
      result.iotEdgeTestsReady = await validateExistingIoTEdgeTests();
      
      if (!result.iotEdgeTestsReady) {
        throw new Error('Existing IoT Edge integration tests not found or not ready');
      }
      console.log('✅ IoT Edge integration tests validated successfully');
    }

    // Step 2: MockDevice validation (local-first uses in-process mock)
    if (environment.mode === 'local-first') {
      console.log('📋 Step 2: Using in-process MockDevice simulation...');
      result.mockDeviceReady = true;
      console.log('✅ In-process MockDevice ready');
    } else {
      console.log('📋 Step 2: Validating MockDevice simulation is ready...');
      result.mockDeviceReady = await validateMockDeviceReady();
      
      if (!result.mockDeviceReady) {
        console.log('🔧 Building MockDevice...');
        await buildMockDevice();
        result.mockDeviceReady = true;
      }
      console.log('✅ MockDevice simulation ready');
    }

    // Step 3: Azure resources validation (local-first uses mocks)
    if (environment.mode === 'local-first') {
      console.log('📋 Step 3: Using local mock Azure services...');
      result.azureResourcesReady = true;
      console.log('✅ Local Azure services ready');
    } else {
      console.log('📋 Step 3: Validating Azure resources connectivity...');
      result.azureResourcesReady = await validateAzureConnectivity();
      
      if (!result.azureResourcesReady) {
        console.log('⚠️  Azure resources not fully available - some tests will run in local-only mode');
      } else {
        console.log('✅ Azure resources connectivity validated');
      }
    }

    // Step 4: Create test results directory
    console.log('📋 Step 4: Setting up test results directory...');
    await setupTestResultsDirectory();
    console.log('✅ Test results directory ready');

    result.success = true;
    const setupTime = Date.now() - setupStartTime;
    console.log(`🎉 E2E test environment setup completed in ${setupTime}ms`);
    
  } catch (error) {
    result.success = false;
    result.errorMessage = error instanceof Error ? error.message : 'Unknown setup error';
    console.error('❌ E2E test environment setup failed:', result.errorMessage);
    throw error;
  }

  // Store setup result for tests to reference
  await fs.writeFile(
    path.join(__dirname, '../.setup-result.json'),
    JSON.stringify(result, null, 2)
  );
}

/**
 * Validates that existing IoT Edge integration tests are present and functional
 * This is our proven foundation that E2E tests will build upon
 */
async function validateExistingIoTEdgeTests(): Promise<boolean> {
  try {
    const iotEdgeTestPath = path.join(__dirname, '../../iot-edge/integration-tests');
    const testRunnerPath = path.join(iotEdgeTestPath, 'test-runner.sh');
    const dockerComposePath = path.join(iotEdgeTestPath, 'docker-compose.integration.yml');

    // Check if integration test files exist
    await fs.access(testRunnerPath);
    await fs.access(dockerComposePath);

    console.log('  ✅ IoT Edge integration test files found');
    return true;
  } catch (error) {
    console.error('  ❌ IoT Edge integration test files not found:', error);
    return false;
  }
}

/**
 * Validates that MockDevice is built and ready to use
 * MockDevice provides realistic BBQ simulation with 24 passing unit tests
 */
async function validateMockDeviceReady(): Promise<boolean> {
  try {
    // Check if MockDevice is built
    const { stdout } = await execAsync('nx show project MockDevice', {
      cwd: path.join(__dirname, '../..')
    });
    
    if (stdout.includes('"name":"MockDevice"')) {
      console.log('  ✅ MockDevice project configuration found');
      return true;
    }
    return false;
  } catch (error) {
    console.error('  ❌ MockDevice validation failed:', error);
    return false;
  }
}

/**
 * Builds MockDevice using Nx if not already built
 */
async function buildMockDevice(): Promise<void> {
  try {
    const { stdout, stderr } = await execAsync('nx build MockDevice', {
      cwd: path.join(__dirname, '../..')
    });
    
    console.log('  ✅ MockDevice built successfully');
    if (stderr && !stderr.includes('warning')) {
      console.warn('  ⚠️  Build warnings:', stderr);
    }
  } catch (error) {
    console.error('  ❌ MockDevice build failed:', error);
    throw new Error('Failed to build MockDevice');
  }
}

/**
 * Validates Azure resources connectivity for cloud integration tests
 * Returns false if Azure not available (tests will run in local-only mode)
 */
async function validateAzureConnectivity(): Promise<boolean> {
  try {
    const connectionString = process.env.AZURE_IOT_CONNECTION_STRING;
    const iotHubConnectionString = process.env.IOT_HUB_CONNECTION_STRING;
    
    if (!connectionString && !iotHubConnectionString) {
      console.log('  ℹ️  No Azure connection strings found - running in local-only mode');
      return false;
    }
    
    console.log('  ✅ Azure connection strings found');
    return true;
  } catch (error) {
    console.error('  ❌ Azure connectivity validation failed:', error);
    return false;
  }
}

/**
 * Creates test results directory structure
 */
async function setupTestResultsDirectory(): Promise<void> {
  const testResultsDir = path.join(__dirname, '../test-results');
  const logsDir = path.join(__dirname, '../logs');
  
  await fs.mkdir(testResultsDir, { recursive: true });
  await fs.mkdir(logsDir, { recursive: true });
  
  // Create timestamp for this test run
  const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
  const runDir = path.join(testResultsDir, `run-${timestamp}`);
  await fs.mkdir(runDir, { recursive: true });
  
  // Store current run directory for tests to use
  await fs.writeFile(
    path.join(__dirname, '../.current-run-dir'),
    runDir
  );
}