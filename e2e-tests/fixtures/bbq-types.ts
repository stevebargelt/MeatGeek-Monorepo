/**
 * TypeScript interfaces for BBQ cooking scenarios and telemetry
 */

export interface SmokerStatus {
  temps: {
    grillTemp: number;
    probe1Temp: number;
    probe2Temp?: number;
    probe3Temp?: number;
    probe4Temp?: number;
    ambientTemp?: number;
  };
  setPoint: number;
  mode: 'startup' | 'cooking' | 'idle' | 'cooling' | 'error' | 'shutdown';
  fanSpeed?: number;
  augerRate?: number;
  timestamp?: string;
  pelletLevel?: number;
  smokerId?: string;
  sessionId?: string;
}

export interface CookingPhase {
  name: string;
  durationMinutes: number;
  targetGrillTemp: number;
  probeTargetRange: [number, number];
  mode: string;
}

export interface CookingScenario {
  name: string;
  description: string;
  targetTemp: number;
  cookingDurationHours: number;
  meatType: string;
  weight: number;
  phases: CookingPhase[];
}

export interface TemperatureProfile {
  initialTemp: number;
  targetInternalTemp: number;
  stallTemp: number | null;
  stallDurationMinutes: number;
  temperatureCurve: 'linear' | 'logarithmic' | 'exponential';
}

export interface BBQScenariosData {
  scenarios: Record<string, CookingScenario>;
  temperatureProfiles: Record<string, TemperatureProfile>;
}

// Default status for testing
export const defaultSmokerStatus: SmokerStatus = {
  temps: {
    grillTemp: 225,
    probe1Temp: 165,
    probe2Temp: 160,
    ambientTemp: 75
  },
  setPoint: 225,
  mode: 'cooking',
  fanSpeed: 45,
  augerRate: 35,
  timestamp: new Date().toISOString(),
  pelletLevel: 75
};

// Helper function to create test smoker status
export function createTestSmokerStatus(overrides: Partial<SmokerStatus> = {}): SmokerStatus {
  return {
    ...defaultSmokerStatus,
    ...overrides,
    temps: {
      ...defaultSmokerStatus.temps,
      ...overrides.temps
    }
  };
}