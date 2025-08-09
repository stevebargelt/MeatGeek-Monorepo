# MeatGeek Monorepo

A comprehensive .NET 8.0 monorepo for the MeatGeek BBQ/Grilling IoT platform, built with Nx for efficient development and CI/CD.

## Architecture Overview

The MeatGeek platform consists of several microservices that work together to provide IoT functionality for BBQ/grilling devices:

- **Sessions API**: Manages cooking sessions (cooks, smokes, BBQs)
- **Device API**: Direct device communication via Azure Relay Service
- **IoT Functions**: IoT telemetry processing and data management
- **Shared Library**: Common utilities and event schemas
- **IoT Edge**: Edge computing modules for device telemetry
- **MockDevice**: Mock BBQ device API for local testing and development

## Quick Start

### Complete System Deployment

Deploy the entire MeatGeek system from scratch:

```bash
# GitHub Actions (Recommended)
# Go to Actions → "Deploy Complete MeatGeek System" → Run workflow

# Local deployment
cd infrastructure
export AZURE_SUBSCRIPTION_ID="your-subscription-id"
export AZURE_OBJECT_ID="your-azure-ad-object-id"
./deploy.sh
```

See [`infrastructure/DEPLOYMENT.md`](infrastructure/DEPLOYMENT.md) for complete deployment guide.

### Development Commands

Prerequisites: .NET 8.0 SDK, Node.js, Azure CLI

```bash
# Install dependencies
npm install

# Build all projects
nx run-many -t build

# Run tests for all projects
nx run-many -t test

# Build specific project
nx build MeatGeek.Sessions.Api
nx build MockDevice

# Run tests for specific project
nx test MeatGeek.Sessions.Api.Tests
nx test MockDevice.Tests

# Start development server
nx serve MeatGeek.Sessions.Api
nx serve MockDevice

# Lint/format code
nx run-many -t lint
```

## Projects

### MeatGeek Sessions API
The API associated with Sessions. Sessions could also be called cooks, smokes, or BBQs. When you are actively cooking something on your grill / BBQ that is a session.

**Nx Commands:**
```bash
# Build
nx build MeatGeek.Sessions.Api
nx build MeatGeek.Sessions.Services
nx build MeatGeek.Sessions.WorkerApi

# Test
nx test MeatGeek.Sessions.Api.Tests
nx test MeatGeek.Sessions.Services.Tests
nx test MeatGeek.Sessions.WorkerApi.Tests

# Serve locally
nx serve MeatGeek.Sessions.Api
```

### MeatGeek Device API
The Device API is an Azure Function App that uses an Azure Relay Service to communicate directly with devices.

**Nx Commands:**
```bash
# Build
nx build MeatGeek.Device.Api

# Test  
nx test MeatGeek.Device.Api.Tests

# Serve locally
nx serve MeatGeek.Device.Api
```

### MeatGeek IoT Functions
Processes IoT telemetry data and manages device communication.

**Nx Commands:**
```bash
# Build
nx build MeatGeek.IoT.Functions
nx build MeatGeek.IoT.WorkerApi

# Test
nx test MeatGeek.IoT.Functions.Tests
nx test MeatGeek.IoT.WorkerApi.Tests

# Serve locally
nx serve MeatGeek.IoT.Functions
```

### MeatGeek Shared
Common utilities, event schemas, and shared functionality across all services.

**Nx Commands:**
```bash
# Build
nx build MeatGeek.Shared

# Test
nx test MeatGeek.Shared.Tests
```

### Telemetry (IoT Edge)
Edge computing module for device telemetry collection.

**Nx Commands:**
```bash
# Build
nx build Telemetry
```

### MockDevice (IoT Edge Testing)
Mock BBQ device API with realistic telemetry simulation for local IoT Edge testing and development.

**Features:**
- Realistic BBQ physics simulation (heating/cooling curves)
- Multiple cooking scenarios (Brisket, Pork Shoulder, Ribs, Chicken)  
- Component state management (auger, blower, igniter cycling)
- Docker containerization for local testing
- Background telemetry simulation updates every 5 seconds

**Nx Commands:**
```bash
# Core development commands
nx build MockDevice                    # Build the API
nx test MockDevice.Tests              # Run all unit tests (24 tests)
nx serve MockDevice                   # Start development server with hot reload

# Batch operations
nx run-many -t build,test -p MockDevice,MockDevice.Tests

# Docker operations  
nx docker-build MockDevice           # Build optimized Docker image
nx docker-run MockDevice            # Run containerized app (port 3000)

# Analysis and debugging
nx graph                             # View project dependency graph
nx show project MockDevice          # Show detailed project configuration
nx lint MockDevice                  # Format C# code
```

**API Endpoints:**
```bash
# Health and status
curl http://localhost:3000/health
curl http://localhost:3000/api/robots/MeatGeekBot/commands/get_status

# Simulation control
curl -X POST "http://localhost:3000/api/simulation/start?scenario=brisket"
curl -X POST "http://localhost:3000/api/simulation/stop" 
curl -X POST "http://localhost:3000/api/simulation/settemp?temperature=275"
```

**Docker Compose Testing:**
```bash
# Start mock device for local testing
cd iot-edge
docker-compose -f docker-compose.test.yml up mock-device

# Test with Telemetry module (full stack)  
docker-compose -f docker-compose.test.yml --profile full-stack up
```
## CI/CD Pipeline

All projects use GitHub Actions for automated testing and deployment:
- **Unit Tests**: Run on every PR
- **Build & Deploy**: Test-first deployment to Azure Functions
- **Code Coverage**: Automated test coverage reporting

## Azure Environment Setup

### Resource Groups

If recreating from scratch, create:

```bash
az group create -n MeatGeek-Sessions -l northcentralus
az group create -n MeatGeek-Proxy -l northcentralus
az group create -n MeatGeek-Device -l northcentralus
az group create -n MeatGeek-IoT -l northcentralus
az group create -n MeatGeek-Shared -l northcentralus
```

### ELMS - Logging and Monitoring

IMPORTANT: ELMS will be configured to capture all logs from the edge modules. To change this behavior, you can go to the Configuration section of the Function App 'iotedgelogsapp-d589c907' and update the regular expression for the app setting 'LogsRegex'.

IMPORTANT: You must update device twin for your IoT edge devices with "tags.logPullEnabled='true'" to collect logs from their modules.
