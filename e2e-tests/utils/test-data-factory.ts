/**
 * Test Data Factory for E2E testing
 * Generates realistic test data for BBQ cooking sessions and telemetry
 */

export interface SessionTestData {
  userId: string;
  deviceId: string;
  startTime: string;
  meatType: string;
  targetTemp: number;
  cookingMethod: string;
  estimatedDurationHours: number;
  notes?: string;
}

export interface TelemetryTestData {
  deviceId: string;
  sessionId: string;
  timestamp: string;
  temps: {
    grillTemp: number;
    probe1Temp: number;
    probe2Temp?: number;
    probe3Temp?: number;
    probe4Temp?: number;
  };
  setPoint: number;
  mode: 'startup' | 'cooking' | 'idle' | 'cooling' | 'error';
  fanSpeed?: number;
  augerRate?: number;
}

export interface CookingProgressData {
  phase: string;
  timeElapsed: number;
  expectedTemp: number;
  actualTemp: number;
  isOnTarget: boolean;
}

/**
 * Generates realistic test data for BBQ cooking scenarios
 * Based on actual BBQ cooking physics and timelines
 */
export class TestDataFactory {
  private readonly deviceIdPrefix = 'e2e-test-device';
  private readonly userIdPrefix = 'e2e-test-user';
  private deviceCounter = 1;
  private userCounter = 1;

  /**
   * Creates test data for a 12-hour brisket cooking session
   */
  createBrisketSession(): SessionTestData {
    const deviceId = this.generateDeviceId();
    const userId = this.generateUserId();
    const startTime = new Date().toISOString();

    return {
      userId,
      deviceId,
      startTime,
      meatType: 'Brisket',
      targetTemp: 225, // Low and slow for brisket
      cookingMethod: 'Smoking',
      estimatedDurationHours: 12,
      notes: 'E2E test brisket cook - 12-14lb packer brisket, salt & pepper rub'
    };
  }

  /**
   * Creates test data for a pork shoulder session
   */
  createPorkShoulderSession(): SessionTestData {
    return {
      userId: this.generateUserId(),
      deviceId: this.generateDeviceId(),
      startTime: new Date().toISOString(),
      meatType: 'Pork Shoulder',
      targetTemp: 250,
      cookingMethod: 'Smoking',
      estimatedDurationHours: 8,
      notes: 'E2E test pulled pork - 8lb Boston butt'
    };
  }

  /**
   * Creates test data for baby back ribs session
   */
  createRibsSession(): SessionTestData {
    return {
      userId: this.generateUserId(),
      deviceId: this.generateDeviceId(),
      startTime: new Date().toISOString(),
      meatType: 'Baby Back Ribs',
      targetTemp: 275,
      cookingMethod: 'Smoking',
      estimatedDurationHours: 4,
      notes: 'E2E test ribs - 3-2-1 method'
    };
  }

  /**
   * Creates test data for chicken session
   */
  createChickenSession(): SessionTestData {
    return {
      userId: this.generateUserId(),
      deviceId: this.generateDeviceId(),
      startTime: new Date().toISOString(),
      meatType: 'Whole Chicken',
      targetTemp: 350,
      cookingMethod: 'Roasting',
      estimatedDurationHours: 2,
      notes: 'E2E test chicken - spatchcocked whole chicken'
    };
  }

  /**
   * Generates realistic telemetry progression for brisket cooking
   * Simulates the actual temperature curve of a 12-hour brisket cook
   */
  generateBrisketTelemetryProgression(sessionId: string, deviceId: string, intervalMinutes: number = 10): TelemetryTestData[] {
    const telemetryData: TelemetryTestData[] = [];
    const totalMinutes = 12 * 60; // 12 hours
    const startTime = Date.now();

    for (let minute = 0; minute <= totalMinutes; minute += intervalMinutes) {
      const timestamp = new Date(startTime + (minute * 60 * 1000)).toISOString();
      const progress = minute / totalMinutes; // 0 to 1

      // Realistic brisket cooking curve
      const grillTemp = this.calculateBrisketGrillTemp(minute);
      const probe1Temp = this.calculateBrisketProbeTemp(minute);
      const mode = this.determineCookingMode(minute, grillTemp, probe1Temp);

      telemetryData.push({
        deviceId,
        sessionId,
        timestamp,
        temps: {
          grillTemp: this.addTemperatureVariation(grillTemp, 3), // Reduced variation from 5 to 3
          probe1Temp: this.addTemperatureVariation(probe1Temp, 2),
          probe2Temp: this.addTemperatureVariation(probe1Temp - 5, 3) // Second probe slightly cooler
        },
        setPoint: 225,
        mode,
        fanSpeed: mode === 'cooking' ? 40 + Math.floor(Math.random() * 20) : 0,
        augerRate: mode === 'cooking' ? 30 + Math.floor(Math.random() * 15) : 0
      });
    }

    return telemetryData;
  }

