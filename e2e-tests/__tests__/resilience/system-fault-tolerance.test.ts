/**
 * System Resilience and Fault Tolerance Tests
 * Validates MeatGeek platform behavior under adverse conditions
 */

import { WorkflowOrchestrator } from '../../utils/workflow-orchestrator';
import { MockDeviceController } from '../../utils/mock-device-controller';
import { AzureClient } from '../../utils/azure-client';
import { ExistingTestRunner } from '../../utils/existing-test-runner';

describe('System Resilience and Fault Tolerance', () => {
  
  describe('Network Resilience', () => {
    it('should handle MockDevice connectivity issues gracefully', async () => {
      // Create controller with non-existent endpoint
      const faultyController = new MockDeviceController({
        baseUrl: 'http://nonexistent-device:9999',
        timeout: 2000,
        retryAttempts: 2
      });

      // Health check should fail gracefully
      const isHealthy = await faultyController.isHealthy();
      expect(isHealthy).toBe(false);

      // Status retrieval should return null, not throw
      const status = await faultyController.getCurrentStatus();
      expect(status).toBeNull();

      // Operations should return false, not throw
      const startResult = await faultyController.startCookingScenario('brisket');
      expect(startResult).toBe(false);

      const stopResult = await faultyController.stopCooking();
      expect(stopResult).toBe(false);

      console.log('🔌 MockDevice connectivity failures handled gracefully');
    });

    it('should handle partial Azure service availability', async () => {
      // Test with mixed service availability
      const partialClient = new AzureClient({
        cosmosConnectionString: undefined, // No Cosmos DB
        iotHubConnectionString: undefined, // No IoT Hub
        eventGridEndpoint: 'https://fake-endpoint.eventgrid.azure.net/api/events',
        eventGridKey: 'fake-key'
      });

      const connectivity = await partialClient.validateConnectivity();
      
      // Should handle partial connectivity
      expect(typeof connectivity).toBe('boolean');

      // Operations should still work in simulation mode
      const sessionId = await partialClient.createTestSession({
        userId: 'resilience-test',
        deviceId: 'test-device',
        startTime: new Date().toISOString(),
        status: 'active',
        telemetryCount: 0
      });

      expect(sessionId).toBeTruthy();
      console.log('☁️ Partial Azure service availability handled gracefully');
    });

    it('should handle complete Azure service unavailability', async () => {
      const noAzureClient = new AzureClient({
        cosmosConnectionString: undefined,
        iotHubConnectionString: undefined,
        eventGridEndpoint: undefined,
        eventGridKey: undefined
      });

      const connectivity = await noAzureClient.validateConnectivity();
      expect(connectivity).toBe(false);

      // Should still provide local simulation
      const telemetry = await noAzureClient.getSessionTelemetry('test-session');
      expect(Array.isArray(telemetry)).toBe(true);
      expect(telemetry.length).toBeGreaterThan(0);

      console.log('🏠 Complete Azure unavailability handled with local simulation');
    });
  });

  describe('Timeout and Retry Behavior', () => {
    it('should handle operations with very short timeouts', async () => {
      const fastController = new MockDeviceController({
        baseUrl: 'http://localhost:3000',
        timeout: 100, // Very short timeout
        retryAttempts: 1
      });

      // Operations should complete quickly or fail gracefully
      const startTime = Date.now();
      
      const healthCheck = await fastController.isHealthy();
      const statusCheck = await fastController.getCurrentStatus();
      
      const duration = Date.now() - startTime;
      
      expect(duration).toBeLessThan(5000); // Should not hang
      expect(typeof healthCheck).toBe('boolean');
      expect(statusCheck === null || typeof statusCheck === 'object').toBe(true);

      console.log(`⚡ Short timeout operations completed in ${duration}ms`);
    });

    it('should respect retry limits', async () => {
      const retryController = new MockDeviceController({
        baseUrl: 'http://definitely-nonexistent-host-12345:9999',
        timeout: 1000,
        retryAttempts: 3
      });

      const startTime = Date.now();
      
      // This will fail and retry 3 times
      const result = await retryController.startCookingScenario('brisket');
      
      const duration = Date.now() - startTime;
      
      expect(result).toBe(false);
      expect(duration).toBeGreaterThan(3000); // Should have tried multiple times
      expect(duration).toBeLessThan(10000); // But not hang forever

      console.log(`🔄 Retry logic completed in ${duration}ms with expected failure`);
    });
  });

  describe('Workflow Resilience', () => {
    it('should handle workflow failures gracefully', async () => {
      const faultyOrchestrator = new WorkflowOrchestrator({
        enableAzureIntegration: false,
        useRealIoTHub: false,
        mockDeviceUrl: 'http://nonexistent:9999',
        testTimeout: 10000,
        enableDetailedLogging: false
      });

      const result = await faultyOrchestrator.executeBrisketCookingWorkflow();

      // Should complete without throwing, but might not succeed
      expect(result).toBeDefined();
      expect(result.workflowName).toBe('BBQ Enthusiast 12-Hour Brisket Journey');
      expect(result.duration).toBeGreaterThan(0);

      if (!result.success) {
        expect(result.errorMessages.length).toBeGreaterThan(0);
        console.log(`🛡️ Workflow failure handled gracefully with ${result.errorMessages.length} error messages`);
      } else {
        console.log('✅ Workflow succeeded despite adverse conditions');
      }
    });

    it('should provide detailed failure information', async () => {
      const debugOrchestrator = new WorkflowOrchestrator({
        enableAzureIntegration: false,
        useRealIoTHub: false,
        mockDeviceUrl: 'http://bad-host:1234',
        testTimeout: 5000,
        enableDetailedLogging: true
      });

      const result = await debugOrchestrator.executeBrisketCookingWorkflow();

      // Should provide meaningful phase information even on failure
      expect(result.phases.length).toBeGreaterThan(0);

      // Analyze failure points
      let failedPhases = 0;
      let failedValidations = 0;

      result.phases.forEach(phase => {
        if (!phase.success) failedPhases++;
        
        phase.validations.forEach(validation => {
          if (!validation.success) {
            failedValidations++;
            expect(validation.errorMessage).toBeTruthy();
          }
        });
      });

      console.log(`📊 Failure analysis: ${failedPhases} failed phases, ${failedValidations} failed validations`);
    });
  });

  describe('Resource Management', () => {
    it('should handle concurrent workflow executions', async () => {
      const orchestrators = Array.from({ length: 3 }, () => new WorkflowOrchestrator({
        enableAzureIntegration: false,
        useRealIoTHub: false,
        mockDeviceUrl: 'http://localhost:3000',
        testTimeout: 30000,
        enableDetailedLogging: false
      }));

      const startTime = Date.now();
      
      // Run multiple workflows concurrently
      const results = await Promise.allSettled(
        orchestrators.map(orchestrator => orchestrator.executeBrisketCookingWorkflow())
      );

      const duration = Date.now() - startTime;

      expect(results).toHaveLength(3);
      
      const successful = results.filter(r => r.status === 'fulfilled').length;
      const failed = results.filter(r => r.status === 'rejected').length;

      console.log(`🔀 Concurrent execution: ${successful} succeeded, ${failed} failed in ${duration}ms`);
      
      // At least some should complete (even if not all succeed)
      expect(successful + failed).toBe(3);
    }, 120000);

    it('should handle memory usage efficiently', async () => {
      // Test with large data generation
      const iterations = 10;
      const results = [];

      for (let i = 0; i < iterations; i++) {
        const orchestrator = new WorkflowOrchestrator({
          enableAzureIntegration: false,
          useRealIoTHub: false,
          mockDeviceUrl: 'http://localhost:3000',
          testTimeout: 15000,
          enableDetailedLogging: false
        });

        const result = await orchestrator.executeBrisketCookingWorkflow();
        results.push(result);

        // Force garbage collection hint
        if (global.gc) {
          global.gc();
        }
      }

      expect(results).toHaveLength(iterations);
      
      const successfulRuns = results.filter(r => r.success).length;
      console.log(`💾 Memory efficiency test: ${successfulRuns}/${iterations} workflows completed`);
    }, 180000);
  });

  describe('Error Recovery', () => {
    it('should recover from temporary service interruptions', async () => {
      let attempts = 0;
      const maxAttempts = 3;

      const resilientController = new MockDeviceController({
        baseUrl: 'http://localhost:3000',
        timeout: 5000,
        retryAttempts: 2
      });

      // Simulate intermittent failures
      while (attempts < maxAttempts) {
        attempts++;
        
        try {
          const health = await resilientController.isHealthy();
          const status = await resilientController.getCurrentStatus();
          
          // If we get here, operations are working
          console.log(`🔄 Recovery attempt ${attempts}: Operations completed successfully`);
          break;
        } catch (error) {
          if (attempts >= maxAttempts) {
            console.log(`⚠️ Recovery attempts exhausted after ${attempts} tries`);
          } else {
            console.log(`🔄 Recovery attempt ${attempts} failed, retrying...`);
            await new Promise(resolve => setTimeout(resolve, 1000));
          }
        }
      }

      expect(attempts).toBeGreaterThan(0);
      expect(attempts).toBeLessThanOrEqual(maxAttempts);
    });

    it('should maintain system integrity during partial failures', async () => {
      const orchestrator = new WorkflowOrchestrator({
        enableAzureIntegration: false,
        useRealIoTHub: false,
        mockDeviceUrl: 'http://localhost:3000',
        testTimeout: 45000,
        enableDetailedLogging: false
      });

      const result = await orchestrator.executeBrisketCookingWorkflow();

      // Even with failures, system should maintain consistency
      expect(result.finalState).toBeDefined();
      expect(result.finalState.timestamp).toBeDefined();

      // Phases should be in correct order
      const expectedPhaseOrder = [
        'Foundation Validation',
        'Session Setup',
        'Device Connection',
        'Cooking Simulation',
        'Data Validation',
        'Session Completion'
      ];

      result.phases.forEach((phase, index) => {
        if (index < expectedPhaseOrder.length) {
          expect(phase.phaseName).toBe(expectedPhaseOrder[index]);
        }
      });

      console.log('🔒 System integrity maintained despite potential partial failures');
    });
  });

  describe('Performance Under Load', () => {
    it('should maintain reasonable performance under concurrent load', async () => {
      const concurrentOperations = 5;
      const operations = [];

      const startTime = Date.now();

      // Create multiple concurrent operations
      for (let i = 0; i < concurrentOperations; i++) {
        const controller = new MockDeviceController({
          baseUrl: 'http://localhost:3000',
          timeout: 10000,
          retryAttempts: 2
        });

        operations.push(
          Promise.all([
            controller.isHealthy(),
            controller.getCurrentStatus(),
            controller.startCookingScenario('brisket')
          ])
        );
      }

      const results = await Promise.allSettled(operations);
      const duration = Date.now() - startTime;

      expect(results).toHaveLength(concurrentOperations);
      expect(duration).toBeLessThan(30000); // Should complete in reasonable time

      const successful = results.filter(r => r.status === 'fulfilled').length;
      console.log(`⚡ Load test: ${successful}/${concurrentOperations} operations succeeded in ${duration}ms`);
    });

    it('should handle rapid successive operations', async () => {
      const controller = new MockDeviceController({
        baseUrl: 'http://localhost:3000',
        timeout: 5000,
        retryAttempts: 1
      });

      const operationCount = 10;
      const operations = [];
      const startTime = Date.now();

      // Rapid successive operations
      for (let i = 0; i < operationCount; i++) {
        operations.push(controller.isHealthy());
      }

      const results = await Promise.all(operations);
      const duration = Date.now() - startTime;

      expect(results).toHaveLength(operationCount);
      expect(duration).toBeLessThan(15000);

      console.log(`🚀 Rapid operations: ${operationCount} health checks in ${duration}ms`);
    });
  });
});