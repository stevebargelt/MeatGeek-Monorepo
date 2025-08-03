# MeatGeek Monorepo

A comprehensive .NET 8.0 monorepo for the MeatGeek BBQ/Grilling IoT platform, built with Nx for efficient development and CI/CD.

## Architecture Overview

The MeatGeek platform consists of several microservices that work together to provide IoT functionality for BBQ/grilling devices:

- **Sessions API**: Manages cooking sessions (cooks, smokes, BBQs)
- **Device API**: Direct device communication via Azure Relay Service
- **IoT Functions**: IoT telemetry processing and data management
- **Shared Library**: Common utilities and event schemas
- **IoT Edge**: Edge computing modules for device telemetry

## Quick Start

### Prerequisites
- .NET 8.0 SDK
- Node.js (for Nx workspace)
- Azure CLI (for deployment)

### Development Commands

```bash
# Install dependencies
npm install

# Build all projects
nx run-many -t build

# Run tests for all projects
nx run-many -t test

# Build specific project
nx build MeatGeek.Sessions.Api

# Run tests for specific project
nx test MeatGeek.Sessions.Api.Tests

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