  /**
   * Generates accelerated telemetry data for testing (minutes instead of hours)
   */
  generateAcceleratedBrisketTelemetry(sessionId: string, deviceId: string, durationMinutes: number): TelemetryTestData[] {
    const telemetryData: TelemetryTestData[] = [];
    const intervalSeconds = 30; // Sample every 30 seconds
    const totalSeconds = durationMinutes * 60;
    const startTime = Date.now();

    for (let second = 0; second <= totalSeconds; second += intervalSeconds) {
      const timestamp = new Date(startTime + (second * 1000)).toISOString();
      const progress = second / totalSeconds; // 0 to 1

      // Map accelerated time to real brisket cooking progression
      const virtualMinute = progress * (12 * 60); // Map to 12-hour cook
      const grillTemp = this.calculateBrisketGrillTemp(virtualMinute);
      const probe1Temp = this.calculateBrisketProbeTemp(virtualMinute);
      const mode = this.determineCookingMode(virtualMinute, grillTemp, probe1Temp);

      telemetryData.push({
        deviceId,
        sessionId,
        timestamp,
        temps: {
          grillTemp: this.addTemperatureVariation(grillTemp, 3),
          probe1Temp: this.addTemperatureVariation(probe1Temp, 2)
        },
        setPoint: 225,
        mode
      });
    }

    return telemetryData;
  }

  /**
   * Creates cooking progress checkpoints for brisket
   */
  createBrisketProgressCheckpoints(): CookingProgressData[] {
    return [
      {
        phase: 'Startup',
        timeElapsed: 0,
        expectedTemp: 180,
        actualTemp: 170,
        isOnTarget: true
      },
      {
        phase: 'Early Cook',
        timeElapsed: 2 * 60, // 2 hours
        expectedTemp: 200,
        actualTemp: 195,
        isOnTarget: true
      },
      {
        phase: 'Stall Beginning',
        timeElapsed: 4 * 60, // 4 hours
        expectedTemp: 160,
        actualTemp: 165,
        isOnTarget: true
      },
      {
        phase: 'The Stall',
        timeElapsed: 6 * 60, // 6 hours
        expectedTemp: 165,
        actualTemp: 163,
        isOnTarget: true
      },
      {
        phase: 'Breaking the Stall',
        timeElapsed: 8 * 60, // 8 hours
        expectedTemp: 175,
        actualTemp: 178,
        isOnTarget: true
      },
      {
        phase: 'Final Push',
        timeElapsed: 10 * 60, // 10 hours
        expectedTemp: 190,
        actualTemp: 188,
        isOnTarget: true
      },
      {
        phase: 'Done',
        timeElapsed: 12 * 60, // 12 hours
        expectedTemp: 203,
        actualTemp: 205,
        isOnTarget: true
      }
    ];
  }

  // Private helper methods for realistic cooking simulation

  private generateDeviceId(): string {
    return `${this.deviceIdPrefix}-${String(this.deviceCounter++).padStart(3, '0')}`;
  }

  private generateUserId(): string {
    return `${this.userIdPrefix}-${String(this.userCounter++).padStart(3, '0')}`;
  }

  private calculateBrisketGrillTemp(minute: number): number {
    // Realistic grill temperature progression for brisket
    if (minute < 30) {
      // Startup phase: ramping up to target
      return 180 + (minute / 30) * 45; // 180°F to 225°F
    } else if (minute < 720) {
      // Long cooking phase: maintaining 225°F
      return 225;
    } else {
      // Final phase: might bump up slightly
      return 235;
    }
  }

  private calculateBrisketProbeTemp(minute: number): number {
    // Realistic internal temperature progression with stall
    if (minute < 60) {
      // First hour: rapid rise to 150°F
      return 70 + (minute / 60) * 80;
    } else if (minute < 240) {
      // Hours 1-4: climb to stall point
      return 150 + ((minute - 60) / 180) * 15; // 150°F to 165°F
    } else if (minute < 480) {
      // Hours 4-8: the stall (minimal temperature increase)
      return 165 + ((minute - 240) / 240) * 5; // 165°F to 170°F slowly
    } else if (minute < 600) {
      // Hours 8-10: breaking through stall
      return 170 + ((minute - 480) / 120) * 20; // 170°F to 190°F
    } else {
      // Hours 10-12: final push to done
      return 190 + ((minute - 600) / 120) * 13; // 190°F to 203°F
    }
  }

  private determineCookingMode(minute: number, grillTemp: number, probeTemp: number): TelemetryTestData['mode'] {
    if (minute < 15) {
      return 'startup';
    } else if (probeTemp >= 210) { // Higher threshold before switching to cooling
      return 'cooling';
    } else if (grillTemp > 200 && probeTemp < 210) {
      return 'cooking';
    } else if (grillTemp < 180) {
      return 'startup';
    } else {
      return 'idle';
    }
  }

  private addTemperatureVariation(baseTemp: number, variationDegrees: number): number {
    const variation = (Math.random() - 0.5) * 2 * variationDegrees;
    return Math.round((baseTemp + variation) * 10) / 10; // Round to 1 decimal place
  }
}