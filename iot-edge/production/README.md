# MeatGeek IoT Edge - Production Components

This directory contains production-ready IoT Edge modules and deployment configurations.

## Structure

- **modules/** - Production IoT Edge modules
  - `Telemetry/` - Main telemetry collection module
- **deployments/** - Deployment manifests and configurations
  - `deployment.template.json` - Main production template
  - `config/` - Platform-specific deployment configurations
- **scripts/** - Production utilities
  - `getlogs.sh` - Log collection script for edge devices

## Deployment

### Build Modules
```bash
# Build for specific platform
docker build -f modules/Telemetry/Dockerfile.amd64 -t <registry>/telemetry:latest modules/Telemetry
```

### Deploy to Edge Device
```bash
# Deploy using Azure CLI
az iot edge set-modules --device-id <device-id> --hub-name <hub-name> --content deployments/deployment.template.json
```

## Module Details

### Telemetry Module
- Polls BBQ device status via HTTP
- Manages session associations
- Sends data to IoT Hub via Edge Hub
- Supports remote configuration via direct methods
- Handles connection resilience

## Configuration

The deployment template uses environment variables for:
- Azure Container Registry credentials
- IoT Hub connection details
- Module-specific settings

## Monitoring

Collect logs from edge device:
```bash
./scripts/getlogs.sh
```

## Platform Support

- AMD64 (x64 Linux)
- ARM32v7 (32-bit ARM)
- ARM64v8 (64-bit ARM)