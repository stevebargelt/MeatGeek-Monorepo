# IoT Test Device Implementation - PR Breakdown

## PR Sequence & Dependencies

### PR #1: Mock Device API Foundation
**Branch**: `feature/iot-mock-device-api`  
**Size**: Small (3-5 files)  
**Dependencies**: None  
**Merge Target**: `develop` or `main`

**Files Added:**
- `iot-edge/mock-device/Program.cs`
- `iot-edge/mock-device/MockDevice.csproj`
- `iot-edge/mock-device/Models/MockSmokerStatus.cs`

**Description:**
Creates the basic ASP.NET Core minimal API with the exact endpoint structure expected by the Telemetry module. Implements a simple static response that matches the SmokerStatus JSON schema.

**Acceptance Criteria:**
- API responds at `GET /api/robots/MeatGeekBot/commands/get_status`
- Response matches existing SmokerStatus JSON structure
- API starts on port 3000
- Basic health check endpoint included
- Builds successfully with `dotnet build`

---

### PR #2: Telemetry Simulation Engine
**Branch**: `feature/iot-telemetry-simulator`  
**Size**: Medium (3-4 files)  
**Dependencies**: PR #1 merged  
**Merge Target**: `develop` or `main`

**Files Added:**
- `iot-edge/mock-device/Services/TelemetrySimulator.cs`
- `iot-edge/mock-device/Services/ICookingScenario.cs`

**Files Modified:**
- `iot-edge/mock-device/Program.cs` (integrate simulator)
- `iot-edge/mock-device/Models/MockSmokerStatus.cs` (add simulation state)

**Description:**
Implements the core simulation logic with realistic BBQ cooking scenarios. Includes temperature progression, component state calculations, and configurable cooking parameters.

**Acceptance Criteria:**
- Realistic grill temperature curves (heating/cooling/steady-state)
- Food probe temperature simulation with different cooking rates
- Component state logic (auger/blower/igniter cycling)
- Configurable setpoint and cooking duration
- Background service updates simulation state every 5 seconds

---

### PR #3: Docker Containerization
**Branch**: `feature/iot-mock-device-docker`  
**Size**: Small (3-4 files)  
**Dependencies**: PR #2 merged  
**Merge Target**: `develop` or `main`

**Files Added:**
- `iot-edge/mock-device/Dockerfile`
- `iot-edge/docker-compose.test.yml`
- `iot-edge/.env.test`

**Files Modified:**
- `iot-edge/mock-device/MockDevice.csproj` (Docker optimization)

**Description:**
Containerizes the mock device API with Docker and creates docker-compose configuration for local testing with the existing Telemetry module.

**Acceptance Criteria:**
- Docker image builds successfully
- Container exposes port 3000 correctly
- docker-compose brings up both mock-device and telemetry-module
- Containers can communicate over Docker network
- Environment variable configuration working

---

### PR #4: Nx Build Integration
**Branch**: `feature/iot-mock-device-nx-integration`  
**Size**: Small (2-3 files)  
**Dependencies**: PR #1 merged  
**Merge Target**: `develop` or `main`

**Files Added:**
- `iot-edge/mock-device/project.json`

**Files Modified:**
- Root `nx.json` or relevant Nx configuration
- `iot-edge/mock-device/MockDevice.csproj` (Nx compatibility)

**Description:**
Integrates the mock device into the Nx monorepo build system with proper build targets, dependencies, and caching configuration.

**Acceptance Criteria:**
- `nx build MockDevice` works successfully
- `nx test MockDevice.Tests` runs unit tests
- `nx serve MockDevice` starts development server
- Build caching works properly
- Dependency graph shows correct relationships

---

### PR #5: Azure IoT Edge Test Deployment
**Branch**: `feature/iot-edge-test-deployment`  
**Size**: Medium (2-3 files)  
**Dependencies**: PR #4 merged  
**Merge Target**: `develop` or `main`

**Files Added:**
- `iot-edge/deployment.test.template.json`
- `iot-edge/config/deployment.test.amd64.json`

**Files Modified:**
- `iot-edge/modules/Telemetry/module.json` (test image tag support)

**Description:**
Creates Azure IoT Edge deployment manifest for testing with both the existing Telemetry module and optional mock-device module deployment to Azure.

**Acceptance Criteria:**
- Test deployment template with proper module configuration
- Environment variable mapping for test scenarios
- Message routing configuration to Azure IoT Hub
- Container registry integration for test images
- Deployment validates successfully with Azure CLI

---

### PR #6: Integration Tests & Documentation
**Branch**: `feature/iot-integration-tests-docs`  
**Size**: Medium (4-5 files)  
**Dependencies**: PR #4, PR #6 merged  
**Merge Target**: `develop` or `main`

**Files Added:**
- `iot-edge/integration-tests/docker-compose.integration.yml`
- `iot-edge/integration-tests/test-runner.sh`
- `iot-edge/README.md` (comprehensive testing guide)

**Files Modified:**
- `iot-edge/iot-test-device.md` (update with final implementation details)

**Description:**
End-to-end integration testing setup and comprehensive documentation for local testing, Azure deployment, and troubleshooting.

**Acceptance Criteria:**
- Integration test script validates complete data flow
- Docker-based integration tests run automatically
- Documentation covers local setup, Azure registration, and troubleshooting
- Test scenarios for session management and direct methods
- Performance benchmarking and monitoring guidance

---

## PR Review Guidelines

### Small PRs (1-5 files, <200 lines changed)
- **Review Time**: 1-2 hours
- **Reviewers**: 1 required, 1 optional
- **Focus**: Code quality, unit tests, documentation

### Medium PRs (3-6 files, 200-500 lines changed)
- **Review Time**: 2-4 hours
- **Reviewers**: 2 required
- **Focus**: Architecture alignment, integration points, comprehensive testing

### Merge Strategy
- **Feature Branches**: Short-lived, merge to `develop`
- **Integration**: Regular merges to avoid conflicts
- **Testing**: All tests pass before merge
- **Documentation**: Updated with each PR

## Development Timeline

**Week 1**: PR #1, PR #2, PR #5 (Foundation + Simulation)
**Week 2**: PR #3, PR #4 (Testing + Containerization)  
**Week 3**: PR #6, PR #7 (Azure Integration + Documentation)

**Total Estimated Effort**: 3 weeks for complete implementation
**Parallel Development**: PRs #3, #4, #5 can be developed concurrently after PR #2

## Risk Mitigation

**Dependency Management**: 
- Keep PRs loosely coupled where possible
- Mock interfaces for integration testing until dependencies merge

**Testing Strategy**:
- Unit tests in early PRs prevent regression
- Integration tests validate end-to-end functionality
- Performance tests ensure production readiness

**Rollback Plan**:
- Each PR is independently revertible
- Feature flags for enabling/disabling mock device functionality
- Backward compatibility with existing Telemetry module