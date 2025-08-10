# MeatGeek IoT Edge Shared Models

This library contains shared telemetry models and constants used across all MeatGeek IoT Edge components.

## Contents

### Models
- **SmokerStatus** - Complete BBQ smoker device status
- **Temps** - Temperature readings from BBQ sensors  
- **DeviceResponse** - Generic API response wrapper

### Constants
- **TelemetryConstants** - Shared constants for data types, modes, TTL values, and intervals

## Features

- **Dual JSON Serialization** - Supports both System.Text.Json and Newtonsoft.Json
- **Nullable Reference Types** - Full nullable annotations for type safety
- **Constants** - Centralized values to prevent magic strings
- **.NET 8.0** - Modern .NET runtime support

## Usage

Add project reference:
```xml
<ProjectReference Include="path/to/shared/MeatGeek.IoT.Edge.Shared.csproj" />
```

Import namespaces:
```csharp
using MeatGeek.IoT.Edge.Shared.Models;
using MeatGeek.IoT.Edge.Shared.Constants;
```

## Components Using This Library

- **Production Telemetry Module** (`production/modules/Telemetry/`)
- **Mock Device** (`test-device/mock-device/`) 
- **Telemetry Direct** (`test-device/telemetry-direct/`)
- **Unit Tests** (`unit-tests/`)

This replaces the previously duplicated model definitions across these components.