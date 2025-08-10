/**
 * In-process mock device for local-first testing
 * Simulates the MockDevice API without requiring external services
 */

import { EventEmitter } from 'events';

export interface MockSmokerStatus {
  deviceId: string;
  sessionId?: string;
  timestamp: string;
  batteryLevel: number;
  smokerId: string;
  type: string;
  ttl?: number;
  probes: Array<{
    probeId: number;
    name: string;
    currentTemp: number;
    targetTemp?: number;
    alarmLow?: number;
    alarmHigh?: number;
  }>;
  cookingSession?: {
    sessionId: string;
    startTime: string;
    targetTemp: number;
    meatType: string;
    cookingMode: string;
  };
}

export interface SimulationScenario {
  name: string;
  duration: number; // minutes
  meatType: string;
  targetTemp: number;
  phases: Array<{
    name: string;
    durationMinutes: number;
    tempRange: { min: number; max: number };
    targetTemp?: number;
  }>;
}

export class InProcessMockDevice extends EventEmitter {
  private deviceId: string;
  private sessionId?: string;
  private isRunning: boolean = false;
  private currentScenario?: SimulationScenario;
  private simulationStartTime?: Date;
  private currentStatus: MockSmokerStatus;
  private simulationTimer?: NodeJS.Timeout;

  constructor(deviceId: string = 'mock-device-001') {
    super();
    this.deviceId = deviceId;
    this.currentStatus = this.createInitialStatus();
  }

  private createInitialStatus(): MockSmokerStatus {
    return {
      deviceId: this.deviceId,
      timestamp: new Date().toISOString(),
      batteryLevel: 85,
      smokerId: this.deviceId,
      type: 'status',
      ttl: -1,
      probes: [
        {
          probeId: 1,
          name: 'Grill Temp',
          currentTemp: 72,
          targetTemp: 225,
          alarmLow: 200,
          alarmHigh: 275
        },
        {
          probeId: 2,
          name: 'Meat Probe 1',
          currentTemp: 68,
          targetTemp: 203,
          alarmLow: 32,
          alarmHigh: 212
        }
      ]
    };
  }

  // Mock API endpoints
  async getStatus(): Promise<MockSmokerStatus> {
    await this.simulateDelay(50);
    return { ...this.currentStatus };
  }

  async isHealthy(): Promise<boolean> {
    await this.simulateDelay(25);
    return true;
  }

  async startCookingScenario(scenarioName: string): Promise<boolean> {
    await this.simulateDelay(100);
    
    const scenario = this.getScenarioByName(scenarioName);
    if (!scenario) {
      throw new Error(`Unknown scenario: ${scenarioName}`);
    }

    this.currentScenario = scenario;
    this.simulationStartTime = new Date();
    this.sessionId = `session-${Date.now()}`;
    this.isRunning = true;

    this.currentStatus.sessionId = this.sessionId;
    this.currentStatus.cookingSession = {
      sessionId: this.sessionId,
      startTime: this.simulationStartTime.toISOString(),
      targetTemp: scenario.targetTemp,
      meatType: scenario.meatType,
      cookingMode: 'low-and-slow'
    };

    this.startSimulation();
    return true;
  }

  async stopCookingScenario(): Promise<boolean> {
    await this.simulateDelay(50);
    
    this.isRunning = false;
    this.currentScenario = undefined;
    this.sessionId = undefined;
    
    if (this.simulationTimer) {
      clearInterval(this.simulationTimer);
      this.simulationTimer = undefined;
    }

    this.currentStatus.sessionId = undefined;
    this.currentStatus.cookingSession = undefined;
    this.currentStatus = this.createInitialStatus();

    return true;
  }

  async getCurrentSession(): Promise<{ sessionId: string } | null> {
    await this.simulateDelay(25);
    return this.sessionId ? { sessionId: this.sessionId } : null;
  }

  async getSimulationProgress(): Promise<{ phase: string; elapsedMinutes: number; totalMinutes: number } | null> {
    await this.simulateDelay(25);
    
    if (!this.currentScenario || !this.simulationStartTime) {
      return null;
    }

    const elapsed = (Date.now() - this.simulationStartTime.getTime()) / (1000 * 60);
    const currentPhase = this.getCurrentPhase(elapsed);
    
    return {
      phase: currentPhase?.name || 'unknown',
      elapsedMinutes: Math.round(elapsed),
      totalMinutes: this.currentScenario.duration
    };
  }

  // Simulation logic
  private startSimulation(): void {
    if (!this.currentScenario) return;

    // Update status every 2 seconds (accelerated simulation)
    this.simulationTimer = setInterval(() => {
      this.updateSimulationStatus();
    }, 2000);
  }

