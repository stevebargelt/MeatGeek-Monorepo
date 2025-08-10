/**
 * Unit tests for TestDataFactory
 */

import { TestDataFactory, SessionTestData, TelemetryTestData } from '../../utils/test-data-factory';

describe('TestDataFactory', () => {
  let factory: TestDataFactory;

  beforeEach(() => {
    factory = new TestDataFactory();
  });

  describe('Session Data Creation', () => {
    describe('createBrisketSession', () => {
      it('should create valid brisket session data', () => {
        const session = factory.createBrisketSession();

        expect(session).toMatchObject({
          meatType: 'Brisket',
          targetTemp: 225,
          cookingMethod: 'Smoking',
          estimatedDurationHours: 12
        });

        expect(session.userId).toMatch(/^e2e-test-user-\d{3}$/);
        expect(session.deviceId).toMatch(/^e2e-test-device-\d{3}$/);
        expect(new Date(session.startTime)).toBeInstanceOf(Date);
        expect(session.notes).toContain('E2E test');
      });

      it('should generate unique device and user IDs for each session', () => {
        const session1 = factory.createBrisketSession();
        const session2 = factory.createBrisketSession();

        expect(session1.deviceId).not.toBe(session2.deviceId);
        expect(session1.userId).not.toBe(session2.userId);
      });
    });

    describe('createPorkShoulderSession', () => {
      it('should create valid pork shoulder session data', () => {
        const session = factory.createPorkShoulderSession();

        expect(session).toMatchObject({
          meatType: 'Pork Shoulder',
          targetTemp: 250,
          cookingMethod: 'Smoking',
          estimatedDurationHours: 8
        });

        expect(session.userId).toMatch(/^e2e-test-user-\d{3}$/);
        expect(session.deviceId).toMatch(/^e2e-test-device-\d{3}$/);
      });
    });

    describe('createRibsSession', () => {
      it('should create valid ribs session data', () => {
        const session = factory.createRibsSession();

        expect(session).toMatchObject({
          meatType: 'Baby Back Ribs',
          targetTemp: 275,
          cookingMethod: 'Smoking',
          estimatedDurationHours: 4
        });

        expect(session.notes).toContain('3-2-1 method');
      });
    });

    describe('createChickenSession', () => {
      it('should create valid chicken session data', () => {
        const session = factory.createChickenSession();

        expect(session).toMatchObject({
          meatType: 'Whole Chicken',
          targetTemp: 350,
          cookingMethod: 'Roasting',
          estimatedDurationHours: 2
        });

        expect(session.notes).toContain('spatchcocked');
      });
    });
  });

  describe('Telemetry Data Generation', () => {
    describe('generateBrisketTelemetryProgression', () => {
      it('should generate realistic brisket telemetry progression', () => {
        const sessionId = 'test-session-123';
        const deviceId = 'test-device-456';
        const telemetryData = factory.generateBrisketTelemetryProgression(sessionId, deviceId, 60); // Every hour

        expect(telemetryData).toHaveLength(13); // 0 to 12 hours (13 data points)
        
        // Check first data point (startup)
        const firstPoint = telemetryData[0];
        expect(firstPoint.sessionId).toBe(sessionId);
        expect(firstPoint.deviceId).toBe(deviceId);
        expect(firstPoint.setPoint).toBe(225);
        expect(firstPoint.mode).toBe('startup');
        
        // Check last data point (should be cooked)
        const lastPoint = telemetryData[telemetryData.length - 1];
        expect(lastPoint.temps.probe1Temp).toBeGreaterThan(200);
        expect(lastPoint.mode).toBe('cooking');
        
        // Validate temperature progression (should generally increase)
        const midPoint = telemetryData[6]; // 6 hours in
        expect(midPoint.temps.probe1Temp).toBeGreaterThan(firstPoint.temps.probe1Temp);
      });

      it('should include realistic temperature variation', () => {
        const telemetryData = factory.generateBrisketTelemetryProgression('session', 'device', 120); // Every 2 hours

        telemetryData.forEach(point => {
          // Grill temp should be around 225°F with reasonable variation
          expect(point.temps.grillTemp).toBeGreaterThan(175); // Allow for startup temperatures
          expect(point.temps.grillTemp).toBeLessThan(250);
          
          // Should have probe temperatures
          expect(point.temps.probe1Temp).toBeGreaterThan(0);
          expect(point.temps.probe2Temp).toBeDefined();
          
          // Should have valid timestamps
          expect(new Date(point.timestamp)).toBeInstanceOf(Date);
        });
      });
    });

    describe('generateAcceleratedBrisketTelemetry', () => {
      it('should generate accelerated telemetry data', () => {
        const sessionId = 'accel-session';
        const deviceId = 'accel-device';
        const telemetryData = factory.generateAcceleratedBrisketTelemetry(sessionId, deviceId, 2); // 2 minutes

        expect(telemetryData.length).toBeGreaterThan(0);
        
        // Should map accelerated time to realistic cooking progression
        const firstPoint = telemetryData[0];
        const lastPoint = telemetryData[telemetryData.length - 1];
        
        expect(firstPoint.mode).toBe('startup');
        expect(lastPoint.temps.probe1Temp).toBeGreaterThan(firstPoint.temps.probe1Temp);
        
        // All points should have the same session and device ID
        telemetryData.forEach(point => {
          expect(point.sessionId).toBe(sessionId);
          expect(point.deviceId).toBe(deviceId);
          expect(point.setPoint).toBe(225);
        });
      });

      it('should have realistic time intervals', () => {
        const telemetryData = factory.generateAcceleratedBrisketTelemetry('session', 'device', 1);
        
        if (telemetryData.length > 1) {
          const time1 = new Date(telemetryData[0].timestamp).getTime();
          const time2 = new Date(telemetryData[1].timestamp).getTime();
          const intervalMs = time2 - time1;
          
          expect(intervalMs).toBe(30000); // Should be 30 second intervals
        }
      });
    });
  });

  describe('Cooking Progress Data', () => {
    describe('createBrisketProgressCheckpoints', () => {
      it('should create realistic brisket progress checkpoints', () => {
        const checkpoints = factory.createBrisketProgressCheckpoints();

        expect(checkpoints).toHaveLength(7);
        
        // Validate checkpoint progression
        expect(checkpoints[0].phase).toBe('Startup');
        expect(checkpoints[0].timeElapsed).toBe(0);
        
        expect(checkpoints[3].phase).toBe('The Stall');
        expect(checkpoints[3].timeElapsed).toBe(6 * 60); // 6 hours
        expect(checkpoints[3].expectedTemp).toBeLessThan(170);
        
        expect(checkpoints[6].phase).toBe('Done');
        expect(checkpoints[6].timeElapsed).toBe(12 * 60); // 12 hours
        expect(checkpoints[6].expectedTemp).toBeGreaterThan(200);
        
        // All checkpoints should be on target for this ideal scenario
        checkpoints.forEach(checkpoint => {
          expect(checkpoint.isOnTarget).toBe(true);
        });
      });

      it('should show realistic temperature stall', () => {
        const checkpoints = factory.createBrisketProgressCheckpoints();
        
        // Find the stall checkpoint
        const stallCheckpoint = checkpoints.find(cp => cp.phase === 'The Stall');
        const preStallCheckpoint = checkpoints.find(cp => cp.phase === 'Stall Beginning');
        
        expect(stallCheckpoint).toBeDefined();
        expect(preStallCheckpoint).toBeDefined();
        
        if (stallCheckpoint && preStallCheckpoint) {
          // Temperature should plateau during stall
          const tempDifference = stallCheckpoint.expectedTemp - preStallCheckpoint.expectedTemp;
          expect(tempDifference).toBeLessThan(10); // Minimal increase during stall
        }
      });
    });
  });

  describe('Data Validation', () => {
    it('should generate consistent device IDs across multiple calls', () => {
      // Device counter should increment consistently
      const factory1 = new TestDataFactory();
      const factory2 = new TestDataFactory();
      
      const session1a = factory1.createBrisketSession();
      const session1b = factory1.createBrisketSession();
      const session2a = factory2.createBrisketSession();
      
      expect(session1a.deviceId).toBe('e2e-test-device-001');
      expect(session1b.deviceId).toBe('e2e-test-device-002');
      expect(session2a.deviceId).toBe('e2e-test-device-001'); // New factory, counter resets
    });

    it('should generate valid ISO timestamps', () => {
      const session = factory.createBrisketSession();
      const telemetry = factory.generateAcceleratedBrisketTelemetry('session', 'device', 1);
      
      expect(() => new Date(session.startTime)).not.toThrow();
      telemetry.forEach(point => {
        expect(() => new Date(point.timestamp)).not.toThrow();
        expect(new Date(point.timestamp).toISOString()).toBe(point.timestamp);
      });
    });

    it('should generate realistic cooking temperatures', () => {
      const telemetry = factory.generateBrisketTelemetryProgression('session', 'device', 30);
      
      telemetry.forEach(point => {
        // Grill temp should be in reasonable range
        expect(point.temps.grillTemp).toBeGreaterThan(100);
        expect(point.temps.grillTemp).toBeLessThan(500);
        
        // Probe temp should not exceed grill temp by much
        expect(point.temps.probe1Temp).toBeLessThan(point.temps.grillTemp + 20);
        
        // Probe temp should be reasonable
        expect(point.temps.probe1Temp).toBeGreaterThan(50);
        expect(point.temps.probe1Temp).toBeLessThan(250);
      });
    });
  });
});