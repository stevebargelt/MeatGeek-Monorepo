# IoT Edge Shared Models Consolidation Plan

## Current State Analysis

After analyzing the MeatGeek IoT Edge components, I've identified model duplication within the IoT Edge project:

### 🔍 Duplicate Models Found in IoT Edge

| Location | Models | JSON Library | Notes |
|----------|--------|--------------|-------|
| `iot-edge/production/modules/Telemetry/Program.cs` | `SmokerStatus`, `Temps` | Newtonsoft.Json | Embedded classes (lines 347-390) |
| `iot-edge/test-device/mock-device/Models/` | `MockSmokerStatus`, `MockTemps`, `MockDeviceResponse` | System.Text.Json | Nullable types, better defaults |

### 🎯 Issues with Current State

1. **Code Duplication**: Models defined in both production and test components
2. **Maintenance Burden**: Changes require updates in multiple places within IoT Edge
3. **Inconsistency**: Different JSON serialization approaches (Newtonsoft vs System.Text.Json)
4. **Type Safety**: Inconsistent nullable reference type usage
5. **Testing Complexity**: Mock models separate from production models

### 📝 Out of Scope (Future Phase)

**IoT Functions Migration**: The `iot/src/MeatGeek.IoT.Functions/Models/` project will be addressed in a future phase to keep this effort focused on IoT Edge components only.

## 📋 Phase-Based Consolidation Plan

### Phase 1: Create IoT Edge Shared Models Library

**Goal**: Create a dedicated shared models library for IoT Edge components only

#### 1.1 Create IoT Edge Shared Library
```
iot-edge/shared/
├── MeatGeek.IoT.Edge.Shared.csproj
├── Models/
│   ├── SmokerStatus.cs
│   ├── Temps.cs
│   └── DeviceResponse.cs
├── Constants/
│   └── TelemetryConstants.cs
└── README.md
```

#### 1.2 Modern Model Design
- **System.Text.Json** for consistent serialization
- **Nullable reference types** enabled
- **Proper default values** and initialization
- **Validation attributes** where appropriate
- **XML documentation** for all public members

#### 1.3 Backward Compatibility
- Support both `System.Text.Json` and `Newtonsoft.Json` via attributes
- Maintain existing property names and types
- Include conversion utilities if needed

### Phase 2: IoT Edge Component Migration Strategy

#### 2.1 IoT Edge Production Migration
```
iot-edge/production/modules/Telemetry/
├── Telemetry.csproj      # Add reference to MeatGeek.IoT.Edge.Shared
├── Dockerfile.amd64      # Update build context and COPY commands
├── Dockerfile.arm32v7    # Update build context and COPY commands  
├── Dockerfile.arm64v8    # Update build context and COPY commands
├── Dockerfile.windows-amd64 # Update build context and COPY commands
└── Program.cs            # Remove embedded classes (lines 347-390), add using
```

#### 2.2 IoT Edge Test Device Migration
```
iot-edge/test-device/mock-device/
├── MockDevice.csproj     # Add reference to MeatGeek.IoT.Edge.Shared
├── Dockerfile           # Update build context and layer caching
├── Models/ (DELETE entire directory)
│   ├── MockSmokerStatus.cs (DELETE)
│   ├── MockTemps.cs (DELETE)
│   └── MockDeviceResponse.cs (DELETE)
└── [Update all references to use shared models]
```

#### 2.3 Docker Configuration Updates
```
iot-edge/test-device/deployments/
├── docker-compose.azure.yml     # Update build contexts
└── docker-compose.local.yml     # Update build contexts (if exists)

iot-edge/test-device/telemetry-direct/
└── Dockerfile                   # Update to reference shared models
```

#### 2.4 Unit Tests Migration
```
iot-edge/unit-tests/MockDevice.Tests/
├── MockDevice.Tests.csproj  # Add reference to MeatGeek.IoT.Edge.Shared
└── [Update test references to use shared models]
```

#### 2.5 Integration Tests Migration
```
iot-edge/integration-tests/
└── [Update any direct model references to use shared library]
```

### Phase 3: Docker Container Considerations

#### 3.1 Build Context Challenges

**Current State:**
```yaml
# docker-compose.azure.yml
mock-device:
  build:
    context: ../mock-device    # Limited to mock-device directory
    dockerfile: Dockerfile

telemetry-direct:
  build:
    context: ../telemetry-direct  # Limited to telemetry-direct directory  
    dockerfile: Dockerfile
```

**Problem:** Shared library at `../../shared/` is outside the build context, making it inaccessible during Docker builds.

