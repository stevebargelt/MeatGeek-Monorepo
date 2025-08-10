/**
 * Test environment configuration for different execution modes
 */

export interface TestEnvironmentConfig {
  mode: 'local-first' | 'integration' | 'full-azure';
  services: {
    mockDevice: {
      enabled: boolean;
      baseUrl: string;
      timeout: number;
    };
    azure: {
      enabled: boolean;
      connectionStrings: {
        iotHub?: string;
        cosmosDb?: string;
        eventGrid?: string;
      };
    };
    docker: {
      enabled: boolean;
      composeFile?: string;
      requiredImages: string[];
    };
  };
  features: {
    realTelemetry: boolean;
    crossServiceValidation: boolean;
    longRunningWorkflows: boolean;
    performanceTesting: boolean;
  };
}

export const TEST_ENVIRONMENTS: Record<string, TestEnvironmentConfig> = {
  'local-first': {
    mode: 'local-first',
    services: {
      mockDevice: {
        enabled: true,
        baseUrl: 'http://localhost:5000', // In-process mock
        timeout: 1000
      },
      azure: {
        enabled: false,
        connectionStrings: {}
      },
      docker: {
        enabled: false,
        requiredImages: []
      }
    },
    features: {
      realTelemetry: false,
      crossServiceValidation: false,
      longRunningWorkflows: true, // Can simulate quickly
      performanceTesting: false
    }
  },
  'integration': {
    mode: 'integration',
    services: {
      mockDevice: {
        enabled: true,
        baseUrl: 'http://localhost:8080',
        timeout: 5000
      },
      azure: {
        enabled: false, // Uses local emulators
        connectionStrings: {}
      },
      docker: {
        enabled: true,
        composeFile: '../iot-edge/integration-tests/docker-compose.integration.yml',
        requiredImages: [
          'mcr.microsoft.com/azureiotedge-workload:1.4',
          'mcr.microsoft.com/azureiotedge-hub:1.4'
        ]
      }
    },
    features: {
      realTelemetry: true,
      crossServiceValidation: true,
      longRunningWorkflows: true,
      performanceTesting: false
    }
  },
  'full-azure': {
    mode: 'full-azure',
    services: {
      mockDevice: {
        enabled: true,
        baseUrl: 'http://localhost:8080',
        timeout: 10000
      },
      azure: {
        enabled: true,
        connectionStrings: {
          iotHub: process.env.TEST_IOT_HUB_CONNECTION_STRING,
          cosmosDb: process.env.TEST_COSMOS_DB_CONNECTION_STRING,
          eventGrid: process.env.TEST_EVENT_GRID_CONNECTION_STRING
        }
      },
      docker: {
        enabled: true,
        composeFile: '../iot-edge/integration-tests/docker-compose.integration.yml',
        requiredImages: [
          'mcr.microsoft.com/azureiotedge-workload:1.4',
          'mcr.microsoft.com/azureiotedge-hub:1.4'
        ]
      }
    },
    features: {
      realTelemetry: true,
      crossServiceValidation: true,
      longRunningWorkflows: true,
      performanceTesting: true
    }
  }
};

export function getCurrentEnvironment(): TestEnvironmentConfig {
  const envMode = process.env.E2E_TEST_MODE || 'local-first';
  const config = TEST_ENVIRONMENTS[envMode];
  
  if (!config) {
    console.warn(`Unknown test mode '${envMode}', falling back to 'local-first'`);
    return TEST_ENVIRONMENTS['local-first'];
  }
  
  return config;
}

export function isServiceAvailable(serviceName: keyof TestEnvironmentConfig['services']): boolean {
  const config = getCurrentEnvironment();
  return config.services[serviceName].enabled;
}

export function isFeatureEnabled(featureName: keyof TestEnvironmentConfig['features']): boolean {
  const config = getCurrentEnvironment();
  return config.features[featureName];
}