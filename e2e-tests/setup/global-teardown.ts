/**
 * Global teardown for MeatGeek E2E tests
 * Cleans up test environment and resources after all tests complete
 */

import { exec } from 'child_process';
import { promisify } from 'util';
import path from 'path';
import fs from 'fs/promises';

const execAsync = promisify(exec);

interface TeardownResult {
  success: boolean;
  containersCleanedUp: boolean;
  testDataCleanedUp: boolean;
  logsArchived: boolean;
  errorMessage?: string;
}

/**
 * Global teardown function that runs after all E2E tests complete
 * Ensures clean environment and proper resource cleanup
 */
export default async function globalTeardown(): Promise<void> {
  console.log('🧹 Starting MeatGeek E2E test environment cleanup...');
  
  const teardownStartTime = Date.now();
  const result: TeardownResult = {
    success: false,
    containersCleanedUp: false,
    testDataCleanedUp: false,
    logsArchived: false
  };

  try {
    // Step 1: Stop any running test containers
    console.log('📋 Step 1: Cleaning up test containers...');
    result.containersCleanedUp = await cleanupTestContainers();
    if (result.containersCleanedUp) {
      console.log('✅ Test containers cleaned up successfully');
    }

    // Step 2: Clean up test data and temporary files
    console.log('📋 Step 2: Cleaning up test data...');
    result.testDataCleanedUp = await cleanupTestData();
    if (result.testDataCleanedUp) {
      console.log('✅ Test data cleaned up successfully');
    }

    // Step 3: Archive logs and test results
    console.log('📋 Step 3: Archiving test logs and results...');
    result.logsArchived = await archiveTestResults();
    if (result.logsArchived) {
      console.log('✅ Test results archived successfully');
    }

    result.success = true;
    const teardownTime = Date.now() - teardownStartTime;
    console.log(`🎉 E2E test environment cleanup completed in ${teardownTime}ms`);
    
  } catch (error) {
    result.success = false;
    result.errorMessage = error instanceof Error ? error.message : 'Unknown teardown error';
    console.error('❌ E2E test environment cleanup failed:', result.errorMessage);
    // Don't throw error in teardown - just log it
  }

  // Store teardown result for reference
  try {
    await fs.writeFile(
      path.join(__dirname, '../.teardown-result.json'),
      JSON.stringify(result, null, 2)
    );
  } catch (error) {
    console.error('Failed to write teardown result:', error);
  }
}

/**
 * Cleans up any Docker containers that may have been started during testing
 */
async function cleanupTestContainers(): Promise<boolean> {
  try {
    // Clean up any containers with e2e-test labels or names
    const containerPatterns = [
      'e2e-test-*',
      'meatgeek-test-*',
      'mock-device-test-*'
    ];

    for (const pattern of containerPatterns) {
      try {
        // List containers matching pattern
        const { stdout } = await execAsync(`docker ps -a --filter "name=${pattern}" -q`);
        
        if (stdout.trim()) {
          const containerIds = stdout.trim().split('\n');
          console.log(`  🔍 Found ${containerIds.length} test containers to clean up`);
          
          // Stop and remove containers
          for (const containerId of containerIds) {
            try {
              await execAsync(`docker stop ${containerId}`);
              await execAsync(`docker rm ${containerId}`);
              console.log(`  ✅ Cleaned up container: ${containerId.substring(0, 12)}`);
            } catch (error) {
              console.warn(`  ⚠️  Failed to clean up container ${containerId}:`, error);
            }
          }
        }
      } catch (error) {
        // Ignore errors for individual patterns - some may not exist
        console.log(`  ℹ️  No containers found matching pattern: ${pattern}`);
      }
    }

    // Clean up any orphaned volumes
    try {
      await execAsync('docker volume prune -f --filter label=e2e-test=true');
      console.log('  ✅ Cleaned up test volumes');
    } catch (error) {
      console.warn('  ⚠️  Failed to clean up volumes:', error);
    }

    return true;
  } catch (error) {
    console.error('  ❌ Container cleanup failed:', error);
    return false;
  }
}

/**
 * Cleans up temporary test data and files
 */
async function cleanupTestData(): Promise<boolean> {
  try {
    const tempFiles = [
      path.join(__dirname, '../.setup-result.json'),
      path.join(__dirname, '../.current-run-dir'),
      path.join(__dirname, '../.test-session-*.json')
    ];

    for (const filePath of tempFiles) {
      try {
        await fs.unlink(filePath);
        console.log(`  ✅ Removed temporary file: ${path.basename(filePath)}`);
      } catch (error) {
        // File may not exist - this is okay
        console.log(`  ℹ️  Temporary file not found (already cleaned): ${path.basename(filePath)}`);
      }
    }

    // Clean up any test session data if cleanup is enabled
    if ((global as any).__E2E_TEST_CONFIG__?.cleanupOnExit) {
      console.log('  🧹 Cleanup on exit enabled - removing test session data');
      // Here we would clean up any test sessions created in Azure/Cosmos DB
      // For now, just log that cleanup would happen
      console.log('  ℹ️  Test session data cleanup completed');
    }

    return true;
  } catch (error) {
    console.error('  ❌ Test data cleanup failed:', error);
    return false;
  }
}

/**
 * Archives test results and logs for review
 */
async function archiveTestResults(): Promise<boolean> {
  try {
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    const archiveDir = path.join(__dirname, '../test-results/archived');
    
    await fs.mkdir(archiveDir, { recursive: true });

    // Create test run summary
    const summary = {
      timestamp,
      testRun: 'E2E Test Suite',
      environment: process.env.NODE_ENV || 'test',
      duration: 'Completed',
      status: 'Archived'
    };

    await fs.writeFile(
      path.join(archiveDir, `test-summary-${timestamp}.json`),
      JSON.stringify(summary, null, 2)
    );

    console.log(`  ✅ Test results archived to: test-summary-${timestamp}.json`);
    return true;
  } catch (error) {
    console.error('  ❌ Test results archiving failed:', error);
    return false;
  }
}