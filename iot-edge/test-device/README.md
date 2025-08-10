# MeatGeek Test Device

This directory contains all components for running a simulated IoT Edge test device that connects to Azure IoT Hub with realistic BBQ telemetry data.

## Components

- **mock-device/** - Realistic BBQ device simulator with physics-based temperature models
- **telemetry-direct/** - Direct IoT Hub connection module (bypasses Edge runtime for testing)
- **deployments/** - Docker Compose and deployment configurations
- **scripts/** - Automation scripts for setup and testing
- **docs/** - Detailed documentation and guides

## Quick Start

Run these commands from the `iot-edge/` root directory:

1. Copy the environment template:
```bash
cp test-device/.env.azure.example test-device/.env
```

2. Update `test-device/.env` with your Azure IoT Hub connection string

3. Start the test device:
```bash
./test-device/scripts/start-test-device.sh
```

4. Monitor telemetry:
```bash
az iot hub monitor-events --hub-name <your-hub> --device-id <your-device>
```

## Documentation

- [Setup Guide](docs/setup-guide.md) - Complete setup instructions
- [Testing Scenarios](docs/testing-scenarios.md) - Various test scenarios
- [Troubleshooting](docs/troubleshooting.md) - Common issues and solutions

## Features

- Realistic BBQ simulation with 4 cooking scenarios
- Direct Azure IoT Hub connection
- Session management support
- Remote control via direct methods
- Configurable telemetry intervals
- Docker-based deployment