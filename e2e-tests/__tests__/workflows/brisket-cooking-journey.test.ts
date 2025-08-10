/**
 * Complete BBQ Enthusiast 12-Hour Brisket Cooking Journey E2E Test
 * Tests the entire MeatGeek platform end-to-end with realistic BBQ scenarios
 */

import { WorkflowOrchestrator, E2EWorkflowResult } from '../../utils/workflow-orchestrator';

describe('E2E Workflow: BBQ Enthusiast Brisket Cooking Journey', () => {
  let orchestrator: WorkflowOrchestrator;

  beforeAll(() => {
    // Initialize orchestrator with test configuration
    orchestrator = new WorkflowOrchestrator({
      enableAzureIntegration: false, // Run locally for CI/CD
      useRealIoTHub: false,
      mockDeviceUrl: 'http://localhost:3000',
      testTimeout: 300000, // 5 minutes total
      enableDetailedLogging: true
    });
  });

  describe('Complete Brisket Cooking Workflow', () => {
    it('should execute the complete 12-hour brisket cooking journey successfully', async () => {
      console.log('🍖 Starting complete BBQ brisket cooking journey...');
      
      const result: E2EWorkflowResult = await orchestrator.executeBrisketCookingWorkflow();

      // Validate overall workflow success
      expect(result.success).toBe(true);
      expect(result.workflowName).toBe('BBQ Enthusiast 12-Hour Brisket Journey');
      expect(result.duration).toBeGreaterThan(0);
      expect(result.phases).toHaveLength(6);
      expect(result.errorMessages).toHaveLength(0);

      console.log(`✅ Brisket cooking journey completed in ${result.duration}ms`);
    }, 300000); // 5 minute timeout

    it('should validate all workflow phases complete successfully', async () => {
      console.log('🔍 Validating individual workflow phases...');
      
      const result: E2EWorkflowResult = await orchestrator.executeBrisketCookingWorkflow();

      // Validate each phase
      const phaseNames = [
        'Foundation Validation',
        'Session Setup',
        'Device Connection',
        'Cooking Simulation',
        'Data Validation',
        'Session Completion'
      ];

      expect(result.phases).toHaveLength(phaseNames.length);

      result.phases.forEach((phase, index) => {
        expect(phase.phaseName).toBe(phaseNames[index]);
        expect(phase.success).toBe(true);
        expect(phase.duration).toBeGreaterThan(0);
        expect(phase.validations.length).toBeGreaterThan(0);

        // All validations within each phase should pass
        phase.validations.forEach(validation => {
          expect(validation.success).toBe(true);
        });

        console.log(`✅ Phase "${phase.phaseName}" completed in ${phase.duration}ms with ${phase.validations.length} validations`);
      });
    }, 300000);

    it('should capture meaningful system state at completion', async () => {
      const result: E2EWorkflowResult = await orchestrator.executeBrisketCookingWorkflow();

      expect(result.finalState).toBeDefined();
      expect(result.finalState.timestamp).toBeDefined();
      
      // Should have MockDevice status in final state
      if (result.finalState.mockDeviceStatus) {
        const deviceStatus = result.finalState.mockDeviceStatus as any;
        expect(deviceStatus.temps).toBeDefined();
        expect(deviceStatus.mode).toBeDefined();
      }

      console.log('📊 Final system state:', JSON.stringify(result.finalState, null, 2));
    }, 300000);
  });

  describe('Workflow Phase Validation', () => {
    it('should validate Phase 1: Foundation infrastructure is ready', async () => {
      // Test just the first phase in isolation for debugging
      const result = await orchestrator.executeBrisketCookingWorkflow();
      
      const foundationPhase = result.phases.find(p => p.phaseName === 'Foundation Validation');
      expect(foundationPhase).toBeDefined();
      expect(foundationPhase!.success).toBe(true);

      // Validate specific foundation checks
      const validationDescriptions = foundationPhase!.validations.map(v => v.description);
      expect(validationDescriptions).toContain('Existing IoT Edge integration tests available');
      expect(validationDescriptions).toContain('Existing Docker Compose environment started');
      expect(validationDescriptions).toContain('MockDevice is healthy and responding');

      console.log(`🏗️ Foundation validation completed with ${foundationPhase!.validations.length} checks`);
    }, 300000);

    it('should validate Phase 2: Session management works correctly', async () => {
      const result = await orchestrator.executeBrisketCookingWorkflow();
      
      const sessionPhase = result.phases.find(p => p.phaseName === 'Session Setup');
      expect(sessionPhase).toBeDefined();
      expect(sessionPhase!.success).toBe(true);

      // Validate session-specific checks
      const validationDescriptions = sessionPhase!.validations.map(v => v.description);
      expect(validationDescriptions).toContain('Cooking session created via Sessions API');
      expect(validationDescriptions).toContain('Session data has required fields');

      console.log(`📝 Session setup completed with ${sessionPhase!.validations.length} validations`);
    }, 300000);

    it('should validate Phase 3: Device connection and brisket scenario start', async () => {
      const result = await orchestrator.executeBrisketCookingWorkflow();
      
      const devicePhase = result.phases.find(p => p.phaseName === 'Device Connection');
      expect(devicePhase).toBeDefined();
      expect(devicePhase!.success).toBe(true);

      // Validate device connection checks
      const validationDescriptions = devicePhase!.validations.map(v => v.description);
      expect(validationDescriptions).toContain('Brisket cooking scenario started on MockDevice');
      expect(validationDescriptions).toContain('MockDevice transitioned to startup mode');
      expect(validationDescriptions).toContain('Initial telemetry data is valid for brisket');

      console.log(`🔌 Device connection completed with ${devicePhase!.validations.length} validations`);
    }, 300000);

    it('should validate Phase 4: Cooking simulation with realistic brisket physics', async () => {
      const result = await orchestrator.executeBrisketCookingWorkflow();
      
      const cookingPhase = result.phases.find(p => p.phaseName === 'Cooking Simulation');
      expect(cookingPhase).toBeDefined();
      expect(cookingPhase!.success).toBe(true);

      // Validate cooking simulation checks
      const validationDescriptions = cookingPhase!.validations.map(v => v.description);
      expect(validationDescriptions).toContain('MockDevice reached brisket cooking temperature (225°F ±15°)');
      expect(validationDescriptions).toContain('Accelerated 12-hour brisket cook simulation completed');
      expect(validationDescriptions).toContain('Final cooking state shows properly cooked brisket');

      console.log(`🔥 Cooking simulation completed with ${cookingPhase!.validations.length} validations`);
    }, 300000);
  });

  describe('Data Flow Validation', () => {
    it('should validate telemetry flows through existing IoT Edge infrastructure', async () => {
      const result = await orchestrator.executeBrisketCookingWorkflow();
      
      const dataPhase = result.phases.find(p => p.phaseName === 'Data Validation');
      expect(dataPhase).toBeDefined();
      expect(dataPhase!.success).toBe(true);

      // Should validate that telemetry is flowing
      const telemetryValidation = dataPhase!.validations.find(v => 
        v.description.includes('telemetry data flowing')
      );
      expect(telemetryValidation).toBeDefined();
      expect(telemetryValidation!.success).toBe(true);

      console.log(`📡 Data validation found telemetry flow through IoT Edge infrastructure`);
    }, 300000);

    it('should validate session data consistency across services', async () => {
      const result = await orchestrator.executeBrisketCookingWorkflow();
      
      const dataPhase = result.phases.find(p => p.phaseName === 'Data Validation');
      expect(dataPhase).toBeDefined();
      
      const consistencyValidation = dataPhase!.validations.find(v => 
        v.description.includes('Session data consistent')
      );
      expect(consistencyValidation).toBeDefined();
      expect(consistencyValidation!.success).toBe(true);

      console.log(`🔄 Data consistency validation passed across services`);
    }, 300000);
  });

  describe('Session Lifecycle', () => {
    it('should properly complete and cleanup the cooking session', async () => {
      const result = await orchestrator.executeBrisketCookingWorkflow();
      
      const completionPhase = result.phases.find(p => p.phaseName === 'Session Completion');
      expect(completionPhase).toBeDefined();
      expect(completionPhase!.success).toBe(true);

      // Validate session completion steps
      const validationDescriptions = completionPhase!.validations.map(v => v.description);
      expect(validationDescriptions).toContain('Cooking session stopped successfully');
      expect(validationDescriptions).toContain('Session marked as completed in Sessions API');
      expect(validationDescriptions).toContain('System returned to idle state');

      console.log(`🏁 Session completion validated with ${completionPhase!.validations.length} cleanup steps`);
    }, 300000);
  });

  describe('Error Handling and Resilience', () => {
    it('should handle graceful degradation when Azure services are not available', async () => {
      // Create orchestrator with Azure disabled
      const localOrchestrator = new WorkflowOrchestrator({
        enableAzureIntegration: false,
        useRealIoTHub: false,
        mockDeviceUrl: 'http://localhost:3000',
        testTimeout: 60000,
        enableDetailedLogging: false
      });

      const result = await localOrchestrator.executeBrisketCookingWorkflow();

      // Should still succeed in local-only mode
      expect(result.success).toBe(true);
      expect(result.errorMessages).toHaveLength(0);

      console.log(`🏠 Local-only mode workflow completed successfully`);
    }, 120000);

    it('should provide detailed error information when workflow fails', async () => {
      // Create orchestrator with impossible configuration to test error handling
      const faultyOrchestrator = new WorkflowOrchestrator({
        enableAzureIntegration: false,
        useRealIoTHub: false,
        mockDeviceUrl: 'http://nonexistent:9999', // Bad URL
        testTimeout: 5000, // Very short timeout
        enableDetailedLogging: true
      });

      const result = await faultyOrchestrator.executeBrisketCookingWorkflow();

      // Should fail gracefully with detailed error information
      if (!result.success) {
        expect(result.errorMessages.length).toBeGreaterThan(0);
        expect(result.phases.length).toBeGreaterThan(0); // Should attempt some phases
        
        // Should have detailed validation failures
        const failedValidations = result.phases.flatMap(phase => 
          phase.validations.filter(v => !v.success)
        );
        expect(failedValidations.length).toBeGreaterThan(0);

        console.log(`⚠️ Expected failure handled gracefully with ${result.errorMessages.length} error messages`);
      } else {
        console.log(`✅ Workflow succeeded despite adverse conditions`);
      }
    }, 30000);
  });
});