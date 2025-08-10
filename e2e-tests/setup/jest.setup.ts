/**
 * Jest setup configuration for MeatGeek E2E tests
 * Configures global test environment, timeouts, and error handling
 */

import { config } from 'dotenv';
import path from 'path';

// Load environment variables from .env files
config({ path: path.join(__dirname, '../.env') });
config({ path: path.join(__dirname, '../.env.local') });

// Extend Jest timeout for E2E tests (5 minutes)
jest.setTimeout(300000);

// Global test configuration interface
interface E2ETestConfig {
  azureConnectionString?: string;
  iotHubConnectionString?: string;
  cosmosDbConnectionString?: string;
  testDeviceId: string;
  testSessionPrefix: string;
  cleanupOnExit: boolean;
}

// Extend global object
declare global {
  const __E2E_TEST_CONFIG__: E2ETestConfig;
}

// Configure global test settings
(global as any).__E2E_TEST_CONFIG__ = {
  azureConnectionString: process.env.AZURE_IOT_CONNECTION_STRING,
  iotHubConnectionString: process.env.IOT_HUB_CONNECTION_STRING,
  cosmosDbConnectionString: process.env.COSMOS_DB_CONNECTION_STRING,
  testDeviceId: process.env.TEST_DEVICE_ID || 'e2e-test-device-001',
  testSessionPrefix: process.env.TEST_SESSION_PREFIX || 'e2e-test-session',
  cleanupOnExit: process.env.CLEANUP_ON_EXIT !== 'false'
};

// Configure console output for tests
const originalConsoleError = console.error;
console.error = (...args: unknown[]): void => {
  // Filter out known warnings that don't affect tests
  const message = args[0];
  if (typeof message === 'string') {
    // Skip Azure SDK warnings that don't affect functionality
    if (message.includes('Warning: ReactDOM.render is deprecated')) {
      return;
    }
    if (message.includes('Warning: You passed a third argument')) {
      return;
    }
  }
  originalConsoleError.apply(console, args);
};

// Fail tests on unhandled promise rejections
process.on('unhandledRejection', (reason, promise) => {
  console.error('Unhandled Rejection at:', promise, 'reason:', reason);
  process.exit(1);
});

// Graceful shutdown on test completion
process.on('exit', () => {
  console.log('E2E test suite completed');
});

export {};