#### 3.2 Solutions for Docker Integration

**Option A: Multi-Stage Builds with Wider Context (Recommended)**
```yaml
# Updated docker-compose.azure.yml
mock-device:
  build:
    context: ../../              # Build from iot-edge root
    dockerfile: test-device/mock-device/Dockerfile
    
telemetry-direct:
  build:
    context: ../../              # Build from iot-edge root  
    dockerfile: test-device/telemetry-direct/Dockerfile
```

**Option B: Dockerfile Updates with Selective Copying**
```dockerfile
# Updated mock-device/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared library first (for better layer caching)
COPY shared/ shared/
COPY test-device/mock-device/MockDevice.csproj test-device/mock-device/
RUN dotnet restore test-device/mock-device/MockDevice.csproj

# Copy application source
COPY test-device/mock-device/ test-device/mock-device/
RUN dotnet build test-device/mock-device/MockDevice.csproj -c Release --no-restore
```

#### 3.3 Layer Caching Optimization

**Optimized Build Order:**
1. Copy shared library and project files first
2. Run `dotnet restore` (cached when dependencies don't change)
3. Copy application source code
4. Build and publish

**Benefits:**
- Shared library changes invalidate cache for all containers
- Application code changes only invalidate final layers
- Faster rebuilds during development

#### 3.4 Docker Compose Service Dependencies

**Build Order Requirements:**
```yaml
# No explicit build dependencies needed - Docker handles project references
# But we should ensure services start in correct order

services:
  mock-device:
    # Builds with shared library included
    
  telemetry-direct:
    depends_on:
      mock-device:
        condition: service_healthy  # Wait for mock device to be ready
```

### Phase 4: Enhanced Shared Models

#### 4.1 Proposed Shared Model Structure

```csharp
// iot-edge/shared/Models/SmokerStatus.cs
namespace MeatGeek.IoT.Edge.Shared.Models;

/// <summary>
/// Represents the complete status of a BBQ smoker device
/// </summary>
public class SmokerStatus
{
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonPropertyName("ttl")]
    [JsonProperty("ttl")]
    public int? Ttl { get; set; }

    [JsonPropertyName("smokerId")]
    [JsonProperty("smokerId")]
    public string? SmokerId { get; set; }

    [JsonPropertyName("sessionId")]
    [JsonProperty("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("type")]
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonPropertyName("augerOn")]
    [JsonProperty("augerOn")]
    public bool AugerOn { get; set; }

    [JsonPropertyName("blowerOn")]
    [JsonProperty("blowerOn")]
    public bool BlowerOn { get; set; }

    [JsonPropertyName("igniterOn")]
    [JsonProperty("igniterOn")]
    public bool IgniterOn { get; set; }

    [JsonPropertyName("temps")]
    [JsonProperty("temps")]
    public Temps Temps { get; set; } = new();

    [JsonPropertyName("fireHealthy")]
    [JsonProperty("fireHealthy")]
    public bool FireHealthy { get; set; }

    [JsonPropertyName("mode")]
    [JsonProperty("mode")]
    public string Mode { get; set; } = "idle";

    [JsonPropertyName("setPoint")]
    [JsonProperty("setPoint")]
    public int SetPoint { get; set; }

    [JsonPropertyName("modeTime")]
    [JsonProperty("modeTime")]
    public DateTime ModeTime { get; set; }

    [JsonPropertyName("currentTime")]
    [JsonProperty("currentTime")]
    public DateTime CurrentTime { get; set; }
}
```

#### 4.2 Additional Utility Classes

```csharp
// iot-edge/shared/Models/DeviceResponse.cs
namespace MeatGeek.IoT.Edge.Shared.Models;

public class DeviceResponse<T>
{
    [JsonPropertyName("result")]
    [JsonProperty("result")]
    public T Result { get; set; } = default!;
}

// iot-edge/shared/Constants/TelemetryConstants.cs
namespace MeatGeek.IoT.Edge.Shared.Constants;

public static class TelemetryConstants
{
    public static class Types
    {
        public const string Status = "status";
        public const string Telemetry = "telemetry";
    }

    public static class Modes
    {
        public const string Idle = "idle";
        public const string Startup = "startup";
        public const string Heating = "heating";
        public const string Cooking = "cooking";
        public const string Cooling = "cooling";
    }

    public static class Ttl
    {
        public const int SessionData = -1;      // Permanent
        public const int TelemetryData = 259200; // 3 days
    }
}
```

### Phase 5: Implementation Steps

#### 5.1 Step-by-Step Migration (IoT Edge Only)
1. **Create shared models library** in `iot-edge/shared/`
2. **Set up project structure** and dependencies
3. **Implement shared models** with dual JSON serialization support
4. **Update Docker configurations** to handle shared library build context
5. **Migrate Telemetry module** (remove embedded classes, update Dockerfiles)
6. **Migrate MockDevice** (remove Models/ directory, update Dockerfile)
7. **Update docker-compose files** with new build contexts
8. **Update unit tests** and integration tests
9. **Verify all builds** and tests pass (including Docker builds)
10. **Test Docker containers** ensure health checks pass
11. **Clean up** old model files
12. **Update Nx build configuration** for new shared project

#### 5.2 Testing Strategy
- **Unit tests** for shared models
- **Serialization tests** for both JSON libraries
- **Integration tests** to verify IoT Edge end-to-end compatibility  
- **Mock data validation** against shared schemas
- **Docker container testing** to ensure all components work together

### Phase 6: Benefits & Future Enhancements

#### 6.1 Immediate Benefits (IoT Edge Scope)
- **Single source of truth** for IoT Edge telemetry models
- **Consistent serialization** across production and test components
- **Reduced maintenance** burden within IoT Edge project
- **Type safety** improvements across all IoT Edge components
- **Better IntelliSense** support for developers
- **Simplified testing** with shared model definitions

#### 6.2 Future Enhancements
- **Model validation** attributes for data integrity
- **OpenAPI schema generation** for documentation
- **Protocol buffer support** for improved performance
- **Versioning strategy** for model evolution
- **IoT Functions integration** (Phase 2 of broader effort)

## 🚧 Migration Risks & Mitigation

### Risks (IoT Edge Focused)
1. **Breaking changes** to production Telemetry module
2. **Serialization compatibility** issues between components
3. **Docker build failures** during transition
4. **Build context access** issues for shared library
5. **Container startup failures** due to missing dependencies
6. **Layer caching invalidation** causing slower builds
7. **Test coverage** gaps in migration
8. **Integration test** disruptions

### Mitigation
1. **Feature branch** for all IoT Edge changes
2. **Docker build context testing** before code migration
3. **Comprehensive testing** of all IoT Edge components before merge
4. **Gradual migration** one component at a time with Docker validation
5. **Rollback plan** with git branch protection
6. **Dual serialization support** to maintain compatibility
7. **Container testing** to ensure Docker builds and health checks work
8. **Mock device validation** to ensure test scenarios still work
9. **Build time monitoring** to catch performance regressions
10. **Multi-platform Docker testing** (amd64, arm32v7, arm64v8)

## 📊 Success Metrics (IoT Edge Scope)

- ✅ All duplicate model files removed from IoT Edge components
- ✅ Single IoT Edge shared models project referenced by all IoT Edge consumers
- ✅ All IoT Edge tests passing (unit, integration, docker)
- ✅ No breaking changes to telemetry data formats or APIs
- ✅ Improved build times within IoT Edge project
- ✅ Enhanced developer experience for IoT Edge development
- ✅ Mock device and production module use identical models

## 🔄 Implementation Timeline (IoT Edge Only)

| Phase | Duration | Dependencies | Key Docker Impacts |
|-------|----------|--------------|-------------------|
| Phase 1: Create IoT Edge Shared Library | 1 day | None | Set up project structure |
| Phase 2: Update Docker Configurations | 0.5 days | Phase 1 | Build contexts, Dockerfiles |
| Phase 3: Migrate Production Telemetry | 0.5 days | Phase 2 | Multi-platform Dockerfiles |
| Phase 4: Migrate MockDevice | 0.5 days | Phase 2 | Test device Docker setup |
| Phase 5: Update Tests & Integration | 1 day | Phase 3 & 4 | Docker builds, compose tests |
| Phase 6: Cleanup & Verification | 0.5 days | Phase 5 | Final container testing |
| **Total** | **3.5-4 days** | | **Includes Docker work** |

## 🚀 Future Phase: IoT Functions Integration

**Deferred to separate effort**: Migration of `iot/src/MeatGeek.IoT.Functions/Models/` to either:
- The monorepo-wide `MeatGeek.Shared` library, OR
- Integration with the IoT Edge shared library

This will be planned as a separate phase to avoid scope creep and focus this effort purely on IoT Edge consolidation.

---

This focused consolidation will significantly improve the maintainability and consistency of the MeatGeek IoT Edge telemetry models while keeping the effort scoped and manageable.