  private updateSimulationStatus(): void {
    if (!this.currentScenario || !this.simulationStartTime || !this.isRunning) {
      return;
    }

    const elapsedMinutes = (Date.now() - this.simulationStartTime.getTime()) / (1000 * 60);
    const currentPhase = this.getCurrentPhase(elapsedMinutes);

    if (!currentPhase) {
      // Simulation complete
      this.stopCookingScenario();
      this.emit('simulationComplete', { sessionId: this.sessionId });
      return;
    }

    // Update temperatures based on current phase
    this.currentStatus.timestamp = new Date().toISOString();
    
    // Simulate grill temperature (probe 1)
    const grillTemp = this.simulateTemperature(
      this.currentStatus.probes[0].currentTemp,
      currentPhase.targetTemp || currentPhase.tempRange.min + 
        (currentPhase.tempRange.max - currentPhase.tempRange.min) / 2,
      5 // variance
    );
    this.currentStatus.probes[0].currentTemp = grillTemp;

    // Simulate meat temperature (probe 2) - slower rise
    const meatTargetTemp = Math.min(
      this.currentScenario.targetTemp,
      grillTemp - 20 // Meat temp lags behind grill temp
    );
    const meatTemp = this.simulateTemperature(
      this.currentStatus.probes[1].currentTemp,
      meatTargetTemp,
      2 // less variance for meat
    );
    this.currentStatus.probes[1].currentTemp = meatTemp;

    // Simulate battery drain
    this.currentStatus.batteryLevel = Math.max(
      20,
      this.currentStatus.batteryLevel - 0.01
    );

    this.emit('statusUpdate', { ...this.currentStatus });
  }

  private getCurrentPhase(elapsedMinutes: number) {
    if (!this.currentScenario) return null;

    let totalTime = 0;
    for (const phase of this.currentScenario.phases) {
      totalTime += phase.durationMinutes;
      if (elapsedMinutes <= totalTime) {
        return phase;
      }
    }
    return null; // Simulation complete
  }

  private simulateTemperature(current: number, target: number, variance: number): number {
    const diff = target - current;
    const change = diff * 0.1 + (Math.random() - 0.5) * variance;
    return Math.round((current + change) * 10) / 10;
  }

  private async simulateDelay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  private getScenarioByName(name: string): SimulationScenario | undefined {
    const scenarios: Record<string, SimulationScenario> = {
      'brisket-12hr': {
        name: 'Texas Brisket (12 Hour Cook)',
        duration: 12 * 60, // 12 hours in minutes
        meatType: 'brisket',
        targetTemp: 203,
        phases: [
          {
            name: 'Initial Heat',
            durationMinutes: 30,
            tempRange: { min: 150, max: 225 },
            targetTemp: 225
          },
          {
            name: 'Low and Slow',
            durationMinutes: 8 * 60, // 8 hours
            tempRange: { min: 220, max: 230 },
            targetTemp: 225
          },
          {
            name: 'The Stall',
            durationMinutes: 2 * 60, // 2 hours
            tempRange: { min: 225, max: 235 },
            targetTemp: 230
          },
          {
            name: 'Power Through',
            durationMinutes: 90, // 1.5 hours
            tempRange: { min: 240, max: 250 },
            targetTemp: 245
          }
        ]
      },
      'ribs-6hr': {
        name: 'Baby Back Ribs (6 Hour Cook)',
        duration: 6 * 60,
        meatType: 'ribs',
        targetTemp: 195,
        phases: [
          {
            name: 'Initial Smoke',
            durationMinutes: 3 * 60,
            tempRange: { min: 220, max: 230 },
            targetTemp: 225
          },
          {
            name: 'Wrap Phase',
            durationMinutes: 2 * 60,
            tempRange: { min: 240, max: 250 },
            targetTemp: 245
          },
          {
            name: 'Final Glaze',
            durationMinutes: 60,
            tempRange: { min: 250, max: 275 },
            targetTemp: 265
          }
        ]
      },
      'quick-test': {
        name: 'Quick Test Scenario (5 minutes)',
        duration: 5,
        meatType: 'test',
        targetTemp: 150,
        phases: [
          {
            name: 'Heat Up',
            durationMinutes: 2,
            tempRange: { min: 70, max: 150 },
            targetTemp: 150
          },
          {
            name: 'Hold Temp',
            durationMinutes: 3,
            tempRange: { min: 145, max: 155 },
            targetTemp: 150
          }
        ]
      }
    };

    return scenarios[name];
  }

  // Cleanup
  destroy(): void {
    this.isRunning = false;
    if (this.simulationTimer) {
      clearInterval(this.simulationTimer);
    }
    this.removeAllListeners();
  }
}